param(
    [string]$RestBaseUrl = "http://localhost:44346",
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path,
    [string]$ResultsDirectory = (Join-Path $RepoRoot "TestResults")
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$server = "(localdb)\MSSQLLocalDB"
$database = "RVS2026SportskiKlub"
New-Item -ItemType Directory -Path $ResultsDirectory -Force | Out-Null
$logPath = Join-Path $ResultsDirectory "rest-tests.log"
"RVS REST TESTS $(Get-Date -Format o)" | Set-Content -LiteralPath $logPath -Encoding UTF8

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

function Assert-Equal {
    param($Actual, $Expected, [string]$Message)
    if ([string]$Actual -ne [string]$Expected) {
        throw "$Message Očekivano: '$Expected'; dobijeno: '$Actual'."
    }
}

function Invoke-SqlScalar {
    param([string]$Query)
    $output = & sqlcmd -S $server -E -d $database -h -1 -W -b -Q "SET NOCOUNT ON; $Query" 2>&1
    if ($LASTEXITCODE -ne 0) { throw "SQL provera nije uspela: $($output | Out-String)" }
    return (($output | ForEach-Object { $_.ToString().Trim() } | Where-Object { $_ }) | Select-Object -First 1)
}

function Get-Season {
    $start = if ((Get-Date).Month -ge 7) { (Get-Date).Year } else { (Get-Date).Year - 1 }
    return "{0}/{1:00}" -f $start, (($start + 1) % 100)
}

function New-Payload {
    param(
        [long]$Number,
        [bool]$Minor = $false,
        [bool]$IncludeGuardian = $false
    )

    $birthDate = if ($Minor) { (Get-Date).AddYears(-15).ToString("yyyy-MM-dd") } else { "1995-05-10" }
    $documents = @(
        [ordered]@{ IDDokumentacije = 0; NazivDokumenta = "Fotografija kandidata"; Dostavljeno = $true },
        [ordered]@{ IDDokumentacije = 0; NazivDokumenta = "Dokaz identiteta"; Dostavljeno = $true },
        [ordered]@{ IDDokumentacije = 0; NazivDokumenta = "Potvrda o sportskom pregledu"; Dostavljeno = $true },
        [ordered]@{ IDDokumentacije = 0; NazivDokumenta = "Evidencija o položenom testu sposobnosti"; Dostavljeno = $true },
        [ordered]@{ IDDokumentacije = 0; NazivDokumenta = "Saglasnost roditelja/staratelja"; Dostavljeno = ($Minor -and $IncludeGuardian) },
        [ordered]@{ IDDokumentacije = 0; NazivDokumenta = "Drugi dokument"; Dostavljeno = $false }
    )

    $payload = [ordered]@{
        IDZahteva = 0
        BrojZahteva = $null
        JMBG = ("{0:D13}" -f $Number)
        Ime = "REST"
        Prezime = "Test$Number"
        DatumRodjenja = $birthDate
        Pol = "M"
        Drzavljanstvo = "Srbija"
        Adresa = "Test adresa 1"
        KontaktTelefon = "064111222"
        Email = "rest$Number@example.com"
        IDSportskeDiscipline = 1
        NazivSportskeDiscipline = $null
        DatumPodnosenja = (Get-Date).ToString("yyyy-MM-dd")
        Sezona = Get-Season
        MestoKluba = "Zrenjanin"
        DatumSportskogPregleda = (Get-Date).AddMonths(-1).ToString("yyyy-MM-dd")
        RezultatTestaSposobnosti = "Položen"
        StatusZahteva = "U obradi"
        Napomena = "Automatizovani REST test"
        Dokumentacija = $documents
        RoditeljStaratelj = $null
        IstorijaStatusa = @()
    }

    if ($IncludeGuardian) {
        $payload.RoditeljStaratelj = [ordered]@{
            IDRoditeljaStaratelja = 0
            ImePrezime = "Roditelj Test"
            JMBG = "0101980123456"
            Srodstvo = "Otac"
            KontaktTelefon = "064777888"
            Email = "roditelj@example.com"
        }
    }

    return $payload
}

function Send-Json {
    param([string]$Method, [string]$Url, $Body, [int]$ExpectedStatus)
    $json = if ($null -eq $Body) { $null } else { $Body | ConvertTo-Json -Depth 12 -Compress }
    $parameters = @{
        Uri = $Url
        Method = $Method
        SkipHttpErrorCheck = $true
        UseBasicParsing = $true
    }
    if ($null -ne $json) {
        $parameters.ContentType = "application/json; charset=utf-8"
        $parameters.Body = [System.Text.Encoding]::UTF8.GetBytes($json)
    }
    $response = Invoke-WebRequest @parameters
    Assert-Equal ([int]$response.StatusCode) $ExpectedStatus "$Method $Url nije vratio očekivani status. Telo: $($response.Content)"
    return $response
}

$parameters = Invoke-RestMethod -Uri "$RestBaseUrl/api/parametri/poslovna-pravila" -Method Get
Assert-Equal $parameters.MaksimalnaStarostSportskogPregledaMeseci 6 "REST parametar X nije 6."
Assert-Equal $parameters.StarosnaGranicaZaSaglasnost 18 "REST starosna granica nije 18."

for ($i = 1; $i -le 10; $i++) {
    $payload = New-Payload -Number (8100000000000 + $i)
    $createdResponse = Send-Json Post "$RestBaseUrl/api/zahtevi" $payload 200
    $created = $createdResponse.Content | ConvertFrom-Json
    Assert-True ($created.IDZahteva -gt 0) "REST POST nije vratio ID u krugu $i."
    Assert-Equal $created.StatusZahteva "U obradi" "REST POST nije forsirao početni status."
    Assert-True ($created.BrojZahteva -match '^ZSK-\d{4}-\d{6}$') "Broj zahteva nije u propisanom formatu."
    Assert-Equal $created.Dokumentacija.Count 6 "Master-detail POST nije sačuvao svih šest stavki."
    Assert-Equal $created.IstorijaStatusa.Count 1 "Početna istorija statusa nije upisana."

    $get = Invoke-RestMethod -Uri "$RestBaseUrl/api/zahtevi/$($created.IDZahteva)" -Method Get
    Assert-Equal $get.JMBG $payload.JMBG "REST GET po ID-u nije vratio kreirani zahtev."

    $filter = [uri]::EscapeDataString($payload.Prezime)
    $filtered = Invoke-RestMethod -Uri "$RestBaseUrl/api/zahtevi?filter=$filter" -Method Get
    $matchingCount = 0
    foreach ($item in $filtered) {
        if ($null -ne $item -and [int]$item.IDZahteva -eq [int]$created.IDZahteva) {
            $matchingCount++
        }
    }
    $filteredJson = $filtered | ConvertTo-Json -Depth 4 -Compress
    Assert-True ($matchingCount -eq 1) "REST filter nije našao tačan zahtev. Odgovor: $filteredJson"

    $created.Prezime = "Izmenjen$i"
    $created.StatusZahteva = "Na proveri"
    $created.Napomena = "REST izmena $i"
    $updatedResponse = Send-Json Put "$RestBaseUrl/api/zahtevi/$($created.IDZahteva)" $created 200
    $updated = $updatedResponse.Content | ConvertFrom-Json
    Assert-Equal $updated.Prezime "Izmenjen$i" "REST PUT nije izmenio kandidata."
    Assert-Equal $updated.StatusZahteva "Na proveri" "REST PUT nije izmenio dozvoljeni status."
    Assert-True ($updated.IstorijaStatusa.Count -ge 2) "REST PUT nije upisao istoriju promene statusa."

    $updated.StatusZahteva = "Odobren"
    [void](Send-Json Put "$RestBaseUrl/api/zahtevi/$($created.IDZahteva)" $updated 400)

    [void](Send-Json Delete "$RestBaseUrl/api/zahtevi/$($created.IDZahteva)" $null 200)
    [void](Send-Json Get "$RestBaseUrl/api/zahtevi/$($created.IDZahteva)" $null 404)
    Assert-Equal (Invoke-SqlScalar "SELECT COUNT(*) FROM dbo.Dokumentacija WHERE IDZahteva=$($created.IDZahteva);") 0 "Brisanje je ostavilo dokumentaciju."
    Assert-Equal (Invoke-SqlScalar "SELECT COUNT(*) FROM dbo.IstorijaStatusaZahteva WHERE IDZahteva=$($created.IDZahteva);") 0 "Brisanje je ostavilo istoriju."
}

for ($i = 1; $i -le 10; $i++) {
    $baseNumber = 8200000000000 + ($i * 20)
    $negativeCases = @()

    $p = New-Payload -Number ($baseNumber + 1); $p.JMBG = "123"; $negativeCases += $p
    $p = New-Payload -Number ($baseNumber + 2); $p.Email = "neispravan-email"; $negativeCases += $p
    $p = New-Payload -Number ($baseNumber + 3); $p.KontaktTelefon = "abc"; $negativeCases += $p
    $p = New-Payload -Number ($baseNumber + 4); $p.DatumRodjenja = (Get-Date).AddDays(1).ToString("yyyy-MM-dd"); $negativeCases += $p
    $p = New-Payload -Number ($baseNumber + 5); $p.DatumSportskogPregleda = (Get-Date).AddDays(1).ToString("yyyy-MM-dd"); $negativeCases += $p
    $p = New-Payload -Number ($baseNumber + 6); $p.Sezona = "2026-27"; $negativeCases += $p
    $p = New-Payload -Number ($baseNumber + 7); $p.IDSportskeDiscipline = 999999; $negativeCases += $p
    $p = New-Payload -Number ($baseNumber + 8); $p.RezultatTestaSposobnosti = "Nevažeći"; $negativeCases += $p
    $p = New-Payload -Number ($baseNumber + 9); $p.Dokumentacija = @($p.Dokumentacija | Where-Object NazivDokumenta -ne "Potvrda o sportskom pregledu"); $negativeCases += $p
    $p = New-Payload -Number ($baseNumber + 10) -Minor $true -IncludeGuardian $false; $negativeCases += $p

    foreach ($case in $negativeCases) {
        [void](Send-Json Post "$RestBaseUrl/api/zahtevi" $case 400)
    }

    $minor = New-Payload -Number ($baseNumber + 11) -Minor $true -IncludeGuardian $true
    $minorCreated = (Send-Json Post "$RestBaseUrl/api/zahtevi" $minor 200).Content | ConvertFrom-Json
    Assert-True ($null -ne $minorCreated.RoditeljStaratelj) "Validan maloletni kandidat nije sačuvao roditelja u krugu $i."
    [void](Send-Json Delete "$RestBaseUrl/api/zahtevi/$($minorCreated.IDZahteva)" $null 200)
}

$triggerSql = "IF OBJECT_ID(N'dbo.TR_TEST_Rollback_Dokumentacija',N'TR') IS NOT NULL DROP TRIGGER dbo.TR_TEST_Rollback_Dokumentacija; EXEC(N'CREATE TRIGGER dbo.TR_TEST_Rollback_Dokumentacija ON dbo.Dokumentacija INSTEAD OF INSERT AS THROW 51000, N''Namerno izazvana greška detail unosa.'', 1;');"
& sqlcmd -S $server -E -d $database -b -Q $triggerSql *> $null
if ($LASTEXITCODE -ne 0) { throw "Test trigger za rollback nije kreiran." }

try {
    for ($i = 1; $i -le 10; $i++) {
        $beforeRequests = Invoke-SqlScalar "SELECT COUNT(*) FROM dbo.ZahtevZaUclanjenje;"
        $beforeDocuments = Invoke-SqlScalar "SELECT COUNT(*) FROM dbo.Dokumentacija;"
        $beforeCandidates = Invoke-SqlScalar "SELECT COUNT(*) FROM dbo.Kandidat;"
        $rollbackPayload = New-Payload -Number (8500000000000 + $i)
        [void](Send-Json Post "$RestBaseUrl/api/zahtevi" $rollbackPayload 500)
        Assert-Equal (Invoke-SqlScalar "SELECT COUNT(*) FROM dbo.ZahtevZaUclanjenje;") $beforeRequests "Rollback je ostavio master zapis."
        Assert-Equal (Invoke-SqlScalar "SELECT COUNT(*) FROM dbo.Dokumentacija;") $beforeDocuments "Rollback je ostavio detail zapis."
        Assert-Equal (Invoke-SqlScalar "SELECT COUNT(*) FROM dbo.Kandidat;") $beforeCandidates "Rollback je ostavio kandidata."
    }
}
finally {
    & sqlcmd -S $server -E -d $database -b -Q "IF OBJECT_ID(N'dbo.TR_TEST_Rollback_Dokumentacija',N'TR') IS NOT NULL DROP TRIGGER dbo.TR_TEST_Rollback_Dokumentacija;" *> $null
}

"REST PASS: 10 punih CRUD/master-detail ciklusa, 100 negativnih validacija, 10 maloletnih pozitivnih slučajeva i 10 rollback provera." |
    Tee-Object -FilePath $logPath -Append
