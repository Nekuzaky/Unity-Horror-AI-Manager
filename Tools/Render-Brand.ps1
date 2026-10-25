<#
.SYNOPSIS
    Renders the component and asset icons, the README banner and the GitHub social preview.
.DESCRIPTION
    Icons: docs/brand/render_icons.py (Python 3 and Pillow) draws a rounded colored tile with a white glyph for each
    component and asset, from the SVG files in Packages/com.nekuzaky.horror-ai-manager/Editor/UI/Icons (Bootstrap Icons,
    MIT, and three original glyphs), into Editor/Icons as 64 px PNG files. Their .meta files keep their GUIDs, so the
    icons assigned to the scripts stay assigned.
    Banner and social preview: starts a small local web server, then opens docs/brand/render.html once per image in a
    Chromium browser (Brave, Chrome or Edge when found, else the default browser). The page runs the HTML source in an
    iframe, draws it through an SVG foreignObject onto a canvas and posts the PNG back to the server, which writes it to
    an allow-listed path in docs/images. Headless browsers are not used: they hang on some machines.
    Pass -Only icons or -Only brand to render one of the two, and -NoBrowser to print the URLs instead.
.EXAMPLE
    ./Tools/Render-Brand.ps1
#>
[CmdletBinding()]
param([ValidateSet('all', 'icons', 'brand')][string] $Only = 'all', [int] $Port = 8766, [switch] $NoBrowser, [int] $TimeoutSeconds = 60)

$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'HorrorAI.Tools.psm1') -Force
$root = Get-RepoRoot

if ($Only -ne 'brand') {
    $python = Get-Command python -ErrorAction SilentlyContinue
    if (-not $python) { $python = Get-Command py -ErrorAction SilentlyContinue }
    if (-not $python) { throw 'Python 3 was not found on PATH. Install it, then: python -m pip install pillow' }
    & $python.Source (Join-Path $root 'docs\brand\render_icons.py')
    if ($LASTEXITCODE -ne 0) { throw "render_icons.py failed with exit code $LASTEXITCODE." }
}
if ($Only -eq 'icons') { return }

$jobs = @(
    @{ Page = 'banner.html'; Width = 1280; Height = 400; Out = 'docs/images/banner.png' },
    @{ Page = 'social-preview.html'; Width = 1280; Height = 640; Out = 'docs/images/social-preview.png' }
)
$allowed = $jobs | ForEach-Object { $_.Out }
$types = @{ '.html' = 'text/html; charset=utf-8'; '.svg' = 'image/svg+xml'; '.png' = 'image/png'; '.css' = 'text/css'; '.js' = 'text/javascript' }
$browsers = @(
    "$env:LOCALAPPDATA\BraveSoftware\Brave-Browser\Application\brave.exe",
    "$env:ProgramFiles\BraveSoftware\Brave-Browser\Application\brave.exe",
    "$env:LOCALAPPDATA\Google\Chrome\Application\chrome.exe",
    "$env:ProgramFiles\Google\Chrome\Application\chrome.exe",
    "${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe",
    "$env:ProgramFiles\Microsoft\Edge\Application\msedge.exe"
) | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1

$listener = New-Object System.Net.HttpListener
$listener.Prefixes.Add("http://127.0.0.1:$Port/")
$listener.Start()

function Send([System.Net.HttpListenerResponse] $response, [int] $status, [byte[]] $body, [string] $type) {
    $response.StatusCode = $status
    if ($type) { $response.ContentType = $type }
    $response.OutputStream.Write($body, 0, $body.Length)
    $response.Close()
}

function Handle([System.Net.HttpListenerContext] $context) {
    $request = $context.Request
    $path = [Uri]::UnescapeDataString($request.Url.AbsolutePath.TrimStart('/'))
    if ($request.HttpMethod -eq 'POST') {
        $out = $request.QueryString['out']
        if ($allowed -notcontains $out) { Send $context.Response 403 ([byte[]]@()) $null; return $null }
        $reader = New-Object System.IO.StreamReader($request.InputStream)
        $data = $reader.ReadToEnd().Split(',', 2)[1]
        [System.IO.File]::WriteAllBytes((Join-Path $root $out), [Convert]::FromBase64String($data))
        Send $context.Response 200 ([Text.Encoding]::UTF8.GetBytes('saved')) 'text/plain'
        return $out
    }
    $file = Join-Path $root $path
    if (-not $path.StartsWith('docs/brand/') -or -not (Test-Path $file -PathType Leaf)) {
        Send $context.Response 404 ([byte[]]@()) $null
        return $null
    }
    Send $context.Response 200 ([System.IO.File]::ReadAllBytes($file)) $types[[System.IO.Path]::GetExtension($file)]
    return $null
}

try {
    foreach ($job in $jobs) {
        $query = "page=$($job.Page)&w=$($job.Width)&h=$($job.Height)&out=$([Uri]::EscapeDataString($job.Out))"
        $url = "http://127.0.0.1:$Port/docs/brand/render.html?$query"
        if ($NoBrowser) { Write-Host "Open: $url" }
        elseif ($browsers) { Start-Process $browsers -ArgumentList @('--new-window', $url) }
        else { Start-Process $url }

        $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
        $saved = $null
        for (; -not $saved -and (Get-Date) -lt $deadline; ) {
            $pending = $listener.GetContextAsync()
            for (; -not $pending.Wait(250) -and (Get-Date) -lt $deadline; ) { }
            if ($pending.IsCompleted) { $saved = Handle $pending.Result }
        }
        if ($saved) { Write-Host "  $saved" -ForegroundColor Green } else { throw "Timed out waiting for $($job.Out)." }
    }
} finally {
    $listener.Stop()
    $listener.Close()
}
