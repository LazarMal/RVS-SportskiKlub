# RVS - specifikacija projekta

## Autoritet

Ovaj dokument sažima obavezni ugovor projekta na osnovu profesorkinog dokumenta od 10. 5. 2026, precizirane prijave teme i početnog audita koda. U slučaju neslaganja važi redosled: profesorkin dokument, prijavljena tema, finalni kod, stara dokumentacija, stari razgovor.

## Tema i poslovni kontekst

- Naziv teme: **Web aplikacija za evidenciju učlanjivanja kandidata u sportski klub**
- Poslovni proces: **Učlanjivanje kandidata u sportski klub**
- Poslovni dokument: **Zahtev za učlanjivanje u sportski klub**
- Fiktivni klub za demonstraciju: **Sportski klub Mladost, Zrenjanin**

## Glavno poslovno pravilo

Zahtev može dobiti status `Odobren` samo ako su istovremeno ispunjeni sledeći uslovi:

1. evidentiran je datum sportskog pregleda;
2. pregled nije stariji od eksterno zadatog broja meseci;
3. rezultat testa sposobnosti je `Položen`;
4. dostavljena je potvrda o sportskom pregledu;
5. dostavljena je evidencija/potvrda o položenom testu sposobnosti.

Parametar se pribavlja preko REST servisa iz JSON fajla:

```json
{
  "MaksimalnaStarostSportskogPregledaMeseci": 6,
  "StarosnaGranicaZaSaglasnost": 18
}
```

Granični datum je uključen: pregled obavljen tačno pre `X` meseci smatra se važećim. Datum pregleda u budućnosti nije dozvoljen. Nijedan MVC ili REST put ne sme omogućiti zaobilaženje pravila pri prelasku u status `Odobren`.

## Statusi i kontrolisani domeni

- Status zahteva: `U obradi`, `Na proveri`, `Odobren`, `Odbijen`
- Rezultat testa sposobnosti: `Položen`, `Nije položen`, `Nije realizovan`
- Pol: `M`, `Ž`
- Sportska disciplina se bira iz aktivnog šifarnika `SportskaDisciplina`.

## Relacioni model - ugovor

| Tabela | Uloga | Ključne obaveze |
| --- | --- | --- |
| `Kandidat` | poslovni entitet | JMBG, ime, prezime, rođenje, pol, državljanstvo, adresa, telefon, email |
| `SportskaDisciplina` | pravi šifarnik | jedinstven naziv i oznaka aktivnosti |
| `ZahtevZaUclanjenje` | glavna poslovna tabela/master | broj zahteva, kandidat, disciplina, datum, sezona, mesto, pregled, test, status, napomena |
| `Dokumentacija` | detail | više stavki dokumentacije uz jedan zahtev |
| `RoditeljStaratelj` | uslovni detail | podaci za maloletnog kandidata |
| `IstorijaStatusaZahteva` | sledljivost | svaka stvarna promena statusa |
| `Korisnik` | nezavisna tabela | login i uloga korisnika |

`BrojZahteva` je jedinstven i prikazuje se kao poslovni broj dokumenta. Veza `SportskaDisciplina 1:M ZahtevZaUclanjenje` dokazuje odnos glavne tabele i šifarnika. Veza `ZahtevZaUclanjenje 1:M Dokumentacija` je master-detail odnos.

## Obavezni podaci dokumenta

- broj zahteva i datum podnošenja;
- sezona i mesto kluba;
- ime, prezime, JMBG, datum rođenja, pol i državljanstvo kandidata;
- adresa, telefon i email;
- sportska disciplina;
- datum sportskog pregleda;
- rezultat testa sposobnosti;
- status i napomena;
- stavke priložene dokumentacije;
- roditelj/staratelj i saglasnost kada je kandidat maloletan;
- istorija statusa u detaljnom prikazu sistema.

## Dokumentacija uz zahtev

- Fotografija kandidata
- Dokaz identiteta
- Potvrda o sportskom pregledu
- Evidencija o položenom testu sposobnosti
- Saglasnost roditelja/staratelja
- Drugi dokument

## Četiri sloja

1. **Sloj podataka:** posebne biblioteke klasa, Repository Pattern i sva tri načina pristupa bazi.
2. **Sloj servisa:** pravi REST servis za CRUD i za parametre poslovne logike.
3. **Sloj poslovne logike:** posebna biblioteka; metod pravila poziva repozitorijum i REST klijent za parametar.
4. **Prezentacioni sloj:** ASP.NET MVC, multipage UI i ViewModeli za Create i Edit tokove.

## Dokaz tri načina rada sa bazom

| Način | Stvarni slučaj korišćenja |
| --- | --- |
| Standardni `SqlClient` + Stored Procedure | prijava korisnika ili druga jasno vidljiva funkcija čiji se rezultat stvarno mapira i koristi |
| Nasleđivanje `TabelaKlasa` + SQL upit | učitavanje aktivnih sportskih disciplina iz šifarnika za MVC padajuću listu |
| Entity Framework | CRUD zahteva, master-detail unos/izmena u transakciji i detaljni prikaz |

Poziv čiji se rezultat ignoriše nije dokaz kriterijuma.

## Tri štampe

1. `StampaSvih` - spisak svih zahteva.
2. `StampaFiltriranih` - isti spisak ograničen trenutno zadatim filterom.
3. `StampaZahteva(id)` - printer-friendly pojedinačni dokument koji prati prijavljeni obrazac i prikazuje master i detail podatke.

## Pravilo maloletnog kandidata

Ako je kandidat mlađi od eksterno zadate starosne granice, obavezni su podaci roditelja/staratelja i saglasnost. Ovo je dodatna uslovna validacija, a ne zamena za glavno poslovno pravilo.

## Jezik i zabrana stare teme

Domenski kod, baza, REST DTO polja i UI moraju biti na srpskom. U aktivnom finalnom projektu nisu dozvoljeni termini `LicnaKarta`, `Gradjanin`, `BrojNoveLK`, `BrojStareLK`, `DatumIstekaLK`, `RazlogIzdavanja`, `TipZahteva`, `MestoPodnosenja` niti sadržaj o izdavanju ličnih karata.

