param(
    [string]$Configuration = "Debug"
)

$msbuildPath = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe 2>$null

if (-not $msbuildPath) {
    Write-Error "MSBuild not found. Please install Visual Studio with MSBuild."
    exit 1
}

$clients = @(
    "Chat.Client.Shared",
    "Chat.Client.Polling",
    "Chat.Client.Duplex"
)

$total = $clients.Count
for ($i = 0; $i -lt $total; $i++) {
    $project = $clients[$i]
    $projectPath = "src\$project\$project.csproj"

    Write-Host ""
    Write-Host "[$($i + 1)/$total] Building $project ($Configuration)..." -ForegroundColor Cyan

    & $msbuildPath $projectPath /p:Configuration=$Configuration /verbosity:minimal

    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed at $project!" -ForegroundColor Red
        exit 1
    }
}

Write-Host ""
Write-Host "All client builds succeeded!" -ForegroundColor Green
