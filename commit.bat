@echo off
cd /d C:\Users\tung7\source\repos\Screenbox

git add -A

git commit -m "refactor(cast): move MediaStreamingService into Screenbox.Casting

Move the HTTP media proxy server into the Screenbox.Casting library so that
all casting-related infrastructure is colocated. This ensures media streaming
is treated as a critical dependency of the casting system.

Changes:
- Add IMediaStreamingService interface to Screenbox.Casting.Abstractions
- Move MediaStreamingService implementation to Screenbox.Casting.Services
- Update CastService to use the new Casting-scoped API (StartStreamAsync(source))
- Update ServiceHelpers DI registration to reference Casting namespaces
- Remove old MediaStreamingService types from Screenbox.Core
- Update both Screenbox.Casting.csproj and Screenbox.Core.csproj

This allows the Screenbox.Casting library to be self-contained with all
necessary infrastructure for both device discovery and media delivery.

Co-authored-by: Copilot ^<223556219+Copilot@users.noreply.github.com^>"

git log --oneline -1
