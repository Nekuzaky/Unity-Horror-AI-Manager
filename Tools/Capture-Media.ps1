<#
.SYNOPSIS
    Records the README screenshots and GIFs of Horror AI Manager from the real Unity Editor.
.DESCRIPTION
    Opens the editor (not in batch mode) on the demo scene and saves, to docs/images:
    - the inspectors (an enemy with its plain-word fixes, the light sources, the Patroller archetype and its profiles),
      the icon set and the Project window with the component icons;
    - the Setup and Archetypes tabs of the dashboard in Edit Mode;
    - in Play Mode, a scripted run of the demo: the enemy patrols, a bottle falls, it investigates and searches, the
      player walks under a lamp and is chased, then hides crouched in the dark. Each frame of the top-down Scene view
      (gizmos colored by state) and of the dashboard is saved, one game frame at a time at 15 fps;
    - the Overview, Enemies and Noises tabs and the Scene view gizmos during the chase.
    Every view is rendered into a texture, never read from the screen, so other windows never end up in the images.
    ffmpeg then turns the frames into horror-ai-demo.gif / .mp4 and dashboard.gif. It takes about two minutes.
.EXAMPLE
    ./Tools/Capture-Media.ps1
#>
[CmdletBinding()]
param([int] $GifWidth = 960, [int] $DashboardWidth = 900)

$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'HorrorAI.Tools.psm1') -Force

$ffmpeg = Get-Command ffmpeg -ErrorAction SilentlyContinue
if (-not $ffmpeg) { throw 'ffmpeg was not found on PATH. Install it, for example with: winget install Gyan.FFmpeg' }

$media = Get-OutputDirectory 'media'
$images = Join-Path (Get-RepoRoot) 'docs\images'
New-Item -ItemType Directory -Force -Path $images | Out-Null
$logFile = Join-Path (Get-OutputDirectory 'logs') 'capture.log'

Write-Host 'Opening the editor to capture the media. Do not use the mouse until it closes...'
$exitCode = Invoke-Unity -Interactive -Arguments @('-executeMethod', 'Nekuzaky.HorrorAI.Dev.MediaCapture.Run') -LogFile $logFile
Select-String -Path $logFile -Pattern '^Captured |^Recorded |\[Director\]|\[Capture\]' | ForEach-Object { Write-Host ('  ' + $_.Line.Trim()) }
$count = @(Get-ChildItem (Join-Path $media 'hero') -Filter 'frame_*.png' -ErrorAction SilentlyContinue).Count
if ($count -lt 30) {
    Write-Host "Only $count frames were recorded (exit code $exitCode). Log: $logFile" -ForegroundColor Red
    exit 1
}

function Convert-Frames([string] $folder, [string] $name, [int] $width, [switch] $Video) {
    $pattern = Join-Path (Join-Path $media $folder) 'frame_%04d.png'
    $palette = Join-Path $media "$folder-palette.png"
    $gif = Join-Path $images "$name.gif"
    $scale = "fps=15,scale=${width}:-1:flags=lanczos"
    & $ffmpeg.Source -y -loglevel error -framerate 15 -i $pattern -vf "$scale,palettegen=max_colors=128:stats_mode=diff" $palette
    & $ffmpeg.Source -y -loglevel error -framerate 15 -i $pattern -i $palette -lavfi "$scale [x]; [x][1:v] paletteuse=dither=bayer:bayer_scale=4:diff_mode=rectangle" $gif
    if ($Video) {
        $mp4 = Join-Path $images "$name.mp4"
        & $ffmpeg.Source -y -loglevel error -framerate 15 -i $pattern -vf 'scale=trunc(iw/2)*2:trunc(ih/2)*2,format=yuv420p' -c:v libx264 -crf 22 -movflags +faststart $mp4
    }
    Write-Host ("  {0}.gif: {1:N1} MB" -f $name, ((Get-Item $gif).Length / 1MB)) -ForegroundColor Green
}

Convert-Frames 'hero' 'horror-ai-demo' $GifWidth -Video
Convert-Frames 'dashboard' 'dashboard' $DashboardWidth
Write-Host "$count frames recorded. Images in docs/images:" -ForegroundColor Green
Get-ChildItem $images | Format-Table Name, Length
