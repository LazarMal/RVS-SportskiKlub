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

$dataLibraryPath = Join-Path $RepoRoot "1_SlojPodataka\DBUtils\DBUtils"
$oldDataLibraryPath = Join-Path $RepoRoot "1_SlojPodataka\TehnoloskeKlase\DBUtils"
$repositoryPath = Join-Path $dataLibraryPath "Repozitorijumi"
$technologyPath = Join-Path $dataLibraryPath "TehnoloskeKlase"

Assert-True (Test-Path -LiteralPath (Join-Path $dataLibraryPath "DBUtils.csproj")) "DBUtils Class Library nije na očekivanoj putanji van foldera TehnoloskeKlase."
Assert-True (-not (Test-Path -LiteralPath $oldDataLibraryPath)) "DBUtils projekat je i dalje unutar spoljnog foldera TehnoloskeKlase."
Assert-True (Test-Path -LiteralPath $repositoryPath) "Nedostaje izdvojeni folder Repozitorijumi."
Assert-True (Test-Path -LiteralPath $technologyPath) "Nedostaje izdvojeni folder TehnoloskeKlase."

$requiredRepositoryFiles = @(
    "IKandidatRepozitorijum.cs",
    "IKorisnikRepozitorijum.cs",
    "ISportskaDisciplinaRepozitorijum.cs",
    "IZahtevZaUclanjenjeRepozitorijum.cs",
    "KandidatRepozitorijum.cs",
    "KorisnikRepozitorijum.cs",
    "SportskaDisciplinaRepozitorijum.cs",
    "ZahtevZaUclanjenjeRepozitorijum.cs"
)
foreach ($file in $requiredRepositoryFiles) {
    Assert-True (Test-Path -LiteralPath (Join-Path $repositoryPath $file)) "Repozitorijumska klasa nije u izdvojenom folderu: $file"
}

$requiredTechnologyFiles = @(
    "OsnovnaTehnoloskaKlasa.cs",
    "TabelaKlasa.cs"
)
foreach ($file in $requiredTechnologyFiles) {
    Assert-True (Test-Path -LiteralPath (Join-Path $technologyPath $file)) "Tehnološka klasa nije u izdvojenom folderu: $file"
}

$repositoryFilesInTechnologyFolder = Get-ChildItem -Path $technologyPath -Recurse -File -Filter *Repozitorijum.cs
Assert-True ($null -eq $repositoryFilesInTechnologyFolder) "Repozitorijumske klase ne smeju biti u folderu TehnoloskeKlase."

$restHomeController = Join-Path $RepoRoot "3_SlojServisa\RESTApi\RESTServis\RESTServis\Controllers\HomeController.cs"
$restPocetnaController = Join-Path $RepoRoot "3_SlojServisa\RESTApi\RESTServis\RESTServis\Controllers\PocetnaController.cs"
Assert-True (-not (Test-Path -LiteralPath $restHomeController)) "REST servis i dalje sadrži HomeController.cs."
Assert-True (Test-Path -LiteralPath $restPocetnaController) "REST servis nema PocetnaController.cs."
Assert-True (-not $allSource.Contains("class HomeController")) "REST servis i dalje sadrži klasu HomeController."
Assert-True ($allSource.Contains("class PocetnaController")) "REST servis nema klasu PocetnaController."
Assert-True (-not ($allSource -match 'controller\s*=\s*"Home"')) "REST podrazumevana ruta i dalje koristi kontroler Home."

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

Write-Host "STATIC PASS: XML fajlova $($xmlFiles.Count), nema legacy termina ni apsolutnih HintPath putanja, repozitorijumi su odvojeni od tehnoloških klasa, REST početni kontroler je na srpskom i svi obavezni simboli/prikazi/projekti postoje."
