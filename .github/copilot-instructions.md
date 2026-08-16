# GitHub instructions

- Use conventional commit format for clear and concise commit messages.
- Use conventional prefixes for pull request titles to summarize changes effectively.
- Reference relevant issues or user stories in the pull requests.
- When describing changes, include the context, motivation, and impact of the changes. Do not just list the changes made.

## Build and test

- Build `Screenbox.Core`: `msbuild Screenbox.Core/Screenbox.Core.csproj /t:Build /p:Configuration=Debug /p:Platform=x64 /m`
- Restore the app project before building it if needed: `msbuild Screenbox/Screenbox.csproj /p:Configuration=Debug /p:Platform=x64 /t:Restore /m`
- Build the app project: `msbuild Screenbox/Screenbox.csproj /p:Configuration=Debug /p:Platform=x64 /t:Build /m`
- Run the full automated test suite currently in the repo: `dotnet test Screenbox.Core.Tests/Screenbox.Core.Tests.csproj -v minimal`
- Run DatabaseService-focused tests: `dotnet test Screenbox.Core.Tests/Screenbox.Core.Tests.csproj --filter "FullyQualifiedName~DatabaseServiceTests" -v n`

## Persistence boundaries

- Treat `library_folders`, `media_records`, and `playback_progress` as rebuildable cache data.
- Treat `playlists` and `playlist_items` as durable user data and preserve them during schema changes or recovery work.
- Prefer per-table recovery or migration over recreating the whole database file when playlist data is involved.
- For library cache refreshes, prefer replacing the cached rows for the affected media type over incremental stale-record cleanup logic unless there is a clear performance need.
