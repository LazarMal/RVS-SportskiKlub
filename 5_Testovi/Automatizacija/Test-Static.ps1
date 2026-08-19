param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) {
        throw $Message
    }
}

$xmlFiles = Get-ChildItem -Path $RepoRoot -Recurse -File |
    Where-Object { $_.Extension -eq ".csproj" -or $_.Extension -eq ".config" }

foreach ($file in $xmlFiles) {
    try {
        [xml](Get-Content -LiteralPath $file.FullName -Raw) | Out-Null
    }
    catch {
        throw "Neispravan XML: $($file.FullName). $($_.Exception.Message)"
    }
}

$activeFiles = Get-ChildItem -Path $RepoRoot -Recurse -File -Include *.cs,*.cshtml,*.sql,*.config |
    Where-Object {
        $_.FullName -notmatch "\\(bin|obj|packages)\\" -and
        $_.FullName -notmatch "\\5_Testovi\\Automatizacija\\"
    }

$legacyPattern = "LicnaKarta|Lična karta|Gradjanin|BrojNoveLK|BrojStareLK|DatumIstekaLK|RazlogIzdavanja|TipZahteva|MestoPodnosenja"
$legacyMatches = $activeFiles | Select-String -Pattern $legacyPattern -CaseSensitive:$false
Assert-True ($null -eq $legacyMatches) "Pronađeni su aktivni termini stare teme: $($legacyMatches | Out-String)"

$absoluteHints = Get-ChildItem -Path $RepoRoot -Recurse -File -Filter *.csproj |
    Select-String -Pattern "<HintPath>([A-Za-z]:\\|/)"
Assert-True ($null -eq $absoluteHints) "Pronađene su apsolutne HintPath putanje: $($absoluteHints | Out-String)"

$requiredSymbols = @(
    "StampaSvih",
    "StampaFiltriranih",
    "StampaZahteva",
    "DodajSaDetaljima",
    "IzmeniSaDetaljima",
    "PotvrdiOdobrenjePoslePoslovneProvere",
    "PrijaviKorisnika"
)
$allSource = ($activeFiles | ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw }) -join "`n"
foreach ($symbol in $requiredSymbols) {
    Assert-True ($allSource.Contains($symbol)) "Nedostaje obavezni simbol: $symbol"
}

$requiredViews = @(
    "Prijava.cshtml",
    "Spisak.cshtml",
    "Dodaj.cshtml",
    "Izmeni.cshtml",
    "Detalji.cshtml",
    "Obrisi.cshtml",
    "StampaSpiska.cshtml",
    "StampaZahteva.cshtml"
)
$viewNames = Get-ChildItem -Path (Join-Path $RepoRoot "4_PrezentacioniSloj") -Recurse -File -Filter *.cshtml |
    Select-Object -ExpandProperty Name
foreach ($view in $requiredViews) {
    Assert-True ($viewNames -contains $view) "Nedostaje obavezni prikaz: $view"
}

$requiredProjects = @(
    "KlasePodataka",
    "DBUtils",
    "PoslovnaLogika",
    "KlaseMapiranja",
    "RESTServis",
    "PrezentacionaLogika",
    "KorisnickiInterfejs",
    "PoslovnaLogikaTestovi"
)
$solutionPath = Join-Path $RepoRoot "4_PrezentacioniSloj\KorisnickiInterfejs\KorisnickiInterfejs.sln"
$solution = Get-Content -LiteralPath $solutionPath -Raw
foreach ($project in $requiredProjects) {
    Assert-True ($solution -match ('Project\(".*?"\) = "' + [regex]::Escape($project) + '"')) "Glavni solution ne sadrži projekat: $project"
}

Write-Host "STATIC PASS: XML fajlova $($xmlFiles.Count), nema legacy termina, nema apsolutnih HintPath putanja, svi obavezni simboli/prikazi/projekti postoje."
