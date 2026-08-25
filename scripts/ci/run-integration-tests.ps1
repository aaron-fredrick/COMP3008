$ErrorActionPreference = 'Stop'

$serverPath = Join-Path $PSScriptRoot '..\..\Chat.Server\bin\Debug\Chat.Server.exe'
$testPath = Join-Path $PSScriptRoot '..\..\Chat.Server.Tests\bin\Debug\Chat.Server.Tests.exe'
$artifactDir = Join-Path $PSScriptRoot '..\..\artifacts\integration'
$serverLog = Join-Path $artifactDir 'server.log'
$serverErrorLog = Join-Path $artifactDir 'server-error.log'
$testLog = Join-Path $artifactDir 'test-output.log'
$testErrorLog = Join-Path $artifactDir 'test-error.log'

New-Item -ItemType Directory -Force -Path $artifactDir | Out-Null

function Wait-ForTcpPort {
    param(
        [int]$Port,
        [int]$TimeoutSeconds = 30
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            $connection = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
            if ($connection) {
                Write-Host "Port $Port is listening."
                return
            }
        } catch {
            # Retry until the timeout expires.
        }
        Start-Sleep -Milliseconds 500
    }

    throw "Timed out waiting for TCP port $Port."
}

$server = $null
$tests = $null
$testResult = $null
$testTimedOut = $false

try {
    if (-not (Test-Path $serverPath)) { throw "Server executable not found: $serverPath" }
    if (-not (Test-Path $testPath)) { throw "Test executable not found: $testPath" }

    Write-Host "Starting Chat.Server.exe..."
    $server = Start-Process `
        -FilePath $serverPath `
        -WorkingDirectory (Split-Path $serverPath) `
        -RedirectStandardOutput $serverLog `
        -RedirectStandardError $serverErrorLog `
        -PassThru

    Wait-ForTcpPort -Port 9000
    Wait-ForTcpPort -Port 8081

    Write-Host "Server endpoints are ready. Starting integration tests..."
    $tests = Start-Process `
        -FilePath $testPath `
        -WorkingDirectory (Split-Path $testPath) `
        -RedirectStandardOutput $testLog `
        -RedirectStandardError $testErrorLog `
        -PassThru

    $deadline = (Get-Date).AddMinutes(5)
    while ((Get-Date) -lt $deadline) {
        if (Test-Path $testLog) {
            $content = Get-Content $testLog -Raw -ErrorAction SilentlyContinue
            if ($content -match 'RESULT: ALL TESTS PASSED') {
                $testResult = 0
                break
            }
            if ($content -match 'RESULT: ([0-9]+) TEST\(S\) FAILED') {
                $testResult = 1
                break
            }
        }

        if ($tests.HasExited) {
            $testResult = if ($tests.ExitCode -eq 0) { 0 } else { $tests.ExitCode }
            break
        }

        Start-Sleep -Seconds 1
    }

    if ($null -eq $testResult) {
        $testTimedOut = $true
        $testResult = 1
        Write-Error 'Integration tests timed out before producing a final RESULT line.'
    }

    if ($tests -and -not $tests.HasExited) {
        Stop-Process -Id $tests.Id -Force -ErrorAction SilentlyContinue
    }

    if ($testResult -ne 0) {
        Write-Error "Integration tests failed (result=$testResult)."
        exit $testResult
    }

    Write-Host 'Integration tests passed.'
}
finally {
    if ($tests -and -not $tests.HasExited) {
        Stop-Process -Id $tests.Id -Force -ErrorAction SilentlyContinue
    }
    if ($server -and -not $server.HasExited) {
        Stop-Process -Id $server.Id -Force -ErrorAction SilentlyContinue
    }

    Write-Host "Integration artifacts: $artifactDir"
    if ($testTimedOut) { Write-Host 'Test process was terminated after timeout.' }
}
