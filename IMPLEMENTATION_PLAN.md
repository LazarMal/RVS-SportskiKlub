# RVS - implementacioni plan

## Pravila rada

- Nema automatskih commit-a ni push-a.
- Svaka faza završava statičkom proverom, a kada okruženje dozvoli i runtime proverom.
- `VERIFIED` se ne koristi bez dokaza.
- Dokumentacija se ne menja dok kod ne bude funkcionalno završen i zamrznut.
- Menja se najmanji skup fajlova potreban za trenutni kriterijum.

## Faze

### R0 - audit početnog ZIP-a

Status: **završen statički audit**.

Izlaz: `PROJECT_SPEC.md`, `REQUIREMENTS.md`, `ADAPTATION_PLAN.md`, ovaj plan. Runtime stare aplikacije nije potvrđen.

### R1 - finalni SQL i domenski ugovor

Status: **implementirano i statički provereno; SQL runtime čeka Windows/SQL Server**.

- kreirati jedinstveni instalacioni SQL;
- definisati tabele, PK/FK/UNIQUE/CHECK/NOT NULL i seed;
- dodati šifarnik sportskih disciplina;
- dodati Stored Procedure koja ima stvarnu funkcionalnu ulogu;
- statički proveriti sva polja iz prijavljenog dokumenta.

Gate: SQL i `PROJECT_SPEC.md` su 1:1 usklađeni.

### R2 - Entity Framework model

Status: **implementirano i statički usklađeno sa SQL šemom; runtime čeka Windows/SQL Server**.

- realizovati sportske EF klase i kontekst;
- uskladiti nazive baze i konekcije;
- ukloniti duple/neaktivne modele iz build-a;
- statički proveriti kardinalnosti i nullable ograničenja.

Gate: sve SQL kolone i veze imaju jednoznačan EF par.

### R3 - domain rename i data biblioteka

Status: **implementirano; aktivni build koristi sportski domen i ProjectReference veze**.

- preimenovati ključne klase/interfejse/metode;
- sačuvati Repository Pattern;
- izbaciti stare sportski-nevažeće property-je;
- prebaciti naše DLL reference na `ProjectReference`.

Gate: data projekti nemaju aktivne termine lične karte niti binarne reference među našim projektima.

### R4 - tri načina rada sa bazom

Status: **implementirano; sva tri načina imaju stvarnog pozivaoca, runtime još nije izvršen**.

- SqlClient + Stored Procedure: stvarni login ili drugi glavni tok;
- `TabelaKlasa` + parametrizovani SQL upit: šifarnik disciplina;
- EF: CRUD i master-detail zahteva;
- ukloniti svaki poziv čiji se rezultat ignoriše.

Gate: za svaki način postoji pozivalac i konkretan runtime test.

### R5 - REST servis

Status: **implementirano; CRUD, DTO i parametarski endpoint čekaju runtime HTTP test**.

- parametarski endpoint iz JSON-a;
- sportski DTO i CRUD endpointi;
- server-side validacije i kontrola statusa;
- dokumentovati test payload-e.

Gate: GET, GET by ID, POST, PUT i DELETE prolaze na test podacima.

### R6 - poslovna logika

Status: **implementirano; dodat deterministički test-runner, izvršavanje čeka .NET Framework**.

- klijent parametara poziva REST;
- repozitorijum učitava zahtev i dokumentaciju;
- implementirati inkluzivnu granicu tačno X meseci;
- sprečiti `Odobren` kada pravilo nije zadovoljeno;
- zadržati maloletnost kao dodatno pravilo.

Gate: prolazi validan slučaj, star pregled, nepoložen test, nerealizovan test i tačno X meseci.

### R7 - MVC i ViewModel

Status: **implementirano; Create/Edit dele isti sportski ViewModel**.

- jedan sportski ViewModel za Create/Edit;
- dropdown disciplina iz pravog šifarnika;
- server-side validacija svih domena;
- kontroleri bez direktnog upravljanja EF grafom.

