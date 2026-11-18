# Migrating from TagLib# to TagLibSharp2

This guide helps developers transition from TagLib# (taglib-sharp) to TagLibSharp2.

## Why Migrate?

| Aspect | TagLib# | TagLibSharp2 |
|--------|---------|--------------|
| **License** | LGPL-2.1 | MIT |
| **Target** | .NET Framework, netstandard2.0 | netstandard2.0/2.1, net8.0, net10.0 |
| **Design** | Mutable objects, exceptions | Immutable data, result types |
| **Performance** | Allocations on every read | `Span<T>`, `ArrayPool<T>`, zero-copy parsing |
| **Async** | Not supported | Full async I/O support |
| **Nullability** | None | Full nullable reference types |

## Quick Comparison

### TagLib# (Before)
```csharp
using TagLib;

var file = TagLib.File.Create("song.mp3");
Console.WriteLine(file.Tag.Title);
Console.WriteLine(file.Tag.FirstPerformer);
file.Tag.Title = "New Title";
file.Save();
```

### TagLibSharp2 (After)
```csharp
using TagLibSharp2.Core;

// Auto-detect format
var result = MediaFile.Read("song.mp3");
if (result.IsSuccess)
{
    Console.WriteLine(result.Tag?.Title);
    Console.WriteLine(result.Tag?.Artist);
}

// Or use format-specific for full access
var mp3Result = Mp3File.ReadFromFile("song.mp3");
if (mp3Result.IsSuccess)
{
    var mp3 = mp3Result.File!;
    mp3.Title = "New Title";
    mp3.SaveToFile("song.mp3", File.ReadAllBytes("song.mp3"));
}
```

## Key Differences

### 1. Error Handling: Exceptions vs Result Types

**TagLib#** throws exceptions for invalid files:
```csharp
try
{
    var file = TagLib.File.Create("corrupted.mp3");
}
catch (CorruptFileException ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
```

**TagLibSharp2** returns result objects:
```csharp
var result = Mp3File.ReadFromFile("corrupted.mp3");
if (!result.IsSuccess)
{
    Console.WriteLine($"Error: {result.Error}");
    return;
}
var mp3 = result.File!;
```

### 2. Property Access: Combined vs Specific

**TagLib#** uses a unified `Tag` property:
```csharp
var file = TagLib.File.Create("song.mp3");
string title = file.Tag.Title;           // Combined from all tags
string[] artists = file.Tag.Performers;  // Array property
```

**TagLibSharp2** provides direct access:
```csharp
var result = Mp3File.ReadFromFile("song.mp3");
var mp3 = result.File!;
string? title = mp3.Title;              // From ID3v2 or ID3v1
string[] artists = mp3.Performers;      // Array property
```

### 3. File Operations: In-Place vs Explicit

**TagLib#** modifies files in-place:
```csharp
var file = TagLib.File.Create("song.mp3");
file.Tag.Title = "New Title";
file.Save();  // Modifies original file
```

**TagLibSharp2** requires explicit data handling:
```csharp
var result = Mp3File.ReadFromFile("song.mp3");
var mp3 = result.File!;
mp3.Title = "New Title";

// Read original bytes, render new data, save atomically
var originalData = File.ReadAllBytes("song.mp3");
mp3.SaveToFile("song.mp3", originalData);
```

#### Why SaveToFile Requires Original Bytes

This design is intentional and provides several benefits:

1. **Memory efficiency**: Audio files can be gigabytes. TagLibSharp2 only parses metadata, not audio data. The audio content stays on disk until you're ready to write.

2. **Explicit data flow**: You control when and how the original file is read. This enables:
   - Reading from one location, writing to another
   - Processing files from streams or memory buffers
   - Avoiding double-reads when you already have the bytes

3. **Atomic saves**: The original bytes are combined with updated metadata and written atomically (temp file + rename), ensuring you never lose data on failure.

4. **Format preservation**: Unknown chunks (FLAC APPLICATION blocks, WAV metadata) are preserved from the original bytes.

**Common patterns:**

