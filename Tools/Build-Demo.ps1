<#
.SYNOPSIS
    Regenerates the Patroller preset and the greybox demo scene (with its baked NavMesh) in Assets/Dev/Demo.
.DESCRIPTION
    Runs Nekuzaky.HorrorAI.Dev.DemoSceneBuilder.Build in batch mode. Run Tools/Sync-Sample.ps1 afterwards to update the
    package sample.
.EXAMPLE
    ./Tools/Build-Demo.ps1
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'HorrorAI.Tools.psm1') -Force

$logFile = Join-Path (Get-OutputDirectory 'logs') 'demo.log'
Write-Host "Building the demo with Unity $(Get-UnityVersion)..."
$exitCode = Invoke-Unity -Arguments @('-executeMethod', 'Nekuzaky.HorrorAI.Dev.DemoSceneBuilder.Build', '-quit') -LogFile $logFile
if ($exitCode -ne 0 -or -not (Select-String -Path $logFile -Pattern 'Demo scene written' -Quiet)) {
    Get-CompilerMessages -LogFile $logFile | ForEach-Object { Write-Host $_ -ForegroundColor Red }
    Write-Host "The demo was not rebuilt (exit code $exitCode). Log: $logFile" -ForegroundColor Red
    exit 1
}
Write-Host 'Demo rebuilt in Assets/Dev/Demo.' -ForegroundColor Green