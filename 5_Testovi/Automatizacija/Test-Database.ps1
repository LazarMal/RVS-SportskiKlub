param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path,
    [string]$ResultsDirectory = (Join-Path $RepoRoot "TestResults")
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$server = "(localdb)\MSSQLLocalDB"
$database = "RVS2026SportskiKlub"
$installScript = Join-Path $RepoRoot "1_SlojPodataka\BazaPodataka\InstalacijaBaze.sql"
New-Item -ItemType Directory -Path $ResultsDirectory -Force | Out-Null
$logPath = Join-Path $ResultsDirectory "database-tests.log"

function Assert-Equal {
    param($Actual, $Expected, [string]$Message)
    if ([string]$Actual -ne [string]$Expected) {
        throw "$Message Očekivano: '$Expected'; dobijeno: '$Actual'."
    }
}

function Invoke-SqlScalar {
    param([string]$Query, [string]$DatabaseName = $database)
    $output = & sqlcmd -S $server -E -d $DatabaseName -h -1 -W -b -Q "SET NOCOUNT ON; $Query" 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "SQL upit nije prošao: $Query`n$($output | Out-String)"
    }
    return (($output | ForEach-Object { $_.ToString().Trim() } | Where-Object { $_ }) | Select-Object -First 1)
}

"RVS DATABASE TESTS $(Get-Date -Format o)" | Set-Content -LiteralPath $logPath -Encoding UTF8

& sqllocaldb info MSSQLLocalDB *> $null
if ($LASTEXITCODE -ne 0) {
    & sqllocaldb create MSSQLLocalDB | Tee-Object -FilePath $logPath -Append
}
& sqllocaldb start MSSQLLocalDB | Tee-Object -FilePath $logPath -Append

$drop = "IF DB_ID(N'$database') IS NOT NULL BEGIN ALTER DATABASE [$database] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$database]; END;"
& sqlcmd -S $server -E -d master -b -Q $drop | Tee-Object -FilePath $logPath -Append
if ($LASTEXITCODE -ne 0) { throw "Čišćenje testne baze nije uspelo." }

& sqlcmd -S $server -E -b -f 65001 -i $installScript 2>&1 | Tee-Object -FilePath $logPath -Append
if ($LASTEXITCODE -ne 0) { throw "Čista instalacija baze nije uspela." }

Assert-Equal (Invoke-SqlScalar "SELECT COUNT(*) FROM sys.tables WHERE name IN (N'Kandidat',N'SportskaDisciplina',N'ZahtevZaUclanjenje',N'Dokumentacija',N'RoditeljStaratelj',N'IstorijaStatusaZahteva',N'Korisnik');") 7 "Nisu kreirane sve obavezne tabele."
Assert-Equal (Invoke-SqlScalar "SELECT COUNT(*) FROM sys.procedures WHERE name=N'PrijaviKorisnika';") 1 "Stored Procedure nije kreirana."
Assert-Equal (Invoke-SqlScalar "SELECT COUNT(*) FROM dbo.SportskaDisciplina WHERE Aktivna=1;") 6 "Šifarnik nema šest aktivnih disciplina."
Assert-Equal (Invoke-SqlScalar "SELECT COUNT(*) FROM dbo.Korisnik;") 2 "Seed korisnici nisu kreirani."

# Browser regresija koristi isključivo privremene, nasumično generisane naloge.
# Time test-automatizacija ne objavljuje niti zavisi od pristupnih podataka iz seed skripta.
$adminUser = "rvs-ci-a-$([Guid]::NewGuid().ToString('N').Substring(0,12))"
$adminPassword = "Rvs!$([Guid]::NewGuid().ToString('N'))"
$referentUser = "rvs-ci-r-$([Guid]::NewGuid().ToString('N').Substring(0,12))"
$referentPassword = "Rvs!$([Guid]::NewGuid().ToString('N'))"
$createCiUsers = @"
INSERT INTO dbo.Korisnik (KorisnickoIme,Sifra,Ime,Prezime,Uloga,Aktivan)
VALUES
    (N'$adminUser',N'$adminPassword',N'CI',N'Administrator',N'Administrator',1),
    (N'$referentUser',N'$referentPassword',N'CI',N'Referent',N'Referent',1);
"@
Invoke-SqlScalar "$createCiUsers SELECT COUNT(*) FROM dbo.Korisnik WHERE KorisnickoIme IN (N'$adminUser',N'$referentUser');" | ForEach-Object {
    Assert-Equal $_ 2 "Privremeni CI korisnici nisu kreirani."
}

