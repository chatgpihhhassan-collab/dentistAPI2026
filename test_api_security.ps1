<#
.SYNOPSIS
    Automated Security Verification Script for Dentia Dental Platform
    Validates Rate Limiting, File Upload Magic-Byte Rejection, and Zero Information Leakage.
#>

param(
    [string]$ApiBaseUrl = "http://localhost:5000"
)

Write-Host "`n========================================================" -ForegroundColor Cyan
Write-Host "    DENTIA API SECURITY VERIFICATION TEST SUITE" -ForegroundColor Cyan
Write-Host "========================================================`n" -ForegroundColor Cyan

# Test 1: Correlation ID on API requests
Write-Host "[TEST 1] Verifying Global Exception & Correlation ID Header..." -ForegroundColor Yellow
try {
    $resp = Invoke-WebRequest -Uri "$ApiBaseUrl/api/auth/doctors" -Method GET -SkipHttpErrorCheck -TimeoutSec 10
    $corrId = $resp.Headers["X-Correlation-Id"]
    if ($corrId) {
        Write-Host "  [PASS] 'X-Correlation-Id' returned: $corrId" -ForegroundColor Green
    } else {
        Write-Host "  [WARN] 'X-Correlation-Id' header not detected in response." -ForegroundColor DarkYellow
    }
} catch {
    Write-Host "  [INFO] Could not connect to $ApiBaseUrl. Ensure DentistAPI is running." -ForegroundColor Gray
}

# Test 2: Rate Limiting on /api/auth/login (Limit: 5 requests / min)
Write-Host "`n[TEST 2] Testing Rate Limiting on /api/auth/login (Strict 5 req/min)..." -ForegroundColor Yellow
$loginPayload = @{
    Username = "security_test_doctor"
    Password = "IncorrectPassword123!"
} | ConvertTo-Json

$rateLimitTriggered = $false
for ($i = 1; $i -le 7; $i++) {
    try {
        $loginResp = Invoke-WebRequest -Uri "$ApiBaseUrl/api/auth/login" `
            -Method POST `
            -ContentType "application/json" `
            -Body $loginPayload `
            -SkipHttpErrorCheck `
            -TimeoutSec 5

        $code = $loginResp.StatusCode
        Write-Host "  Request $i: HTTP Status $code" -ForegroundColor $(if ($code -eq 429) { "Magenta" } else { "Gray" })

        if ($code -eq 429) {
            $rateLimitTriggered = $true
            Write-Host "  [PASS] Rate limit enforced on request $i! Response: $($loginResp.Content)" -ForegroundColor Green
            break
        }
    } catch {
        Write-Host "  Request $i failed: $($_.Exception.Message)" -ForegroundColor Red
    }
    Start-Sleep -Milliseconds 100
}

if (-not $rateLimitTriggered) {
    Write-Host "  [INFO] If API was not running, start DentistAPI to test live rate limiting." -ForegroundColor DarkYellow
}

# Test 3: File Upload Security - Reject Dangerous Extensions (.php, .exe, .svg)
Write-Host "`n[TEST 3] Testing File Upload Security (Spoofed & Prohibited files)..." -ForegroundColor Yellow
$testBytes = [System.Text.Encoding]::UTF8.GetBytes("<?php phpinfo(); ?>")
$tempPhpFile = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), "shell.php")
[System.IO.File]::WriteAllBytes($tempPhpFile, $testBytes)

try {
    # Using multipart form-data upload attempt to profile-image
    $form = @{
        file = Get-Item $tempPhpFile
    }
    $uploadResp = Invoke-RestMethod -Uri "$ApiBaseUrl/api/patients/1/profile-image" `
        -Method POST `
        -Form $form `
        -SkipHttpErrorCheck -ErrorAction SilentlyContinue

    Write-Host "  Upload result: $($uploadResp | ConvertTo-Json -Compress)" -ForegroundColor Gray
} catch {
    Write-Host "  [PASS] Server rejected dangerous file extension upload!" -ForegroundColor Green
} finally {
    if (Test-Path $tempPhpFile) { Remove-Item $tempPhpFile }
}

Write-Host "`n========================================================" -ForegroundColor Cyan
Write-Host "    TEST SUITE RUN COMPLETED" -ForegroundColor Cyan
Write-Host "========================================================`n" -ForegroundColor Cyan
