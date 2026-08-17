# RVS - runtime test protokol

Ovaj protokol se izvršava na Windows 10/11 računaru sa Visual Studio 2022, .NET Framework 4.7.2 Developer Pack-om i SQL Server Express LocalDB-om. U `REQUIREMENTS.md` menjati `IMPLEMENTED` u `VERIFIED` samo kada je konkretan korak prošao i dokaz je sačuvan.

## 1. Čista instalacija baze

1. U SSMS-u proveriti da baza `RVS2026SportskiKlub` ne postoji.
2. Izvršiti ceo `1_SlojPodataka/BazaPodataka/InstalacijaBaze.sql`.
3. Potvrditi da skript nema grešku i da postoje tabele:
   - `Kandidat`
   - `SportskaDisciplina`
   - `ZahtevZaUclanjenje`
   - `Dokumentacija`
   - `RoditeljStaratelj`
   - `IstorijaStatusaZahteva`
   - `Korisnik`
4. Potvrditi da postoji `dbo.PrijaviKorisnika` i da šifarnik ima šest aktivnih disciplina.
5. Ponovo pokrenuti skript i potvrditi da bezbedno prekida rad bez brisanja postojeće baze.

Dokaz: screenshot uspešnog prvog izvršavanja, liste tabela/procedure i očekivane zaštitne poruke drugog izvršavanja.

## 2. NuGet restore i clean build

1. Otvoriti `4_PrezentacioniSloj/KorisnickiInterfejs/KorisnickiInterfejs.sln`.
2. Pokrenuti `Restore NuGet Packages`.
3. Izabrati `Build > Clean Solution`.
4. Izabrati `Build > Rebuild Solution`.
5. Potvrditi `0 failed` i da se svih osam projekata učitalo bez upozorenja o nedostajućim referencama.

Dokaz: screenshot završnog Build Output-a.

## 3. Pokretanje oba web projekta

U Solution Properties postaviti više startup projekata:

- `RESTServis` — Start
- `KorisnickiInterfejs` — Start

Očekivane adrese:

- REST: `https://localhost:44346/`
- MVC: `https://localhost:44334/`

Potvrditi IIS Express sertifikat kada Visual Studio to zatraži. Otvoriti `https://localhost:44346/api/parametri/poslovna-pravila` i proveriti X=6 i starosnu granicu 18.

## 4. Login, logout i sesija

1. Otvoriti zaštićenu MVC rutu bez sesije i potvrditi preusmerenje na prijavu.
2. Pokušati pogrešnu lozinku i potvrditi poruku bez kreirane sesije.
3. Prijaviti se kao `referent` / `referent123`.
4. Odjaviti se i potvrditi da zaštićena ruta ponovo traži prijavu.
5. Prijaviti se kao `admin` / `admin123` i potvrditi dostupnost brisanja.

Ovim se istovremeno dokazuje SqlClient + Stored Procedure, jer UI zavisi od mapiranog rezultata procedure.

## 5. Tri načina pristupa bazi

1. **SqlClient + Stored Procedure:** uspešan i neuspešan login.
2. **TabelaKlasa + SQL upit:** otvoriti unos i potvrditi da dropdown prikazuje šest aktivnih disciplina iz baze.
3. **Entity Framework:** otvoriti listu, detalje, kreirati, izmeniti i obrisati zahtev.

Ne prihvatati samo postojanje koda kao dokaz; svaki od tri toka mora stvarno vratiti podatke.

## 6. MVC CRUD i master-detail

1. Kreirati punoletnog kandidata sa svih šest stavki dokumentacije na istoj formi.
2. Potvrditi automatski broj `ZSK-GGGG-xxxxxx`, početni status `U obradi` i početni zapis istorije.
3. Otvoriti detalje i potvrditi kandidata, disciplinu, dokumentaciju, roditelja kada postoji i istoriju.
4. Izmeniti kandidatove podatke, disciplinu, rezultat testa, status i `Dostavljeno` vrednosti.
5. Potvrditi da uklonjene/izmenjene detail stavke nisu ostale kao duplikati ili orphan zapisi.
6. Kao administrator obrisati zahtev i SQL upitom potvrditi da nema njegove dokumentacije, roditelja ni istorije.

## 7. Rollback master-detail transakcije

Pre testa zabeležiti broj redova u tabelama `ZahtevZaUclanjenje` i `Dokumentacija`. Privremeno u test bazi napraviti trigger koji namerno prekida unos detalja:

```sql
CREATE TRIGGER dbo.TR_TEST_Rollback_Dokumentacija
ON dbo.Dokumentacija
INSTEAD OF INSERT
AS
    THROW 51000, N'Namerno izazvana greška detail unosa.', 1;
```

1. Kroz MVC pokušati unos novog zahteva.
2. Očekivati poruku da čuvanje nije uspelo.
3. Potvrditi da se broj zahteva i dokumentacije nije promenio i da nije ostao kandidat korišćen samo u neuspelom pokušaju.
4. Obavezno ukloniti test trigger:

```sql
DROP TRIGGER dbo.TR_TEST_Rollback_Dokumentacija;
```

## 8. Poslovno pravilo - pet obaveznih slučajeva

Najpre pokrenuti konzolni projekat `PoslovnaLogikaTestovi` i očekivati `SVI TESTOVI SU PROŠLI.` Zatim kroz MVC potvrditi iste slučajeve:

1. važeći pregled + `Položen` + obe potvrde dostavljene → `Odobren`;
2. pregled stariji od šest meseci → odbijena promena statusa;
3. `Nije položen` → odbijena promena statusa;
4. `Nije realizovan` → odbijena promena statusa;
5. datum pregleda tačno šest meseci pre dana testa → `Odobren`.

Za odobren slučaj potvrditi novu stavku istorije. Promeniti JSON X sa 6 na 5, ponoviti relevantan test i dokazati da poslovna logika koristi novi REST parametar; zatim vratiti X na 6.

## 9. Zaštita statusa Odobren

1. U MVC Edit formi pokušati direktno postaviti `Odobren` izmenom POST vrednosti i očekivati odbijanje.
2. REST PUT payload sa `"StatusZahteva": "Odobren"` mora vratiti `400 Bad Request`.
3. Dugme `Proveri pravilo i odobri` sme postaviti status samo kada svih uslovi prolaze.
4. Izmena već odobrenog zahteva mora ga vratiti na `Na proveri` pre nove provere.

## 10. Uslov roditelja/staratelja

1. Punoletan kandidat bez roditelja i saglasnosti prolazi validaciju.
2. Maloletan kandidat bez roditelja ne prolazi.
3. Maloletan kandidat sa delimičnim podacima ne prolazi.
4. Maloletan kandidat sa potpunim podacima, ali bez označene saglasnosti ne prolazi.
5. Maloletan kandidat sa potpunim podacima i saglasnošću prolazi.
6. Ponoviti negativan i pozitivan slučaj i kroz REST POST/PUT.

## 11. REST CRUD

Koristiti Postman ili Visual Studio `.http` klijent. Datume prilagoditi danu testiranja.

- `GET /api/zahtevi`
- `GET /api/zahtevi/1`
- `GET /api/zahtevi?filter=Košarka`
- `POST /api/zahtevi` sa punim DTO objektom, dokumentacijom i po potrebi roditeljem
- `PUT /api/zahtevi/{id}` sa punim DTO objektom i dozvoljenim statusom
- `DELETE /api/zahtevi/{id}`

Posle svakog poziva potvrditi stvarno stanje u bazi i sledeći GET odgovor.

## 12. Negativne validacije

U MVC i REST toku proveriti najmanje:

- prazan obavezan podatak;
- JMBG kraći/duži od 13 cifara i JMBG sa slovom;
- neispravan email;
- neispravan telefon;
- budući datum rođenja i sportskog pregleda;
- sezona pogrešnog formata i sezona čiji drugi deo nije naredna godina;
- nepostojeća/neaktivna sportska disciplina;
- nedozvoljen rezultat testa;
- nedozvoljen status;
- duplikat broja zahteva/korisničkog imena/šifre discipline direktno u SQL-u.

## 13. Tri štampe

1. Otvoriti `Štampa svih` i proveriti sve zahteve.
2. Postaviti filter, otvoriti `Štampa filtriranih` i proveriti da je skup isti kao u MVC listi.
3. Otvoriti pojedinačnu štampu i proveriti sva polja prijavljenog dokumenta, dokumentaciju, roditelja i potpise.
4. U browser Print Preview proveriti A4 prelom, čitljivost i da dugmad nisu odštampana.

## 14. Završetak R12

Tek kada svi koraci prođu:

1. upisati konkretan dokaz uz svaki red `REQUIREMENTS.md`;
2. promeniti samo dokazane stavke u `VERIFIED`;
3. evidentirati svaki problem bez prećutkivanja;
4. ponoviti Clean/Rebuild posle poslednje popravke;
5. proglasiti code freeze i tek tada početi finalni DOCX.