```csharp
// Pattern 1: Modify in-place (like TagLib#)
var result = Mp3File.ReadFromFile("song.mp3");
var mp3 = result.File!;
mp3.Title = "New Title";
mp3.SaveToFile("song.mp3", File.ReadAllBytes("song.mp3"));

// Pattern 2: Read once, modify, save (more efficient)
var bytes = File.ReadAllBytes("song.mp3");
var result = Mp3File.Read(bytes);
var mp3 = result.File!;
mp3.Title = "New Title";
mp3.SaveToFile("song.mp3", bytes);

// Pattern 3: Copy with modifications
var bytes = File.ReadAllBytes("source.mp3");
var result = Mp3File.Read(bytes);
var mp3 = result.File!;
mp3.Title = "Modified Copy";
mp3.SaveToFile("destination.mp3", bytes);

// Pattern 4: Async with cancellation
var bytes = await File.ReadAllBytesAsync("song.mp3", cancellationToken);
var result = await Mp3File.ReadFromFileAsync("song.mp3", ct: cancellationToken);
var mp3 = result.File!;
mp3.Title = "New Title";
await mp3.SaveToFileAsync("song.mp3", bytes, cancellationToken);
```

### 4. Async Support

**TagLib#** is synchronous only:
```csharp
var file = TagLib.File.Create("song.mp3");  // Blocking
```

**TagLibSharp2** supports async operations:
```csharp
var result = await Mp3File.ReadFromFileAsync("song.mp3", cancellationToken);
if (result.IsSuccess)
{
    var mp3 = result.File!;
    await mp3.SaveToFileAsync("song.mp3", originalData, cancellationToken);
}
```

## Property Mapping

### Common Properties

| TagLib# | TagLibSharp2 | Notes |
|---------|--------------|-------|
| `Tag.Title` | `Title` | Same |
| `Tag.Performers` | `Performers` | Same (array) |
| `Tag.FirstPerformer` | `Performers[0]` | Use array indexer |
| `Tag.Album` | `Album` | Same |
| `Tag.AlbumArtists` | `AlbumArtists` | Same (array) |
| `Tag.FirstAlbumArtist` | `AlbumArtists[0]` | Use array indexer |
| `Tag.Composers` | `Composers` | Same (array) |
| `Tag.FirstComposer` | `Composers[0]` | Use array indexer |
| `Tag.Genres` | `Genres` | Same (array) |
| `Tag.FirstGenre` | `Genres[0]` | Use array indexer |
| `Tag.Year` | `Year` | Same |
| `Tag.Track` | `TrackNumber` | Renamed |
| `Tag.TrackCount` | `TotalTracks` | Renamed |
| `Tag.Disc` | `DiscNumber` | Renamed |
| `Tag.DiscCount` | `TotalDiscs` | Renamed |
| `Tag.Comment` | `Comment` | Same |
| `Tag.Lyrics` | `Lyrics` | Same |
| `Tag.Conductor` | `Conductor` | Same |
| `Tag.Copyright` | `Copyright` | Same |
| `Tag.BeatsPerMinute` | `BeatsPerMinute` | Same |
| `Tag.Pictures` | `Pictures` | Returns `IPicture[]` |

### MusicBrainz Properties

| TagLib# | TagLibSharp2 |
|---------|--------------|
| `Tag.MusicBrainzTrackId` | `MusicBrainzTrackId` |
| `Tag.MusicBrainzReleaseId` | `MusicBrainzReleaseId` |
| `Tag.MusicBrainzArtistId` | `MusicBrainzArtistId` |
| `Tag.MusicBrainzReleaseGroupId` | `MusicBrainzReleaseGroupId` |
| `Tag.MusicBrainzReleaseArtistId` | `MusicBrainzAlbumArtistId` |

### ReplayGain Properties

| TagLib# | TagLibSharp2 |
|---------|--------------|
| `Tag.ReplayGainTrackGain` | `ReplayGainTrackGain` |
| `Tag.ReplayGainTrackPeak` | `ReplayGainTrackPeak` |
| `Tag.ReplayGainAlbumGain` | `ReplayGainAlbumGain` |
| `Tag.ReplayGainAlbumPeak` | `ReplayGainAlbumPeak` |

## Type Changes

### Pictures

**TagLib#**:
```csharp
IPicture picture = file.Tag.Pictures[0];
byte[] data = picture.Data.Data;
string mime = picture.MimeType;
PictureType type = picture.Type;
```

**TagLibSharp2**:
```csharp
IPicture picture = mp3.Pictures[0];
BinaryData data = picture.PictureData;
byte[] bytes = data.ToArray();          // Or use Span<T>
string mime = picture.MimeType;
PictureType type = picture.PictureType; // Property renamed
```

### TagTypes

**TagLib#**:
```csharp
TagTypes types = file.TagTypes;
bool hasId3v2 = (types & TagTypes.Id3v2) != 0;
```

