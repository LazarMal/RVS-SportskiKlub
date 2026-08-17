# RVS - plan kontrolisane prerade

## R0 nalaz

Postojeći projekat daje upotrebljivu četvoroslojnu osnovu, ali nije spreman za prosto presvlačenje. Funkcionalni rizici koji moraju biti rešeni pre finalizacije:

- nema pravog šifarnika povezanog sa glavnom tabelom;
- rezultat Stored Procedure/DBUtils poziva se u više tokova ignoriše;
- DBUtils trenutno ne demonstrira traženo izvršavanje običnog SQL upita kao zaseban način;
- poslovna logika direktno koristi `HttpContext` i `File.ReadAllText`, umesto REST servisa;
- glavno sportsko poslovno pravilo ne postoji;
- Create i Edit koriste različite modele, a Edit direktno binduje EF entitet;
- kontroler za Create meša UI, business i data odgovornosti;
- štampe nisu razdvojene u tačno tri tražene funkcije;
- projekti referenciraju `bin\Debug` DLL-ove, a REST ima i apsolutnu putanju sa računara autora;
- stari Git remote, namespace-i, baza i celokupan domen lične karte su aktivni;
- clean build i runtime ne mogu se dokazati u ovom Linux okruženju jer nisu dostupni MSBuild/.NET Framework ni SQL Server; završni runtime gate mora se izvršiti na Windowsu sa Visual Studio 2022 i SQL Server/LocalDB.

## Šta čuvamo

- ASP.NET MVC 5 i ASP.NET Web API 2 tehnološki izbor;
- podelu na data, business, service i presentation sloj;
- EF6, Repository Pattern, sesiju i autorizacioni filter;
- osnovnu organizaciju CRUD ruta;
- 1:M koncept Zahtev-Dokumentacija;
- istoriju statusa i uslovne podatke roditelja/staratelja;
- Bootstrap/Razor osnovu i JavaScript validacije gde su ispravne.

## Šta semantički menjamo

| Stara tema | Novi domen |
| --- | --- |
| `Gradjanin` | `Kandidat` |
| `Zahtev` | `ZahtevZaUclanjenje` |
| `JMBGGradjanina` | `JMBGKandidata` |
| `RazlogIzdavanja` | `Sezona` |
| `TipZahteva` | veza ka `SportskaDisciplina` |
| `MestoPodnosenja` | `MestoKluba` |
| `BrojNoveLK` | `RezultatTestaSposobnosti` |
| `DatumIstekaNoveLK` | `DatumSportskogPregleda` |
| dokumenti lične karte | sportska dokumentacija iz specifikacije |
| `MapiranjeLicneKarteKlasa` | DTO/mapiranje zahteva za učlanjivanje |
| baza `RVS2026LicnaKartaV1` | baza `RVS2026SportskiKlub` |

`BrojStareLK` i `DatumIstekaLK` se uklanjaju; nisu potrebni prijavljenom dokumentu. Dodaju se `BrojZahteva` i pravi šifarnik `SportskaDisciplina`, jer su obavezni za dokument i profesorkin relacioni kriterijum.

## Minimalna bezbedna tehnička strategija

1. Najpre zaključati finalni SQL i model podataka.
2. Zadržati EF6, ali ukloniti zavisnost aktivnog koda od starog EDMX modela kada novi model bude spreman; finalni izbor mora imati jednoznačan izvor entiteta i konekcije.
3. Pretvoriti binarne DLL reference između naših projekata u `ProjectReference` i napraviti jedan glavni solution.
4. U data biblioteci odvojiti tri demonstraciona slučaja pristupa podacima; nijedan dummy poziv.
5. Poslovno pravilo implementirati iza interfejsa sa zamenjivim repozitorijumom i REST klijentom za parametre.
6. MVC Create/Edit prebaciti na isti sportski ViewModel i transakcione repozitorijumske metode.
7. Tek posle funkcionalnog toka preraditi sve Razor prikaze, REST DTO polja i štampe.
8. Globalni legacy cleanup raditi kada svi novi pozivi budu povezani, uz prethodnu proveru referenci.

## Rizici i kontrole

| Rizik | Kontrola |
| --- | --- |
| rušenje veza pri domain rename-u | menjati po slojevima i posle svakog sloja uraditi statičku proveru |
| neusaglašeni SQL i EF model | jedna finalna SQL šema + ciljano poređenje svih tabela/kolona/veza |
| zaobilaženje pravila statusom | centralna metoda odobravanja i odbijanje direktnog prelaska |
| REST servis nije pokrenut | jasna greška korisniku i test nedostupnosti servisa |
| orphan detalji | transakcija i eksplicitna sinhronizacija kolekcija |
| neobnovljive package/DLL putanje | NuGet restore + `ProjectReference`, bez apsolutnih putanja |
| lažni PASS bez Windows runtime-a | status ostaje `IMPLEMENTED` dok ne prođe Visual Studio/SQL test |

