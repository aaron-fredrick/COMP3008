param(
    [string]$Target = "COMP3008.sln",
    [string]$Configuration = "Debug"
)

$msbuildPath = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe 2>$null

if (-not $msbuildPath) {
    Write-Error "MSBuild not found. Please install Visual Studio with MSBuild."
    exit 1
}

# Accept either a .sln, a project name (Chat.Server -> Chat.Server\Chat.Server.csproj), or a direct path.
if ($Target -match '\.(sln|csproj)$') {
    $targetPath = $Target
} else {
    $targetPath = "$Target\$Target.csproj"
}

if (-not (Test-Path $targetPath)) {
    Write-Error "Build target not found: $targetPath"
    exit 1
}

Write-Host "Building $targetPath ($Configuration)..." -ForegroundColor Cyan
& $msbuildPath $targetPath /p:Configuration=$Configuration /verbosity:minimal /m

if ($LASTEXITCODE -eq 0) {
    Write-Host "Build succeeded!" -ForegroundColor Green
} else {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}