**TagLibSharp2**:
```csharp
TagTypes type = mp3.TagType;            // Single tag type per object
bool isId3v2 = type == TagTypes.Id3v2;

// Or check available tags
bool hasId3v2 = mp3.Id3v2Tag is not null;
bool hasId3v1 = mp3.Id3v1Tag is not null;
```

## Format-Specific Access

### MP3 Files

**TagLib#**:
```csharp
var file = TagLib.File.Create("song.mp3") as TagLib.Mpeg.AudioFile;
var id3v2 = file?.GetTag(TagTypes.Id3v2) as TagLib.Id3v2.Tag;
var id3v1 = file?.GetTag(TagTypes.Id3v1) as TagLib.Id3v1.Tag;
```

**TagLibSharp2**:
```csharp
var result = Mp3File.ReadFromFile("song.mp3");
var mp3 = result.File!;
var id3v2 = mp3.Id3v2Tag;  // Nullable, may be null
var id3v1 = mp3.Id3v1Tag;  // Nullable, may be null
```

### FLAC Files

**TagLib#**:
```csharp
var file = TagLib.File.Create("song.flac") as TagLib.Flac.File;
var xiph = file?.GetTag(TagTypes.Xiph) as TagLib.Ogg.XiphComment;
```

**TagLibSharp2**:
```csharp
var result = FlacFile.ReadFromFile("song.flac");
var flac = result.File!;
var vorbis = flac.VorbisComment;  // Direct access
```

### Ogg Vorbis Files

**TagLib#**:
```csharp
var file = TagLib.File.Create("song.ogg") as TagLib.Ogg.File;
var xiph = file?.Tag as TagLib.Ogg.XiphComment;
```

**TagLibSharp2**:
```csharp
var result = OggVorbisFile.ReadFromFile("song.ogg");
var ogg = result.File!;
var vorbis = ogg.VorbisComment;  // Direct access
```

## Low-Level Frame Access

### ID3v2 Frames

**TagLib#**:
```csharp
var id3v2 = file.GetTag(TagTypes.Id3v2) as TagLib.Id3v2.Tag;
var frame = id3v2?.GetFrames<TextInformationFrame>("TIT2").FirstOrDefault();
string? value = frame?.Text?.FirstOrDefault();
```

**TagLibSharp2**:
```csharp
var id3v2 = mp3.Id3v2Tag;
string? value = id3v2?.GetTextFrame("TIT2");

// Or get all values for multi-value frames
IReadOnlyList<string> values = id3v2?.GetTextFrameValues("TPE1") ?? [];
```

### User-Defined Frames (TXXX)

**TagLib#**:
```csharp
var txxx = id3v2?.GetFrames<UserTextInformationFrame>()
    .FirstOrDefault(f => f.Description == "REPLAYGAIN_TRACK_GAIN");
string? gain = txxx?.Text?.FirstOrDefault();
```

**TagLibSharp2**:
```csharp
string? gain = id3v2?.GetUserText("REPLAYGAIN_TRACK_GAIN");
id3v2?.SetUserText("REPLAYGAIN_TRACK_GAIN", "-6.5 dB");
```

## Binary Data Handling

**TagLib#** uses `ByteVector`:
```csharp
ByteVector data = file.Tag.Pictures[0].Data;
byte[] bytes = data.Data;
```

**TagLibSharp2** uses `BinaryData` with `Span<T>` support:
```csharp
BinaryData data = mp3.Pictures[0].PictureData;

// Multiple access patterns
byte[] bytes = data.ToArray();              // Allocates new array
ReadOnlySpan<byte> span = data.Span;        // Zero-copy access
ReadOnlyMemory<byte> memory = data.Memory;  // For async operations
```

## Creating Pictures

**TagLib#**:
```csharp
var picture = new Picture(imageBytes)
{
    Type = PictureType.FrontCover,
    MimeType = "image/jpeg",
    Description = "Album Art"
};
file.Tag.Pictures = new IPicture[] { picture };
file.Save();
```

**TagLibSharp2** (ID3v2):
```csharp
var picture = new PictureFrame(
    PictureType.FrontCover,
    "image/jpeg",
    "Album Art",
    new BinaryData(imageBytes));

mp3.Id3v2Tag!.AddPicture(picture);
mp3.SaveToFile("song.mp3", originalData);
```

