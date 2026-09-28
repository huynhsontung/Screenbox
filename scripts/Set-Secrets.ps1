<#
.SYNOPSIS
    Sets the Sentry DSN value in the application's Secrets.cs file.

.DESCRIPTION
    The Set-Secrets.ps1 script updates the Secrets.cs file by inserting the
    provided Sentry DSN string.

.PARAMETER SentryDsn
    Specifies the Sentry Data Source Name (DSN) to embed in the Secrets.cs file.

    If the parameter is not specified, an empty string is used, disabling Sentry
    in the application.

.EXAMPLE
    PS> ./Set-Secrets.ps1 -SentryDsn "https://examplePublicKey@o123.ingest.sentry.io/456"

.EXAMPLE
    PS> ./Set-Secrets.ps1

.NOTES
    The script is intended to be used in CI pipelines (e.g., GitHub Actions)
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