$env:RVS_E2E_ADMIN_USER = $adminUser
$env:RVS_E2E_ADMIN_PASSWORD = $adminPassword
$env:RVS_E2E_REFERENT_USER = $referentUser
$env:RVS_E2E_REFERENT_PASSWORD = $referentPassword
if ($env:GITHUB_ENV) {
    "RVS_E2E_ADMIN_USER=$adminUser" | Out-File -FilePath $env:GITHUB_ENV -Encoding utf8 -Append
    "RVS_E2E_ADMIN_PASSWORD=$adminPassword" | Out-File -FilePath $env:GITHUB_ENV -Encoding utf8 -Append
    "RVS_E2E_REFERENT_USER=$referentUser" | Out-File -FilePath $env:GITHUB_ENV -Encoding utf8 -Append
    "RVS_E2E_REFERENT_PASSWORD=$referentPassword" | Out-File -FilePath $env:GITHUB_ENV -Encoding utf8 -Append
}

$secondRun = & sqlcmd -S $server -E -b -f 65001 -i $installScript 2>&1
if ($LASTEXITCODE -eq 0) { throw "Drugo izvršavanje instalacionog skripta moralo je bezbedno da se prekine." }
if (($secondRun | Out-String) -notmatch "već postoji") { throw "Drugo izvršavanje nije vratilo očekivanu zaštitnu poruku." }
$secondRun | Tee-Object -FilePath $logPath -Append | Out-Null

for ($i = 1; $i -le 10; $i++) {
    Assert-Equal (Invoke-SqlScalar "DECLARE @T TABLE(IDKorisnika int, KorisnickoIme nvarchar(50), Ime nvarchar(50), Prezime nvarchar(50), Uloga nvarchar(30)); INSERT INTO @T EXEC dbo.PrijaviKorisnika N'$adminUser', N'$adminPassword'; SELECT COUNT(*) FROM @T;") 1 "SP login nije uspeo u krugu $i."
    Assert-Equal (Invoke-SqlScalar "DECLARE @T TABLE(IDKorisnika int, KorisnickoIme nvarchar(50), Ime nvarchar(50), Prezime nvarchar(50), Uloga nvarchar(30)); INSERT INTO @T EXEC dbo.PrijaviKorisnika N'$adminUser', N'namerno-pogresno'; SELECT COUNT(*) FROM @T;") 0 "SP je prihvatio pogrešnu lozinku u krugu $i."
}

$constraintTests = @(
    @{ Name = "duplikat šifre discipline"; Sql = "INSERT INTO dbo.SportskaDisciplina(Sifra,Naziv,Aktivna) VALUES(N'KOS',N'Test duplikat',1);" },
    @{ Name = "JMBG sa slovom"; Sql = "INSERT INTO dbo.Kandidat(JMBG,Ime,Prezime,DatumRodjenja,Pol,Drzavljanstvo,Adresa,KontaktTelefon) VALUES('123456789012A',N'Test',N'Test','20000101',N'M',N'Srbija',N'Adresa',N'064111222');" },
    @{ Name = "nedozvoljen status"; Sql = "UPDATE dbo.ZahtevZaUclanjenje SET StatusZahteva=N'Nevažeći' WHERE IDZahteva=1;" },
    @{ Name = "nedozvoljen test"; Sql = "UPDATE dbo.ZahtevZaUclanjenje SET RezultatTestaSposobnosti=N'Nevažeći' WHERE IDZahteva=1;" },
    @{ Name = "neispravna sezona"; Sql = "UPDATE dbo.ZahtevZaUclanjenje SET Sezona='2026-27' WHERE IDZahteva=1;" }
)

foreach ($test in $constraintTests) {
    for ($i = 1; $i -le 10; $i++) {
        $escaped = $test.Sql.Replace("'", "''")
        $query = "BEGIN TRAN; BEGIN TRY EXEC(N'$escaped'); SELECT 0; END TRY BEGIN CATCH SELECT 1; END CATCH; IF XACT_STATE() <> 0 ROLLBACK;"
        Assert-Equal (Invoke-SqlScalar $query) 1 "SQL ograničenje '$($test.Name)' nije odbilo unos u krugu $i."
    }
}

"DATABASE PASS: clean install, zaštitni drugi run, šema/seed, SP login 20 poziva i 50 provera SQL ograničenja." |
    Tee-Object -FilePath $logPath -Append
