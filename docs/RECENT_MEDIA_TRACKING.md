# 🕒 Recent Media Tracking System

This document describes the architecture, data model, queue session lifecycle, and UI behavior of the Recent Media tracking system on the Home page (`HomePage.xaml`).

---

## 📋 Table of Contents

- [📖 Overview](#-overview)
- [🏗️ Architecture & Components](#️-architecture--components)
- [📦 Supported Recent Entry Types (`IRecentItem`)](#-supported-recent-entry-types-irecentitem)
- [🔄 Queue Session Lifecycle & Tracking Rules](#-queue-session-lifecycle--tracking-rules)
  - [Container vs. Individual Item Tracking](#container-vs-individual-item-tracking)
  - [Loose Multi-File Queues & 1-Item Rules](#loose-multi-file-queues--1-item-rules)
  - [Deferred Session Upgrades](#deferred-session-upgrades)
  - [In-Queue Mutations & Origin Unlinking](#in-queue-mutations--origin-unlinking)
- [🔑 Deduplication & Live Re-Playback Semantics](#-deduplication--live-re-playback-semantics)
- [⚡ Active & Playing State Synchronization](#-active--playing-state-synchronization)
- [💾 SQLite Persistence & UWP File Access](#-sqlite-persistence--uwp-file-access)

---

## 📖 Overview

Screenbox's Recent Media system records **shortcuts to live queue origins** rather than play queue snapshots or raw Windows MRU (`MostRecentlyUsedList`) entries.

Key characteristics:
- **Capacity**: Up to **100 items** (`RecentContext.MaxRecentItems = 100`), ordered by `last_played DESC`.
- **Live Origin Shortcuts**: Recent entries represent live sources—a single file/URL (`MediaViewModel`), Folder or `.m3u`/`.m3u8` playlist file (`MediaCollectionViewModel`), User Playlist (`PlaylistViewModel`), Album (`AlbumViewModel`), or Artist (`ArtistViewModel`). Re-opening any container card resolves and plays its **current live contents** (e.g., new files added to a folder or new songs added under an artist).
- **No Snapshot Rows for Containers**: Only `SingleMedia` stores a track record in `recent_entry_items`. Container shortcuts persist metadata and any required UWP folder/file MRU token directly on `recent_entries`.
- **Queue Mutation Unlinking**: Advancing (`Next`/`Previous`), shuffling/unshuffling, or reordering items inside an active container session preserves its `QueueOrigin`. Adding or removing items from the queue unlinks the container origin so subsequent track navigations record played tracks individually.

---

## 🏗️ Architecture & Components

Because `HomePageViewModel` is transient and only alive while the user is viewing the Home page, recent entry persistence, deferred library/playlist linking, and queue-session recording are owned by a singleton coordinator (`RecentMediaCoordinator`).

```mermaid
flowchart TD
    subgraph PlaybackInitiators["Playback Initiators"]
        Drop["Drag & Drop / Open File(s) / Open Folder / URL"]
        FolderView["FolderViewPage"]
        Library["AlbumsPage / ArtistsPage / PlaylistsPage"]
        LibWide["SongsPage / AllVideosPage / SearchResultPage"]
        HomeCard["HomePage Recent Card Click"]
    end

    subgraph CoreQueue["Play Queue Management (Singleton)"]
        PQC["PlayQueueCoordinator"]
        PQContext["PlayQueueContext\n(ActiveSessionId, ActiveOrigin, CurrentItem)"]
    end

    subgraph RecentManagement["Recent Media Management (Singleton)"]
        RMC["RecentMediaCoordinator\n(IRecentMediaCoordinator)"]
        RContext["RecentContext\n(IReadOnlyList<IRecentItem>, Max=100)"]
        DB["DatabaseService (SQLite)\nrecent_entries + recent_entry_items"]
    end

    subgraph UI["Presentation Layer (Transient)"]
        HPVM["HomePageViewModel\n(ObservableCollection<IRecentItem> via SyncItems)"]
        HP["HomePage.xaml\n(RecentItemTemplateSelector + GridViewItemTemplates)"]
    end

    PlaybackInitiators -->|"PlayMediaMessage / SetQueueMessage\n(QueueSession + QueueOrigin)"| PQC
    PQC -->|"Updates ActiveSessionId, ActiveOrigin, CurrentItem\n& MediaViewModel.IsPlayingIndividually"| PQContext
    PQC -->|"QueuePlaybackStateChangedMessage"| RContext
    PQC -->|"QueueSessionStartedMessage(Queue, IsUpgrade)"| RMC
    RMC <-->|"Load / Upsert / Delete / Prune 100"| DB
    RMC -->|"Publishes atomic IReadOnlyList<IRecentItem> snapshots"| RContext
    RContext -->|"PropertyChangedMessage<IReadOnlyList<IRecentItem>>"| HPVM
    HPVM --> HP
```

| Component | Lifetime | Responsibility |
|---|---|---|
| `IRecentItem` | Interface | Minimal shared contract (`INotifyPropertyChanged`, `Name`, `IsPlaying`) implemented directly by all 5 item ViewModel types. |
| `QueueSession` | Per queue snapshot | Carries `SessionId` (`Guid`), `Origin` (`QueueOrigin?`), and `GetCanonicalItems()`. |
| `QueueOrigin` | Immutable record | Describes the source container (`Kind`, `SourceId`, `Title`, `Subtitle`, `Token`, `TrackItemsIndividually`). |
| `PlayQueueCoordinator` | Singleton | Owns the active `QueueSession`, updates `PlayQueueContext`, maintains `MediaViewModel.IsPlayingIndividually`, unlinks `Origin` on add/remove mutations, and broadcasts `QueueSessionStartedMessage` / `QueuePlaybackStateChangedMessage`. |
| `RecentMediaCoordinator` | Singleton | Listens to `QueueSessionStartedMessage`, `PropertyChangedMessage<MusicLibrary>` (reloading on scan completion), `Playlists.CollectionChanged` (`Remove`), and `GeneralSettings.ShowRecent`; builds local item lists per operation (displaying non-playable empty ViewModels when libraries/playlists are not yet loaded, then replacing them and reusing `MediaCollectionViewModel`s on `LoadAsync(forceReload: true)`), publishes atomic snapshots to `RecentContext.Recent`, and persists entries to SQLite. |
| `RecentContext` | Singleton | Observable recipient holding the snapshot `IReadOnlyList<IRecentItem> Recent` (`[NotifyPropertyChangedRecipients]`) and `MaxRecentItems = 100`. |
| `MediaCollectionViewModel` | Per folder / `.m3u` card | Lightweight ViewModel representing a live Folder or playlist-file (`.m3u`/`.m3u8`/URI) shortcut; resolves live items on demand via MRU token and `IQueueService`. |
| `HomePageViewModel` | Transient | Maintains a local `ObservableCollection<IRecentItem> Recent` synced from `RecentContext.Recent` via `SyncItems()` + `TryExecuteOrEnqueue()`, and handles `Play`, `PlayNext`, `AddToQueue`, `Remove`, context flyout resolution, and multi-select actions via pattern matching on `IRecentItem`. |
| `RecentItemTemplateSelector` | UI Selector | Maps each `IRecentItem` concrete type to its strongly-typed template in `GridViewItemTemplates.xaml`. |

---

## 📦 Supported Recent Entry Types (`IRecentItem`)

| `RecentItemKind` | Concrete ViewModel | Trigger Scenarios | Home Card Template | Stored in `recent_entry_items`? |
|---|---|---|---|---|
| `SingleMedia` (`0`) | `MediaViewModel` | Single local file, network/stream URL, loose multi-file track playback, or individual track from a library-wide queue (`SongsPage`, `AllVideosPage`, `SearchResultPage` songs). | `MediaGridViewItemTemplate` | **Yes** (1 row + file MRU token) |
| `MediaCollection` (`1`) | `MediaCollectionViewModel` | Dropped/opened Folder (`StorageFolder`), auto-enqueued neighboring folder ($\ge 2$ playable files), `.m3u`/`.m3u8` playlist file, or URL with sub-items. | `MediaCollectionGridViewItemTemplate` (`FolderGlyph` / `PlaylistGlyph`) | **No** (0 rows; container path + MRU `token` stored on `recent_entries`) |
| `Playlist` (`2`) | `PlaylistViewModel` | Playing a saved playlist from `PlaylistsPage` or `PlaylistDetailsPage`. | `PlaylistGridViewItemTemplate` (`PlaylistGlyph`) | **No** (0 rows; resolved live from `PlaylistsContext.Playlists`) |
| `Album` (`3`) | `AlbumViewModel` | Playing an album from `AlbumsPage`, `AlbumDetailsPage`, or `SearchResultPage`. | `AlbumGridViewItemTemplate` | **No** (0 rows; resolved/swapped live from `LibraryContext.Music.Albums`) |
| `Artist` (`4`) | `ArtistViewModel` | Playing an artist from `ArtistsPage`, `ArtistDetailsPage`, or `SearchResultPage`. | `ArtistGridViewItemTemplate` | **No** (0 rows; resolved/swapped live from `LibraryContext.Music.Artists`) |

---

## 🔄 Queue Session Lifecycle & Tracking Rules

### Container vs. Individual Item Tracking

`QueueOrigin.TrackItemsIndividually` determines whether a queue session is tracked as a single container shortcut or per track:

1. **Container Sessions (`TrackItemsIndividually == false`)**:
   - Applies to `Playlist`, `Album`, `Artist`, and `MediaCollection` when `SourceId` is non-empty (Folder, playlist file, or expandable URL).
   - Recorded **once** when the queue session starts (`ApplyQueueSnapshot`).
   - Advancing (`Next`/`Previous`), auto-advancing on media end, or selecting another track inside the Play Queue (`PlayMediaMessage(item, existing: true)`) preserves `SessionId` and `Origin` and **does not** add child tracks to Recent.

2. **Individual Track Sessions (`TrackItemsIndividually == true` or `Origin == null`)**:
   - Applies to `SingleMedia`, library-wide queues (`QueueOrigin.ForLibraryQueue`), loose multi-file selections/drops, and unlinked modified queues.
   - Recorded in `ProcessCurrentItemChangedAsync` whenever `PlayQueueContext.CurrentItem` changes, adding or promoting each navigated track individually as a `SingleMedia` card.

### Loose Multi-File Queues & 1-Item Rules

| User Action | Playable Files Count | Recorded Entry | Rationale |
|---|---|---|---|
| Drop a folder or use **Open Folder** | $\ge 1$ | `MediaCollection` (Folder shortcut) | Explicit folder origin; clicking reloads live folder contents. |
| Drop or open an `.m3u`/`.m3u8` playlist file | $\ge 1$ | `MediaCollection` (Playlist file shortcut) | Explicit playlist file origin; clicking re-expands live file contents. |
| Play a `Playlist`, `Album`, or `Artist` | $\ge 1$ | `Playlist` / `Album` / `Artist` | Explicit live container. |
| Drop or pick multiple loose files, or multi-select **Play** | $\ge 2$ | Tracks played item(s) as `SingleMedia` | No persistent folder or playlist origin exists on disk to shortcut to. |
| Open/drop a single file (with `EnqueueAllFilesInFolder` enabled) | $1$ | `SingleMedia` | Only playable file in the folder. |
| Open/drop a single file (with `EnqueueAllFilesInFolder` enabled) | $\ge 2$ | `MediaCollection` (Folder shortcut) | Neighboring files expanded into a folder session (`IsUpgrade: true`). |

### Deferred Session Upgrades

Two playback flows start as a 1-item `SingleMedia` queue and asynchronously expand within the **same** `SessionId`:

1. **Neighboring Files Auto-Enqueue (`MessengerExtensions.PlayFilesAsync` → `QueueService.AddNeighboringFilesAsync`)**:
   - `PlayFilesAsync` immediately sends a 1-item `SetQueueMessage` (`SingleMedia`) so playback begins immediately.
   - `AddNeighboringFilesAsync` enumerates the parent folder; if $> 1$ playable files are found, it updates `Origin = QueueOrigin.ForMediaCollection(folderName, folderPath, folderToken)` within the same `SessionId`.
2. **Expandable URL Sub-Items (`PlayQueueCoordinator.CheckSubItemsAsync`)**:
   - Opening a URL starts as `SingleMedia`. If LibVLC parses $> 1$ sub-items, `CheckSubItemsAsync` upgrades `_queueSession.Origin` to `MediaCollection(title, url)` within the same `SessionId`.

When `PlayQueueCoordinator` detects `_recordedSessionId == queueSession.SessionId` transitioning from `SingleMedia` to `MediaCollection`, it sends `QueueSessionStartedMessage(playlist, IsUpgrade: true)`, replacing the temporary `SingleMedia` entry in-place with the `MediaCollectionViewModel` shortcut.

### In-Queue Mutations & Origin Unlinking

- **Operations that preserve `Origin`**:
  - `Next` / `Previous` track navigation
  - Selecting an existing track in the Play Queue (`PlayMediaMessage(item, existing: true)`)
  - Shuffling / unshuffling (`HandleShuffleModeChanged`)
  - Reordering items within the queue (`MoveItemUp`, `MoveItemDown`, drag-and-drop reorder)
- **Operations that unlink `Origin` (`UnlinkOrigin()` → `_queueSession.Origin = null`)**:
  - `Enqueue` (appending/dropping files into the active queue)
  - `InsertNext` (inserting a track next from the queue panel)
  - `Remove` (removing an item from the active queue)
  - `MessengerExtensions.SendPlayNext` / `MessengerExtensions.SendAddToQueue`

Once `Origin` is unlinked, the active container card immediately clears its active/playing highlight, and every subsequent track navigation records the newly played track individually as `SingleMedia`. Clearing the queue (`ClearQueue()`) resets `ActiveSessionId = Guid.Empty` and `ActiveOrigin = null`.

---

## 🔑 Deduplication & Live Re-Playback Semantics

Every entry computes a deterministic `id` key via `QueueOrigin.GetEntryId()` (`$"{(int)Kind}|{NormalizeLocation(SourceId)}"`):

| `RecentItemKind` | Hash Key (`SourceId`) | Re-Playback Behavior from Home Page |
|---|---|---|
| `SingleMedia` | File path or URL | Resolves cached `MediaViewModel` (reopening picker MRU token if outside libraries) and sends `PlayMediaMessage(media, existing: false)`. |
| `MediaCollection` | Folder path, `.m3u` file path, or URL | Resolves live `StorageFolder` / `StorageFile` (via retained MRU `Token` or path fallback) and parses current live items via `IQueueService`. |
| `Playlist` | `PlaylistViewModel.Id` | Invokes `PlaylistViewModel.PlayCommand` using current live `PlaylistViewModel.Items`. |
| `Album` | `AlbumViewModel.AlbumKey` | Invokes `AlbumViewModel.PlayAlbumCommand` using current live `AlbumViewModel.OrderedSongs`. |
| `Artist` | `ArtistViewModel.ArtistKey` | Invokes `ArtistViewModel.PlayArtistCommand` using current live `ArtistViewModel.OrderedSongs`. |

---

## ⚡ Active & Playing State Synchronization

Only **one** card in the Recent list displays the active playing badge at any time:

- **`AlbumViewModel`, `ArtistViewModel`, `PlaylistViewModel`, and `MediaCollectionViewModel`** each listen to `QueuePlaybackStateChangedMessage` and set:
  - `IsQueueActive = state.SessionId != Guid.Empty && state.CurrentItem is not null && (state.Origin?.Matches(kind, sourceId) ?? false)`
  - `IsPlaying = IsQueueActive && state.IsPlaying`
- **`MediaViewModel`** retains `IsPlaying` for track-level UI (`SongsPage`, `AlbumDetailsPage`, `AllVideosPage`, `PlayQueuePanel`), while `PlayQueueCoordinator` also updates **`MediaViewModel.IsPlayingIndividually`**:
  - `IsPlayingIndividually = _context.IsPlaying && (_queueSession.Origin is null || _queueSession.Origin.TrackItemsIndividually)`
  - `MediaGridViewItemTemplate` binds `IsPlaying="{x:Bind IsPlayingIndividually, Mode=OneWay}"`, and `HomePage.xaml` sets `UseIndividualPlaybackState="True"` on `MediaMenuFlyout`.
  - When a track inside an Album/Artist/Playlist/Folder is playing, only that container card shows the Pause badge (`IsPlayingIndividually` is `false` on standalone `SingleMedia` cards), and clicking a `SingleMedia` card starts it standalone instead of pausing the container queue.

---

## 💾 SQLite Persistence & UWP File Access

### Database Tables (`screenbox.db`)

`recent_entries` and `recent_entry_items` are replaceable cache tables managed in `DatabaseService.Schema.cs` and `DatabaseService.Recent.cs`:

- **`recent_entries`**:
  - `id` (`TEXT PRIMARY KEY`), `kind` (`INTEGER`), `source_id` (`TEXT`), `title` (`TEXT`), `subtitle` (`TEXT`), `token` (`TEXT`), `last_played` (`INTEGER`, UTC ticks).
- **`recent_entry_items`**:
  - `id` (`INTEGER PRIMARY KEY AUTOINCREMENT`), `entry_id` (`TEXT REFERENCES recent_entries(id) ON DELETE CASCADE`), `sort_order` (`INTEGER`), `path` (`TEXT`), `title` (`TEXT`), `media_type` (`INTEGER`), `duration_ticks` (`INTEGER`), `token` (`TEXT`).
  - Populated **only** for `SingleMedia` (1 row). Hydrated at startup via `LEFT JOIN media_records ON media_records.path = recent_entry_items.path` to avoid disk I/O for library files.

### UWP Sandbox Access (`MostRecentlyUsedList`)

Because Screenbox runs in the UWP app container without `broadFileSystemAccess`, files and folders outside the Music/Videos libraries require OS-level capability tokens across app restarts:
- **Single files (`SingleMedia`)**: `RecentMediaCoordinator.ToRawMediaRecord` captures/retains the file's MRU token in `recent_entry_items.token` and `IFileMediaSource.Token`, reopening `StorageFile` via `MostRecentlyUsedList.GetFileAsync(token)` on startup hydration.
- **Folders and playlist files (`MediaCollection`)**: `QueueService` captures the MRU token returned by `IFilesService.AddToRecent(item)` into `QueueOrigin.Token`, which `RecentMediaCoordinator` stores in `recent_entries.token`. On startup hydration, `MediaCollectionViewModel` uses `MostRecentlyUsedList.GetFolderAsync(token)` or `GetFileAsync(token)` to access live folder/playlist contents across sessions.
- SQLite owns the entire recent list history and ordering; legacy Windows MRU migration is not used.
