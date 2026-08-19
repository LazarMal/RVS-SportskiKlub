param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path,
    [string]$ResultsDirectory = (Join-Path $RepoRoot "TestResults")
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

New-Item -ItemType Directory -Path $ResultsDirectory -Force | Out-Null
$restProject = Join-Path $RepoRoot "3_SlojServisa\RESTApi\RESTServis\RESTServis"
$mvcProject = Join-Path $RepoRoot "4_PrezentacioniSloj\KorisnickiInterfejs\KorisnickiInterfejs"
$mvcWebConfig = Join-Path $mvcProject "Web.config"
$iisExpress = Join-Path ${env:ProgramFiles} "IIS Express\iisexpress.exe"
$restLog = Join-Path $ResultsDirectory "iis-rest.log"
$restErrorLog = Join-Path $ResultsDirectory "iis-rest-error.log"
$mvcLog = Join-Path $ResultsDirectory "iis-mvc.log"
$mvcErrorLog = Join-Path $ResultsDirectory "iis-mvc-error.log"

if (-not (Test-Path -LiteralPath $iisExpress)) {
    throw "IIS Express nije pronađen: $iisExpress"
}

$originalWebConfig = [System.IO.File]::ReadAllBytes($mvcWebConfig)
$restProcess = $null
$mvcProcess = $null

function Wait-Url {
    param([string]$Url, [string]$Name, [System.Diagnostics.Process]$Process)
    $lastResponse = $null
    for ($attempt = 1; $attempt -le 90; $attempt++) {
        $Process.Refresh()
        if ($Process.HasExited) {
            throw "$Name proces se prerano završio (exit code $($Process.ExitCode)): $Url"
        }
        try {
            $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -SkipHttpErrorCheck -TimeoutSec 3
            $lastResponse = $response
            if ([int]$response.StatusCode -lt 500) {
                Write-Host "$Name je spreman: $Url (HTTP $($response.StatusCode))."
                return
            }
        }
        catch {
            Start-Sleep -Milliseconds 700
            continue
        }
        Start-Sleep -Milliseconds 700
    }
    if ($null -ne $lastResponse) {
        $lastResponse.Content | Set-Content -LiteralPath (Join-Path $ResultsDirectory "startup-failure.html") -Encoding UTF8
    }
    throw "$Name nije postao dostupan: $Url"
}

try {
    $webConfigText = [System.Text.Encoding]::UTF8.GetString($originalWebConfig).TrimStart([char]0xFEFF)
    $webConfigText = $webConfigText.Replace("https://localhost:44346/", "http://localhost:44346/")
    [System.IO.File]::WriteAllText($mvcWebConfig, $webConfigText, [System.Text.UTF8Encoding]::new($true))

    $restProcess = Start-Process -FilePath $iisExpress `
        -ArgumentList @("/path:$restProject", "/port:44346", "/systray:false") `
        -RedirectStandardOutput $restLog -RedirectStandardError $restErrorLog -PassThru
    Wait-Url "http://localhost:44346/api/parametri/poslovna-pravila" "REST servis" $restProcess

    $mvcProcess = Start-Process -FilePath $iisExpress `
        -ArgumentList @("/path:$mvcProject", "/port:44334", "/systray:false") `
        -RedirectStandardOutput $mvcLog -RedirectStandardError $mvcErrorLog -PassThru
    Wait-Url "http://localhost:44334/Nalog/Prijava" "MVC aplikacija" $mvcProcess

    & (Join-Path $PSScriptRoot "Test-RestApi.ps1") `
        -RestBaseUrl "http://localhost:44346" `
        -RepoRoot $RepoRoot `
        -ResultsDirectory $ResultsDirectory

    $env:RVS_REPO_ROOT = $RepoRoot
    $env:RVS_RESULTS_DIR = $ResultsDirectory
    $env:RVS_MVC_URL = "http://localhost:44334"
    $env:RVS_REST_URL = "http://localhost:44346"
    $env:RVS_CHROME_PATH = "C:\Program Files\Google\Chrome\Application\chrome.exe"

    & node (Join-Path $PSScriptRoot "ui-e2e.js") 2>&1 |
        Tee-Object -FilePath (Join-Path $ResultsDirectory "ui-e2e.log")
    if ($LASTEXITCODE -ne 0) {
        throw "Browser E2E testovi nisu prošli."
    }

    & node (Join-Path $PSScriptRoot "capture-documentation-screenshots.js") 2>&1 |
        Tee-Object -FilePath (Join-Path $ResultsDirectory "documentation-capture.log")
    if ($LASTEXITCODE -ne 0) {
        throw "Snimanje ekrana za dokumentaciju nije prošlo."
    }

    $finalSql = @"
SET NOCOUNT ON;
SELECT CONCAT(
    (SELECT COUNT(*) FROM dbo.ZahtevZaUclanjenje), N'|',
    (SELECT COUNT(*) FROM dbo.Dokumentacija), N'|',
    (SELECT COUNT(*) FROM dbo.RoditeljStaratelj), N'|',
    (SELECT COUNT(*) FROM dbo.IstorijaStatusaZahteva), N'|',
    (SELECT COUNT(*) FROM sys.triggers WHERE name=N'TR_TEST_Rollback_Dokumentacija'));
"@
    $finalState = & sqlcmd -S "(localdb)\MSSQLLocalDB" -E -d RVS2026SportskiKlub -h -1 -W -b -Q $finalSql 2>&1
    if ($LASTEXITCODE -ne 0) { throw "Završna SQL provera nije uspela: $($finalState | Out-String)" }
    $finalState = ($finalState | ForEach-Object { $_.ToString().Trim() } | Where-Object { $_ } | Select-Object -First 1)
    if ($finalState -ne "3|18|1|6|0") {
        throw "Testovi nisu očistili master-detail podatke ili trigger. Završno stanje: $finalState; očekivano 3|18|1|6|0."
    }
    "FINAL DATABASE STATE PASS: $finalState" | Set-Content -LiteralPath (Join-Path $ResultsDirectory "final-database-state.log") -Encoding UTF8
}
finally {
    [System.IO.File]::WriteAllBytes($mvcWebConfig, $originalWebConfig)
    foreach ($process in @($mvcProcess, $restProcess)) {
        if ($null -ne $process -and -not $process.HasExited) {
            Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
        }
    }
}

Write-Host "RUNTIME PASS: SQL, REST, MVC browser E2E i dokumentacioni screenshotovi su prošli."
