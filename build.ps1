param(
    [string]$Target = "COMP3008.slnx",
    [string]$Configuration = "Debug"
)

$msbuildPath = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe 2>$null

if (-not $msbuildPath) {
    Write-Error "MSBuild not found. Please install Visual Studio with MSBuild."
    exit 1
}

# Accept either a .sln/.csproj path, a bare project name, or the default solution.
if ($Target -match '\.(sln|slnx|csproj)$') {
    $targetPath = $Target
} else {
    # Bare project name: look under src\ first, then tests\.
    $srcPath = "src\$Target\$Target.csproj"
    $testsPath = "tests\$Target\$Target.csproj"
    if (Test-Path $srcPath) {
        $targetPath = $srcPath
    } elseif (Test-Path $testsPath) {
        $targetPath = $testsPath
    } else {
        $targetPath = $srcPath  # let the existence check below produce a clear error
    }
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