**TagLibSharp2** (FLAC/Vorbis):
```csharp
var picture = new FlacPicture(
    PictureType.FrontCover,
    "image/jpeg",
    "Album Art",
    width: 500,
    height: 500,
    colorDepth: 24,
    colorCount: 0,
    new BinaryData(imageBytes));

flac.VorbisComment.AddPicture(picture);
```

## Common Migration Patterns

### Pattern 1: Simple Tag Reading
```csharp
// TagLib#
string? GetTitle(string path)
{
    try
    {
        using var file = TagLib.File.Create(path);
        return file.Tag.Title;
    }
    catch { return null; }
}

// TagLibSharp2
string? GetTitle(string path)
{
    var result = Mp3File.ReadFromFile(path);
    return result.IsSuccess ? result.File!.Title : null;
}
```

### Pattern 2: Tag Writing
```csharp
// TagLib#
void SetTitle(string path, string title)
{
    using var file = TagLib.File.Create(path);
    file.Tag.Title = title;
    file.Save();
}

// TagLibSharp2
void SetTitle(string path, string title)
{
    var result = Mp3File.ReadFromFile(path);
    if (!result.IsSuccess) return;

    var mp3 = result.File!;
    mp3.Title = title;
    mp3.SaveToFile(path, File.ReadAllBytes(path));
}
```

### Pattern 3: Async Processing
```csharp
// TagLibSharp2 only - no TagLib# equivalent
async Task<string?> GetTitleAsync(string path, CancellationToken ct = default)
{
    var result = await Mp3File.ReadFromFileAsync(path, ct: ct);
    return result.IsSuccess ? result.File!.Title : null;
}
```

## What's Not Yet Supported

TagLibSharp2 is under active development. These TagLib# features are planned but not yet implemented:

- **Formats**: Speex, TrueAudio, Tracker formats (MOD/S3M/IT/XM)
- **Features**: Matroska/WebM container, MPEG-4 video files

## Format-Specific Examples

### DSD Files (DSF/DFF)

DSD (Direct Stream Digital) is a high-resolution audio format. TagLibSharp2 supports both DSF and DFF container formats.

```csharp
// DSF files store ID3v2 tags at the end of the file
var dsfResult = DsfFile.ReadFromFile("audio.dsf");
if (dsfResult.IsSuccess)
{
    var dsf = dsfResult.File!;

    // Access audio properties
    Console.WriteLine($"Duration: {dsf.Duration}");
    Console.WriteLine($"Sample Rate: {dsf.SampleRate} Hz");
    Console.WriteLine($"DSD Rate: {dsf.DsdRate}"); // DSD64, DSD128, etc.

    // Access/modify metadata via ID3v2
    Console.WriteLine($"Title: {dsf.Id3v2Tag?.Title}");
    dsf.EnsureId3v2Tag().Title = "New Title";
    dsf.SaveToFile("audio.dsf");
}

// DFF files also support ID3v2 tags (unofficial extension)
var dffResult = DffFile.ReadFromFile("audio.dff");
if (dffResult.IsSuccess)
{
    var dff = dffResult.File!;
    Console.WriteLine($"Format Version: {dff.FormatVersionMajor}.{dff.FormatVersionMinor}");
    Console.WriteLine($"Compressed: {dff.IsCompressed}"); // DST compression

    dff.EnsureId3v2Tag().Artist = "Artist Name";
    dff.SaveToFile("audio.dff");
}
```

### WavPack Files

WavPack is a lossless audio format that uses APE tags for metadata.

```csharp
var result = WavPackFile.ReadFromFile("audio.wv");
if (result.IsSuccess)
{
    var wv = result.File!;

    // Audio properties
    Console.WriteLine($"Duration: {wv.Duration}");
    Console.WriteLine($"Sample Rate: {wv.SampleRate}");
    Console.WriteLine($"Bits Per Sample: {wv.BitsPerSample}");

    // Access/modify metadata via APE tag
    Console.WriteLine($"Title: {wv.ApeTag?.Title}");
    wv.EnsureApeTag().Album = "Album Name";

    // APE tags support ReplayGain natively
    wv.ApeTag!.ReplayGainTrackGain = "-6.50 dB";

    var rendered = wv.Render(originalData);
    wv.SaveToFile("audio.wv");
}
```

### Monkey's Audio Files

Monkey's Audio (.ape) is a lossless audio format that also uses APE tags.

