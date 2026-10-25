<#
.SYNOPSIS
    Measures what Horror AI Manager costs in real Play Mode and writes Tools/Output/benchmark/benchmark.md.
.DESCRIPTION
    Plays two scenarios with 50 NavMesh enemies perceiving one moving target, in a lit scene (8 lamps and a
    flashlight) and in a dark one, and reports the main-thread time of the Horror AI update per frame (perception,
    behavior and NavMesh destinations), raycasts, perceptions and garbage per frame. Fails if the average reaches
    0.1 ms or if any garbage is allocated.
.EXAMPLE
    ./Tools/Benchmark.ps1
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'HorrorAI.Tools.psm1') -Force

$output = Get-OutputDirectory 'benchmark'
$report = Join-Path $output 'benchmark.md'
$logFile = Join-Path (Get-OutputDirectory 'logs') 'benchmark.log'
Remove-Item $report -ErrorAction SilentlyContinue

Write-Host "Benchmarking with Unity $(Get-UnityVersion). This takes about a minute..."
$exitCode = Invoke-Unity -Arguments @('-executeMethod', 'Nekuzaky.HorrorAI.Dev.Benchmark.Run') -LogFile $logFile

if (-not (Test-Path $report)) {
    Get-CompilerMessages -LogFile $logFile | ForEach-Object { Write-Host $_ -ForegroundColor Red }
    Write-Host "No report was written (exit code $exitCode). Log: $logFile" -ForegroundColor Red
    exit 1
}
Get-Content $report | Write-Host
$rows = Get-Content $report | Where-Object { $_ -match '^\| 50 enemies' }
$failed = $false
foreach ($row in $rows) {
    $cells = $row.Split('|') | ForEach-Object { $_.Trim() }
    $average = [double]::Parse(($cells[2].Split('/')[0].Trim()), [Globalization.CultureInfo]::InvariantCulture)
    $garbage = [int]($cells[5] -replace '[^0-9]', '')
    if ($average -ge 0.1 -or $garbage -ne 0) { $failed = $true }
}
if ($rows.Count -eq 0 -or $failed) {
    Write-Host 'Benchmark target missed: 0.1 ms average and 0 B per frame.' -ForegroundColor Red
    exit 1
}
Write-Host 'Benchmark target met: under 0.1 ms per frame on average and 0 B of garbage.' -ForegroundColor Green