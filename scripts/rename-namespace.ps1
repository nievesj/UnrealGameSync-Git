<#
.SYNOPSIS
    Rename UGSGit <-> SourceGit namespaces in .cs and .axaml files.
.PARAMETER Direction
    "Forward" = SourceGit->UGSGit, "Reverse" = UGSGit->SourceGit
.PARAMETER DryRun
    If set, lists files that would be changed without modifying them.
#>
param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("Forward", "Reverse")]
    [string]$Direction,
    [switch]$DryRun
)
$ErrorActionPreference = "Stop"
if ($Direction -eq "Reverse") { $From = "UGSGit"; $To = "SourceGit" } else { $From = "SourceGit"; $To = "UGSGit" }
Write-Host "Direction: $From -> $To" -ForegroundColor Cyan
if ($DryRun) { Write-Host "DRY RUN MODE" -ForegroundColor Yellow }

# Patterns to rename
$patterns = @(
    @{ Pattern = "namespace $From"; Replace = "namespace $To"; Ext = "cs" },
    @{ Pattern = "using $From\."; Replace = "using $To."; Ext = "cs" },
    @{ Pattern = "x:Class=`"$From\."; Replace = "x:Class=`"$To."; Ext = "axaml" },
    @{ Pattern = "using:$From\."; Replace = "using:$To."; Ext = "axaml" },
    @{ Pattern = "using:$From`""; Replace = "using:$To`""; Ext = "axaml" },
    @{ Pattern = "avares://$From"; Replace = "avares://$To"; Ext = "axaml" },
    @{ Pattern = "fonts:$From#"; Replace = "fonts:$To#"; Ext = "cs" },
    @{ Pattern = "fonts:$From#"; Replace = "fonts:$To#"; Ext = "axaml" }
)

$files = Get-ChildItem -Path "src" -Include "*.cs","*.axaml" -Recurse | Where-Object { $_.FullName -notmatch "\\libs\\" }
$total = 0

foreach ($file in $files) {
    $content = [System.IO.File]::ReadAllText($file.FullName)
    $orig = $content
    foreach ($p in $patterns) {
        if ($file.Extension.TrimStart('.') -ne $p.Ext) { continue }
        $content = $content -replace $p.Pattern, $p.Replace
    }
    # Restore protected patterns (plugin namespaces stay as UGSGit.*)
    if ($Direction -eq "Reverse") {
        $content = $content -replace "using SourceGit\.PluginAbstractions", "using UGSGit.PluginAbstractions"
        $content = $content -replace "using SourceGit\.Plugins\.", "using UGSGit.Plugins."
        $content = $content -replace "x:Class=`"SourceGit\.Plugins\.", "x:Class=`"UGSGit.Plugins."
        $content = $content -replace "avares://SourceGit\.Plugins\.", "avares://UGSGit.Plugins."
        $content = $content -replace "avares://SourceGit\.PluginAbstractions", "avares://UGSGit.PluginAbstractions"
        $content = $content -replace "new Plugins\.HelloWorld\.", "new UGSGit.Plugins.HelloWorld."
        $content = $content -replace "new Plugins\.UnrealSync\.", "new UGSGit.Plugins.UnrealSync."
        $content = $content -replace "PluginAbstractions\.CommitAnnotation", "UGSGit.PluginAbstractions.CommitAnnotation"
    }
    if ($content -ne $orig) {
        $total++
        if ($DryRun) { Write-Host "  $file" -ForegroundColor Gray }
        else {
            $bytes = [System.IO.File]::ReadAllBytes($file.FullName)
            $hasBOM = ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)
            $enc = [System.Text.UTF8Encoding]::new($hasBOM)
            [System.IO.File]::WriteAllText($file.FullName, $content, $enc)
        }
    }
}
Write-Host "`nTotal files modified: $total" -ForegroundColor Green