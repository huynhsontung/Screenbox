<#
.SYNOPSIS
    Converts a Sizoscope diff report into a Markdown file.

.DESCRIPTION
    The ConvertTo-SizoscopeSummary script parses a Sizoscope diff file, then writes
    a Markdown file with the total delta, entry counts, and per-entry changes.

.PARAMETER InputPath
    Specifies the path to the Sizoscope diff file to be converted.

.PARAMETER OutputPath
    Specifies the path to the Markdown output file.

.EXAMPLE
    PS> ./scripts/ConvertTo-SizoscopeSummary.ps1 -InputPath .\artifacts\sizoscope-report.txt

    Converts the report and writes the summary to sizoscope-summary.md in the repository root.

.EXAMPLE
    PS> ./scripts/ConvertTo-SizoscopeSummary.ps1 -InputPath .\artifacts\sizoscope-report.txt -OutputPath .\artifacts\summary.md

    Converts the report and writes the summary to the specified path.

.NOTES
    The script expects a valid Sizoscope diff report.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$InputPath,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$OutputPath
)

function Get-DeltaEmoji {
    param(
        [double]$Value
    )

    if ($Value -eq 0) { return '🟰' }
    if ($Value -lt 0) { return '📉' }
    return '📈'
}

function Add-SizeRow {
    param(
        [string]$Line,
        [System.Collections.Generic.List[string]]$Rows
    )

    # Parse summary rows in the form:
    #   "New frozen objects: +1.0 kB"
    #   "New blobs: +0.8 kB"
    # Convert them to a markdown table row with a normalized name.
    if ($Line.Contains(':')) {
        $parts = $Line -split ':', 2
        $label = $parts[0].Trim()
        $name = if ($label.StartsWith('New ')) {
            $label.Substring(4)
        } else {
            $label.Substring(8)
        }

        $name = $name.Substring(0, 1).ToUpperInvariant() + $name.Substring(1)
        $change = $parts[1].Trim()
        $Rows.Add("| $name | $change |")
        return
    }

    # Parse assembly rows in the form:
    #   "+15.5 kB Microsoft.Windows.UI.Xaml"
    # Split into size change ("+15.5 kB") and item name ("Microsoft.Windows.UI.Xaml")
    # and add a markdown table row.
    $parts = $Line -split '\s+', 3
    if ($parts.Count -ne 3) {
        throw "The $Line size row could not be parsed."
    }

    $Rows.Add("| $($parts[2]) | $($parts[0]) $($parts[1]) |")
}

function Render-Section {
    param(
        [string]$Heading,
        [System.Collections.Generic.List[string]]$Rows
    )

    $lines = @(
        '',
        "### $Heading",
        ''
    )

    if ($Rows.Count -gt 0) {
        $lines += @(
            '| Assembly | Change |',
            '|----------|----------:|'
        )
        $lines += $Rows
    } else {
        $lines += '_No entries were found._'
    }

    return $lines
}

if (-not (Test-Path -LiteralPath $InputPath -PathType Leaf)) {
    throw "Input file not found: $InputPath"
}

$report = Get-Content -LiteralPath $InputPath -Raw -Encoding UTF8
$lines = $report -split "`r?`n"

$currentSection = $null
$growthRows = [System.Collections.Generic.List[string]]::new()
$reductionRows = [System.Collections.Generic.List[string]]::new()

$totalLine = $lines |
    Where-Object { $_.Trim().StartsWith('Total accounted size difference:', [System.StringComparison]::OrdinalIgnoreCase) } |
    Select-Object -First 1

if ($null -eq $totalLine) {
    throw "The total size delta could not be found in the input file."
}

$totalParts = $totalLine.Substring($totalLine.IndexOf(':') + 1).Trim() -split '\s+', 2
if ($totalParts.Count -ne 2) {
    throw "The total size delta could not be parsed from the input file."
}

$totalValueText = $totalParts[0]
$totalUnit = $totalParts[1]
$totalValue = 0.0
if (-not [double]::TryParse(
        $totalValueText,
        [System.Globalization.NumberStyles]::Float,
        [System.Globalization.CultureInfo]::InvariantCulture,
        [ref]$totalValue)) {
    throw "The total size delta could not be parsed from the input file."
}

$totalDeltaEmoji = Get-DeltaEmoji -Value $totalValue
$absoluteTotalValueText = [Math]::Abs($totalValue).ToString([System.Globalization.CultureInfo]::InvariantCulture)

if ($totalValue -gt 0) {
    $totalDelta = "$totalDeltaEmoji +$totalValueText $totalUnit"
} elseif ($totalValue -lt 0) {
    $totalDelta = "$totalDeltaEmoji -$absoluteTotalValueText $totalUnit"
} else {
    $totalDelta = "$totalDeltaEmoji $totalValueText $totalUnit"
}

foreach ($line in $lines) {
    $trimmed = $line.Trim()

    if ($trimmed.StartsWith('===') -and $trimmed.EndsWith('===') -and $trimmed.Length -ge 6) {
        $currentSection = $trimmed.Substring(3, $trimmed.Length - 6).Trim()
        continue
    }

    if ([string]::IsNullOrWhiteSpace($trimmed)) {
        continue
    }

    if ($trimmed.StartsWith('New frozen objects:', [System.StringComparison]::OrdinalIgnoreCase) -or
        $trimmed.StartsWith('New blobs:', [System.StringComparison]::OrdinalIgnoreCase)) {
        Add-SizeRow -Line $trimmed -Rows $growthRows
        continue
    }

    if ($trimmed.StartsWith('Removed frozen objects:', [System.StringComparison]::OrdinalIgnoreCase) -or
        $trimmed.StartsWith('Removed blobs:', [System.StringComparison]::OrdinalIgnoreCase)) {
        Add-SizeRow -Line $trimmed -Rows $reductionRows
        continue
    }

    switch ($currentSection) {
        'New / Grown' {
            Add-SizeRow -Line $trimmed -Rows $growthRows
        }
        'Removed / Shrunk' {
            Add-SizeRow -Line $trimmed -Rows $reductionRows
        }
    }
}

$markdown = @(
    '# 📊 Sizoscope Report',
    '',
    '## Summary',
    '',
    '| Metric | Value |',
    '|--------|--------:|',
    "| Total size delta | $totalDelta |",
    "| New / Grown entries | $($growthRows.Count) |",
    "| Removed / Shrunk entries | $($reductionRows.Count) |"
)

$markdown += Render-Section -Heading 'Growth' -Rows $growthRows
$markdown += Render-Section -Heading 'Reduction' -Rows $reductionRows

$markdownText = $markdown -join "`r`n"

$directory = Split-Path -Path $OutputPath -Parent
if ($directory -and -not (Test-Path -LiteralPath $directory)) {
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
}

[System.IO.File]::WriteAllText($OutputPath, $markdownText, [System.Text.UTF8Encoding]::new($false))
$markdownText
