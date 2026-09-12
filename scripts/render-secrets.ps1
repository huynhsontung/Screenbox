<#
    .SYNOPSIS
    Performs a string replacement in the Secrets.cs file to embed the Sentry DSN.

    .DESCRIPTION
    The render-secrets.ps1 script updates the Secrets.cs file with the
    provided DSN value. If no DSN is provided, an empty string is used,
    which disables Sentry in the app.

    .PARAMETER SentryDsn
    Specifies the Sentry Data Source Name (DSN) to embed in the Secrets.cs file.
    The default is an empty string.

    .EXAMPLE
    PS> ./render-secrets.ps1 -SentryDsn "https://examplePublicKey@o123.ingest.sentry.io/456"

    .EXAMPLE
    PS> ./render-secrets.ps1

    .NOTES
    This script is intended to be used in CI pipelines (e.g., GitHub Actions)
    where the DSN is provided via environment variables.
#>

[CmdletBinding()]
param (
    [string]$SentryDsn = ""
)

$template = @"
namespace Screenbox;

internal static class Secrets
{
    public const string SentryDsn = "$SentryDsn";
}
"@

Set-Content -Path "Screenbox/Secrets.cs" -Value $template