```csharp
var result = MonkeysAudioFile.ReadFromFile("audio.ape");
if (result.IsSuccess)
{
    var ape = result.File!;

    // Audio properties
    Console.WriteLine($"Duration: {ape.Duration}");
    Console.WriteLine($"Compression Level: {ape.CompressionLevel}");
    Console.WriteLine($"Version: {ape.Version}");

    // Metadata via APE tag
    ape.EnsureApeTag().Title = "Track Title";
    ape.EnsureApeTag().Artist = "Artist";

    var rendered = ape.Render(originalData);
    ape.SaveToFile("audio.ape");
}
```

### Musepack Files

Musepack (.mpc) is a lossy audio format with excellent quality at low bitrates. It uses APE tags.

```csharp
var result = MusepackFile.ReadFromFile("audio.mpc");
if (result.IsSuccess)
{
    var mpc = result.File!;

    // Audio properties
    Console.WriteLine($"Duration: {mpc.Duration}");
    Console.WriteLine($"Stream Version: {mpc.StreamVersion}"); // SV7 or SV8
    Console.WriteLine($"Average Bitrate: {mpc.AverageBitrate} kbps");

    // Metadata via APE tag
    mpc.EnsureApeTag().Title = "Track Title";
    mpc.EnsureApeTag().MusicBrainzTrackId = "guid-here";

    var rendered = mpc.Render(originalData);
    mpc.SaveToFile("audio.mpc");
}
```

### Ogg FLAC Files

Ogg FLAC wraps FLAC audio in an Ogg container, using Vorbis Comments for metadata.

```csharp
var result = OggFlacFile.ReadFromFile("audio.oga");
if (result.IsSuccess)
{
    var oggFlac = result.File!;

    // FLAC audio properties
    Console.WriteLine($"Duration: {oggFlac.Duration}");
    Console.WriteLine($"Sample Rate: {oggFlac.SampleRate}");
    Console.WriteLine($"Bits Per Sample: {oggFlac.BitsPerSample}");

    // Metadata via Vorbis Comment
    oggFlac.EnsureVorbisComment().Title = "Track Title";
    oggFlac.VorbisComment!.ReplayGainTrackGain = "-6.50 dB";

    var rendered = oggFlac.Render(originalData);
    oggFlac.SaveToFile("audio.oga");
}
```

### ASF/WMA Files

ASF (Advanced Systems Format) is Microsoft's container format for Windows Media Audio.

```csharp
var result = AsfFile.ReadFromFile("audio.wma");
if (result.IsSuccess)
{
    var wma = result.File!;

    // Audio properties
    Console.WriteLine($"Duration: {wma.Duration}");
    Console.WriteLine($"Sample Rate: {wma.SampleRate}");
    Console.WriteLine($"Bitrate: {wma.Bitrate} kbps");
    Console.WriteLine($"Channels: {wma.Channels}");

    // Content Description (Title, Artist, Copyright)
    Console.WriteLine($"Title: {wma.Title}");
    Console.WriteLine($"Artist: {wma.Artist}");

    // Extended Content Description (Album, Year, Genre, etc.)
    Console.WriteLine($"Album: {wma.Album}");
    Console.WriteLine($"Year: {wma.Tag.Year}");
    Console.WriteLine($"Genre: {wma.Tag.Genre}");
    Console.WriteLine($"Track: {wma.Tag.Track}");

    // Modify metadata
    wma.Title = "New Title";
    wma.Artist = "New Artist";
    wma.Album = "New Album";
    wma.Tag.Year = "2026";
    wma.Tag.Genre = "Rock";

    wma.SaveToFile("audio.wma");
}
```

## New Features Not in TagLib#

TagLibSharp2 includes several features not available in TagLib#:

- **Automatic format detection**: `MediaFile.Read()` auto-detects 15 formats: MP3, FLAC, Ogg Vorbis, Ogg Opus, Ogg FLAC, WAV, AIFF, MP4/M4A, ASF/WMA, DSF, DFF, WavPack, Monkey's Audio, and Musepack
- **DSD audio support**: Native DSF and DFF format support for high-resolution DSD audio
- **Lossless format breadth**: WavPack and Monkey's Audio support with APE tag handling
- **Async I/O**: Full async support throughout the API with cancellation token support
- **Result-based error handling**: No exceptions for invalid files, predictable control flow

## Getting Help

- [GitHub Issues](https://github.com/decriptor/TagLibSharp2/issues)
- [API Documentation](CORE-TYPES.md)
- [Examples](../examples/)
