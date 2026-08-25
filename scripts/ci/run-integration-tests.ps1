$ErrorActionPreference = 'Stop'

$serverPath = Join-Path $PSScriptRoot '..\..\Chat.Server\bin\Debug\Chat.Server.exe'
$testPath = Join-Path $PSScriptRoot '..\..\Chat.Server.Tests\bin\Debug\Chat.Server.Tests.exe'
$artifactDir = Join-Path $PSScriptRoot '..\..\artifacts\integration'
$serverLog = Join-Path $artifactDir 'server.log'
$serverErrorLog = Join-Path $artifactDir 'server-error.log'
$testLog = Join-Path $artifactDir 'test-output.log'
$testErrorLog = Join-Path $artifactDir 'test-error.log'

New-Item -ItemType Directory -Force -Path $artifactDir | Out-Null

function Write-LogLine {
    param([string]$Line)
    if ($Line -match '^\[PASS\]') { Write-Host $Line -ForegroundColor Green }
    elseif ($Line -match '^\[FAIL\]') { Write-Host $Line -ForegroundColor Red }
    elseif ($Line -match '^\[WARN\]') { Write-Host $Line -ForegroundColor Yellow }
    elseif ($Line -match '^\[INFO\]') { Write-Host $Line -ForegroundColor Cyan }
    elseif ($Line -match '^=+') { Write-Host $Line -ForegroundColor DarkCyan }
    elseif ($Line -match '^(Starting integration suite|Structured integration tests:)') { Write-Host $Line -ForegroundColor Cyan }
    else { Write-Host $Line }
}

function Show-TestLog {
    if (Test-Path $testLog) {
        Get-Content $testLog | ForEach-Object { Write-LogLine $_ }
    }
    if (Test-Path $testErrorLog) {
        $errors = Get-Content $testErrorLog -ErrorAction SilentlyContinue
        if ($errors) {
            Write-Host '================ TEST ERRORS ================' -ForegroundColor Red
            $errors | ForEach-Object { Write-Host $_ -ForegroundColor Red }
        }
    }
}

function Wait-ForTcpPort {
    param([int]$Port, [int]$TimeoutSeconds = 30)
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            $connection = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
            if ($connection) {
                Write-Host "Port $Port is listening." -ForegroundColor DarkGreen
                return
            }
        } catch { }
        Start-Sleep -Milliseconds 500
    }
    throw "Timed out waiting for TCP port $Port."
}

function Show-Diagnostics {
    Write-Host ''
    Write-Host '================ TEST OUTPUT ================' -ForegroundColor Yellow
    Show-TestLog
    if (Test-Path $serverErrorLog) {
        $serverErrors = Get-Content $serverErrorLog -ErrorAction SilentlyContinue
        if ($serverErrors) {
            Write-Host '=============== SERVER ERRORS ===============' -ForegroundColor Red
            $serverErrors | ForEach-Object { Write-Host $_ -ForegroundColor Red }
        }
    }
}

$server = $null
$tests = $null
$testResult = $null
$testTimedOut = $false

try {
    if (-not (Test-Path $serverPath)) { throw "Server executable not found: $serverPath" }
    if (-not (Test-Path $testPath)) { throw "Test executable not found: $testPath" }

    Write-Host 'Starting Chat.Server.exe...' -ForegroundColor Cyan
    $server = Start-Process -FilePath $serverPath -WorkingDirectory (Split-Path $serverPath) -RedirectStandardOutput $serverLog -RedirectStandardError $serverErrorLog -PassThru

    Wait-ForTcpPort -Port 9000
    Wait-ForTcpPort -Port 8081

    Write-Host 'Server endpoints are ready. Starting structured tests...' -ForegroundColor Cyan
    $tests = Start-Process -FilePath $testPath -WorkingDirectory (Split-Path $testPath) -RedirectStandardOutput $testLog -RedirectStandardError $testErrorLog -PassThru

    $exited = $tests.WaitForExit(300000)
    $tests.Refresh()

    if ($exited -and $tests.HasExited) {
        $testResult = [int]$tests.ExitCode
    }
    else {
        $testTimedOut = $true
        $testResult = 1
        Write-Host 'Integration tests timed out before the process exited.' -ForegroundColor Red
    }

    if ($tests -and -not $tests.HasExited) {
        Stop-Process -Id $tests.Id -Force -ErrorAction SilentlyContinue
        $tests.Refresh()
    }

    if ($testResult -ne 0) {
        Show-Diagnostics
        Write-Error "Integration tests failed (result=$testResult)."
        exit $testResult
    }

    Write-Host 'Structured unit and integration tests passed.' -ForegroundColor Green
    Show-TestLog
}
finally {
    if ($tests -and -not $tests.HasExited) { Stop-Process -Id $tests.Id -Force -ErrorAction SilentlyContinue }
    if ($server -and -not $server.HasExited) { Stop-Process -Id $server.Id -Force -ErrorAction SilentlyContinue }
    Write-Host "Integration artifacts: $artifactDir" -ForegroundColor DarkGray
    if ($testTimedOut) { Write-Host 'Test process was terminated after timeout.' -ForegroundColor Red }
}
