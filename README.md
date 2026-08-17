# Evidencija učlanjivanja kandidata u sportski klub

Seminarski projekat za predmet Razvoj višeslojnog softvera. Aplikacija je ASP.NET MVC 5 / .NET Framework 4.7.2 sistem sa SQL Server bazom, Entity Framework 6, REST servisom, Repository Pattern-om i izdvojenom poslovnom logikom.

## Preduslovi

- Windows 10/11
- Visual Studio 2022 sa workload-om **ASP.NET and web development**
- SQL Server Express LocalDB (`(localdb)\MSSQLLocalDB`)
- .NET Framework 4.7.2 Developer Pack

## Instalacija

1. U SQL Server Management Studio-u ili Visual Studio SQL alatu izvršiti `1_SlojPodataka/BazaPodataka/InstalacijaBaze.sql`.
2. Skript bezbedno prekida rad ako baza `RVS2026SportskiKlub` već postoji; ne briše postojeće podatke.
3. Otvoriti `4_PrezentacioniSloj/KorisnickiInterfejs/KorisnickiInterfejs.sln`.
4. Pokrenuti **Restore NuGet Packages**, zatim **Clean Solution** i **Rebuild Solution**.
5. Kao višestruke startup projekte postaviti:
   - `RESTServis` — Start;
   - `KorisnickiInterfejs` — Start.
6. REST servis koristi `https://localhost:44346/`, a MVC aplikacija `https://localhost:44334/`. Ako Visual Studio ponudi poverenje IIS Express sertifikatu, potvrditi ga.

Konekcija `SportskiKlubKonekcija` je već usklađena u oba Web.config fajla.
`NuGet.config` usmerava sve projekte iz glavnog solutiona na jedan korenski `packages` folder, pa build ne zavisi od paketa iz originalnog računara.

## Test nalozi

- administrator: `admin` / `admin123`
- referent: `referent` / `referent123`

Prijava stvarno koristi `SqlConnection`, `SqlCommand` i Stored Procedure `dbo.PrijaviKorisnika`.

## Arhitektura i kriterijumi

- `1_SlojPodataka` — EF klase, repozitorijumi i SQL instalacija;
- `2_SlojPoslovneLogike` — pravilo za odobravanje i REST klijent parametara;
- `3_SlojServisa` — DTO mapiranje, CRUD REST API i JSON parametri;
- `4_PrezentacioniSloj` — MVC ViewModel, master-detail forme i štampe;
- `5_Testovi` — obavezni granični scenariji poslovnog pravila.

Tri tražena načina pristupa bazi imaju stvarnu funkciju:

1. standardni SqlClient + Stored Procedure — prijava korisnika;
2. `SportskaDisciplinaRepozitorijum : TabelaKlasa` + parametrizovan SQL upit — dropdown šifarnika;
3. Entity Framework — CRUD i transakcioni master-detail rad sa zahtevima.

## Poslovno pravilo

Zahtev se može odobriti samo kada:

- datum sportskog pregleda nije u budućnosti;
- pregled nije stariji od X meseci (tačno X meseci je dozvoljeno);
- rezultat testa sposobnosti za izabranu disciplinu je `Položen`;
- dostavljene su potvrda pregleda i evidencija položenog testa.

X se nalazi u `RESTServis/App_Data/poslovna_pravila.json`. Poslovna logika ga ne čita direktno, već ga dobija pozivom `GET /api/parametri/poslovna-pravila`.

Generički MVC Edit i REST PUT ne mogu postaviti status `Odobren`. To radi samo akcija poslovnog pravila i svaka stvarna promena upisuje istoriju statusa.

## REST rute

- `GET /api/zahtevi?filter=tekst`
- `GET /api/zahtevi/{id}`
- `POST /api/zahtevi`
- `PUT /api/zahtevi/{id}`
- `DELETE /api/zahtevi/{id}`
- `GET /api/parametri/poslovna-pravila`

## Test poslovnog pravila

Postaviti `PoslovnaLogikaTestovi` kao startup projekat i pokrenuti ga. Test-runner proverava:

- validan zahtev;
- pregled stariji od X meseci;
- test `Nije položen`;
- test `Nije realizovan`;
- pregled star tačno X meseci (inkluzivna granica).

Za svaki scenario proverava se i da su pozvani data sloj i servis parametara.

## Štampa

U spisku zahteva dostupne su tri odvojene funkcije:

- štampa svih zahteva;
- štampa trenutno filtriranog spiska;
- parametarska štampa pojedinačnog zahteva u formi registrovanog dokumenta.

Detaljna usklađenost sa kriterijumima vodi se u `REQUIREMENTS.md`; status `VERIFIED` se dodeljuje tek nakon izvršavanja na Windows/SQL Server okruženju.
Tačan redosled i očekivani ishodi svih runtime provera nalaze se u `RUNTIME_TEST_CHECKLIST.md`.
