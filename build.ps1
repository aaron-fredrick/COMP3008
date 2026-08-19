param(
    [string]$Project = "Chat.Server",
    [string]$Configuration = "Debug"
)

$msbuildPath = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe 2>$null

if (-not $msbuildPath) {
    Write-Error "MSBuild not found. Please install Visual Studio with MSBuild."
    exit 1
}

$projectPath = "$Project\$Project.csproj"

if (-not (Test-Path $projectPath)) {
    Write-Error "Project file not found: $projectPath"
    exit 1
}

Write-Host "Building $Project ($Configuration)..." -ForegroundColor Cyan
& $msbuildPath $projectPath /p:Configuration=$Configuration /verbosity:minimal

if ($LASTEXITCODE -eq 0) {
    Write-Host "Build succeeded!" -ForegroundColor Green
} else {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}
