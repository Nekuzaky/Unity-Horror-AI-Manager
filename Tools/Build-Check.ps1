<#
.SYNOPSIS
    Builds the demo for Windows and proves Horror AI Manager works in a real player, without editor code and without garbage.
.DESCRIPTION
    Checks that:
    - the build succeeds;
    - the player holds the runtime assembly only: no editor, test, development or integration assembly;
    - the player runs the demo for a few seconds (batch mode, no graphics) without any exception;
    - a probe compiled only into this build (Assets/Dev/BuildProbe, HORROR_AI_BUILD_PROBE) makes a noise next to the
      enemy, and the enemy reacts: at least one state change is logged;
    - the Horror AI update allocated 0 B in the player once warmed up.
.EXAMPLE
    ./Tools/Build-Check.ps1
#>
[CmdletBinding()]
param([int] $TimeoutSeconds = 60)

$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'HorrorAI.Tools.psm1') -Force

$buildDirectory = Join-Path (Get-OutputDirectory 'build') ''
Remove-Item (Join-Path $buildDirectory '*') -Recurse -Force -ErrorAction SilentlyContinue
$logFile = Join-Path (Get-OutputDirectory 'logs') 'build-check.log'

Write-Host "Building the demo player with Unity $(Get-UnityVersion)..."
$exitCode = Invoke-Unity -Arguments @('-executeMethod', 'Nekuzaky.HorrorAI.Dev.BuildCheck.Run') -LogFile $logFile
Select-String -Path $logFile -Pattern '\[BuildCheck\]' | ForEach-Object { Write-Host ('  ' + $_.Line.Trim()) }
if ($exitCode -ne 0) {
    Get-CompilerMessages -LogFile $logFile | ForEach-Object { Write-Host $_ -ForegroundColor Red }
    Write-Host "Build failed. Log: $logFile" -ForegroundColor Red
    exit 1
}

$managed = Join-Path $buildDirectory 'HorrorAIDemo_Data\Managed'
$problems = New-Object System.Collections.Generic.List[string]
$ours = @(Get-ChildItem $managed -Filter 'Nekuzaky.HorrorAI*.dll')
foreach ($assembly in $ours) { Write-Host ("  in the player: {0} ({1:N1} KB)" -f $assembly.Name, ($assembly.Length / 1KB)) }
if (-not ($ours | Where-Object { $_.Name -eq 'Nekuzaky.HorrorAI.dll' })) { $problems.Add('The runtime assembly is missing from the player.') }
if ($ours | Where-Object { $_.Name -like '*.Editor.dll' -or $_.Name -like '*Tests*' -or $_.Name -like '*Dev*' }) { $problems.Add('An editor, test or development assembly is in the player.') }
if ($ours | Where-Object { $_.Name -like '*Resonance*' -or $_.Name -like '*Hindsight*' }) { $problems.Add('An integration assembly is in the player although its package is not installed.') }

Write-Host 'Running the player (batch mode, no graphics)...'
$playerLog = Join-Path (Get-OutputDirectory 'logs') 'build-check-player.log'
$player = Start-Process -FilePath (Join-Path $buildDirectory 'HorrorAIDemo.exe') -ArgumentList @('-batchmode', '-nographics', '-logFile', "`"$playerLog`"") -PassThru -WindowStyle Hidden
if (-not $player.WaitForExit($TimeoutSeconds * 1000)) { Stop-Process -Id $player.Id -Force; $problems.Add("The player did not quit within $TimeoutSeconds s.") }
Select-String -Path $playerLog -Pattern '\[BuildProbe\]' | ForEach-Object { Write-Host ('  player: ' + $_.Line.Trim()) }

$exceptions = @(Select-String -Path $playerLog -Pattern 'Exception')
if ($exceptions.Count -gt 0) { $problems.Add("The player logged $($exceptions.Count) exception line(s). Log: $playerLog") }
$summary = Select-String -Path $playerLog -Pattern '\[BuildProbe\] summary: enemies=(\d+) transitions=(\d+) frames=(\d+) averageMs=([\d.]+) allocatedBytes=(\d+)' | Select-Object -First 1
if (-not $summary) {
    $problems.Add("The probe did not report. Log: $playerLog")
} else {
    $groups = $summary.Matches[0].Groups
    if ([int]$groups[1].Value -lt 1) { $problems.Add('No enemy was running in the player.') }
    if ([int]$groups[2].Value -lt 1) { $problems.Add('The enemy did not react to the noise in the player.') }
    if ([long]$groups[5].Value -ne 0) { $problems.Add("The Horror AI update allocated $($groups[5].Value) B in the player.") }
}

if ($problems.Count -gt 0) {
    $problems | ForEach-Object { Write-Host $_ -ForegroundColor Red }
    exit 1
}
Write-Host 'The demo runs in a Windows player: runtime assembly only, enemies react, 0 B allocated.' -ForegroundColor Green