Gate: nevalidan model ostaje na formi sa ispravnim opcijama; ključni tokovi ne binduju EF entitet direktno.

### R8 - CRUD i master-detail

Status: **implementirano; transakcije i sinhronizacija detalja čekaju runtime proveru rollback-a**.

- Create mastera i svih detail stavki na jednoj formi;
- transakcija u data sloju;
- Edit sinhronizuje detalje bez orphan zapisa;
- Delete briše ceo graf prema definisanom FK ponašanju;
- Details prikazuje sve delove.

Gate: namerno izazvana greška detail unosa vraća celu transakciju.

### R9 - validacije

Status: **implementirano u MVC, REST DTO/domenskom toku i SQL ograničenjima; runtime negativni testovi čekaju**.

- Data Annotations + server-side domenska validacija;
- JMBG, email, telefon, datum, sezona, status, rezultat testa;
- jedinstven broj zahteva;
- uslovna sportska dokumentacija i roditelj/staratelj;
- JavaScript/regex opcioni bodovi.

Gate: evidentirani pozitivni, negativni i granični testovi.

### R10 - štampe

Status: **implementirane tri odvojene funkcije; vizuelni print-preview test čeka browser**.

- štampa svih;
- štampa trenutno filtriranog spiska;
- pojedinačni zahtev kao prijavljeni dokument sa master-detail podacima;
- print CSS i pregled preloma.

Gate: tri funkcije su jasno dostupne i vizuelno proverene u print preview-u.

### R11 - cleanup stare teme

Status: **završen cleanup aktivnog stabla; legacy pretraga nema pogodaka, finalni paket mora biti bez `.git`, `bin`, `obj` i `.vs`**.

- globalna pretraga termina lične karte;
- uklanjanje neaktivnog starog modela, starih seedova i starih prikaza;
- uklanjanje autora, starog remote-a i apsolutnih putanja tek posle provere upotrebe;
- bez brisanja funkcionalno potrebnih fajlova naslepo.

Gate: nema aktivnih legacy pogodaka; eventualni arhivski trag je jasno izvan build-a i predajnog paketa.

### R12 - puna regresija

Status: **u toku na Windowsu; potvrđeni su čista SQL instalacija, NuGet Restore, Clean/Rebuild 8/8, REST parametarski endpoint, stvarni login, EF lista i UTF-8 srpska latinica**.

Tokom prvog runtime prolaza pronađeno je i ispravljeno:

- pouzdano kopiranje Roslyn kompajlera u `bin/roslyn` oba web projekta;
- EF6 provider registracija u ulaznom MVC `Web.config` fajlu;
- globalno UTF-8 čitanje Razor fajlova i srpska latinica;
- početna REST MVC ruta ka postojećem prikazu.

- clean DB install;
- NuGet restore i Clean/Rebuild solutiona;
- login/logout/sesija;
- CRUD/master-detail/status/istorija/pravilo;
- tri data pristupa;
- REST;
- tri štampe;
- maloletan/punoletan kandidat.

Gate: svaki red matrice ima dokaz ili eksplicitno prijavljen problem.

### R13 - freeze koda

- git status/diff/legacy pregled;
- commit samo uz izričitu potvrdu korisnika;
- bez novih funkcionalnih izmena posle freeze-a osim potvrđene greške.

### R14 - seminarska dokumentacija

- koristiti staru DOCX samo kao strukturni uzor;
- svi dijagrami, slike i listing koda moraju doći iz finalnog projekta;
- obavezno renderovanje i vizuelni pregled svake stranice.

### R15 - finalni paket

- kod bez `bin`, `obj`, `.vs` i tuđeg Git identiteta;
- dokumentacija, SQL i programski kod;
- uputstvo za instalaciju/demonstraciju;
- završni audit `STATIC + BUILD + RUNTIME + DOCUMENTATION`.
