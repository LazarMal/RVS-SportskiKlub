using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using KlasePodataka;

namespace DBUtils.Repozitorijumi
{
    public class ZahtevZaUclanjenjeRepozitorijum : OsnovnaTehnoloskaKlasa, IZahtevZaUclanjenjeRepozitorijum
    {
        public List<ZahtevZaUclanjenje> DajSve(string filter = null)
        {
            using (var db = new SportskiKlubKontekst())
            {
                IQueryable<ZahtevZaUclanjenje> upit = db.ZahteviZaUclanjenje
                    .Include(z => z.Kandidat)
                    .Include(z => z.SportskaDisciplina)
                    .Include(z => z.Dokumentacija)
                    .Include(z => z.RoditeljiStaratelji)
                    .Include(z => z.IstorijaStatusa);

                if (!string.IsNullOrWhiteSpace(filter))
                {
                    upit = upit.Where(z =>
                        z.BrojZahteva.Contains(filter) ||
                        z.JMBGKandidata.Contains(filter) ||
                        z.Kandidat.Ime.Contains(filter) ||
                        z.Kandidat.Prezime.Contains(filter) ||
                        z.SportskaDisciplina.Naziv.Contains(filter) ||
                        z.StatusZahteva.Contains(filter));
                }

                return upit
                    .OrderByDescending(z => z.DatumPodnosenja)
                    .ThenByDescending(z => z.IDZahteva)
                    .ToList();
            }
        }

        public ZahtevZaUclanjenje DajPoId(int idZahteva)
        {
            using (var db = new SportskiKlubKontekst())
            {
                return db.ZahteviZaUclanjenje
                    .Include(z => z.Kandidat)
                    .Include(z => z.SportskaDisciplina)
                    .Include(z => z.Dokumentacija)
                    .Include(z => z.RoditeljiStaratelji)
                    .Include(z => z.IstorijaStatusa)
                    .FirstOrDefault(z => z.IDZahteva == idZahteva);
            }
        }

        public int DodajSaDetaljima(
            Kandidat kandidat,
            ZahtevZaUclanjenje zahtev,
            IList<Dokumentacija> dokumentacija,
            RoditeljStaratelj roditeljStaratelj,
            string korisnickoIme)
        {
            using (var db = new SportskiKlubKontekst())
            using (DbContextTransaction transakcija = db.Database.BeginTransaction())
            {
                try
                {
                    SacuvajKandidata(db, kandidat);

                    zahtev.JMBGKandidata = kandidat.JMBG;
                    zahtev.DatumPodnosenja = DateTime.Today;
                    zahtev.BrojZahteva = "TMP-" + Guid.NewGuid().ToString("N").Substring(0, 20);
                    zahtev.StatusZahteva = "U obradi";

                    db.ZahteviZaUclanjenje.Add(zahtev);
                    db.SaveChanges();

                    zahtev.BrojZahteva = string.Format(
                        "ZSK-{0}-{1:000000}",
                        zahtev.DatumPodnosenja.Year,
                        zahtev.IDZahteva);

                    DodajDokumentaciju(db, zahtev.IDZahteva, dokumentacija);

                    if (roditeljStaratelj != null)
                    {
                        roditeljStaratelj.IDZahteva = zahtev.IDZahteva;
                        db.RoditeljiStaratelji.Add(roditeljStaratelj);
                    }

                    db.IstorijaStatusaZahteva.Add(new IstorijaStatusaZahteva
                    {
                        IDZahteva = zahtev.IDZahteva,
                        StariStatus = null,
                        NoviStatus = "U obradi",
                        DatumPromene = DateTime.Now,
                        KorisnickoIme = korisnickoIme,
                        Napomena = "Zahtev je evidentiran."
                    });

                    db.SaveChanges();
                    transakcija.Commit();
                    return zahtev.IDZahteva;
                }
                catch
                {
                    transakcija.Rollback();
                    throw;
                }
            }
        }

        public void IzmeniSaDetaljima(
            Kandidat kandidat,
            ZahtevZaUclanjenje zahtev,
            IList<Dokumentacija> dokumentacija,
            RoditeljStaratelj roditeljStaratelj,
            string korisnickoIme)
        {
            using (var db = new SportskiKlubKontekst())
            using (DbContextTransaction transakcija = db.Database.BeginTransaction())
            {
                try
                {
                    ZahtevZaUclanjenje postojeci = db.ZahteviZaUclanjenje
                        .Include(z => z.Dokumentacija)
                        .Include(z => z.RoditeljiStaratelji)
                        .FirstOrDefault(z => z.IDZahteva == zahtev.IDZahteva);

                    if (postojeci == null)
                    {
                        throw new InvalidOperationException("Zahtev ne postoji.");
                    }

                    if (zahtev.StatusZahteva == "Odobren")
                    {
                        throw new InvalidOperationException(
                            "Odobren zahtev se pre izmene vraća na proveru; status Odobren može postaviti samo servis poslovnog pravila.");
                    }

                    SacuvajKandidata(db, kandidat);

                    string stariStatus = postojeci.StatusZahteva;

                    postojeci.JMBGKandidata = kandidat.JMBG;
                    postojeci.IDSportskeDiscipline = zahtev.IDSportskeDiscipline;
                    postojeci.Sezona = zahtev.Sezona;
                    postojeci.MestoKluba = zahtev.MestoKluba;
                    postojeci.DatumSportskogPregleda = zahtev.DatumSportskogPregleda;
                    postojeci.RezultatTestaSposobnosti = zahtev.RezultatTestaSposobnosti;
                    postojeci.StatusZahteva = zahtev.StatusZahteva;
                    postojeci.Napomena = zahtev.Napomena;

                    SinhronizujDokumentaciju(db, postojeci, dokumentacija);
                    SinhronizujRoditelja(db, postojeci, roditeljStaratelj);

                    if (!string.Equals(stariStatus, postojeci.StatusZahteva, StringComparison.Ordinal))
                    {
                        DodajIstoriju(
                            db,
                            postojeci.IDZahteva,
                            stariStatus,
                            postojeci.StatusZahteva,
                            korisnickoIme,
                            "Status je promenjen iz forme za izmenu.");
                    }

                    db.SaveChanges();
                    transakcija.Commit();
                }
                catch
                {
                    transakcija.Rollback();
                    throw;
                }
            }
        }

        public void PromeniStatusBezOdobravanja(
            int idZahteva,
            string noviStatus,
            string korisnickoIme,
            string napomena)
        {
            if (string.Equals(noviStatus, "Odobren", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Status Odobren može postaviti samo servis poslovnog pravila.");
            }

            SacuvajPromenuStatusa(idZahteva, noviStatus, korisnickoIme, napomena);
        }

        public void PotvrdiOdobrenjePoslePoslovneProvere(
            int idZahteva,
            string korisnickoIme,
            string napomena)
        {
            SacuvajPromenuStatusa(idZahteva, "Odobren", korisnickoIme, napomena);
        }

        private static void SacuvajPromenuStatusa(
            int idZahteva,
            string noviStatus,
            string korisnickoIme,
            string napomena)
        {
            using (var db = new SportskiKlubKontekst())
            using (DbContextTransaction transakcija = db.Database.BeginTransaction())
            {
                try
                {
                    ZahtevZaUclanjenje zahtev = db.ZahteviZaUclanjenje.Find(idZahteva);

                    if (zahtev == null)
                    {
                        throw new InvalidOperationException("Zahtev ne postoji.");
                    }

                    string stariStatus = zahtev.StatusZahteva;

                    if (string.Equals(stariStatus, noviStatus, StringComparison.Ordinal))
                    {
                        return;
                    }

                    zahtev.StatusZahteva = noviStatus;
                    DodajIstoriju(db, idZahteva, stariStatus, noviStatus, korisnickoIme, napomena);

                    db.SaveChanges();
                    transakcija.Commit();
                }
                catch
                {
                    transakcija.Rollback();
                    throw;
                }
            }
        }

        public void Obrisi(int idZahteva)
        {
            using (var db = new SportskiKlubKontekst())
            {
                ZahtevZaUclanjenje zahtev = db.ZahteviZaUclanjenje.Find(idZahteva);

                if (zahtev == null)
                {
                    return;
                }

                db.ZahteviZaUclanjenje.Remove(zahtev);
                db.SaveChanges();
            }
        }

        private static void SacuvajKandidata(SportskiKlubKontekst db, Kandidat kandidat)
        {
            Kandidat postojeci = db.Kandidati.Find(kandidat.JMBG);

            if (postojeci == null)
            {
                db.Kandidati.Add(kandidat);
                return;
            }

            db.Entry(postojeci).CurrentValues.SetValues(kandidat);
        }

        private static void DodajDokumentaciju(
            SportskiKlubKontekst db,
            int idZahteva,
            IEnumerable<Dokumentacija> dokumentacija)
        {
            foreach (Dokumentacija stavka in dokumentacija ?? Enumerable.Empty<Dokumentacija>())
            {
                stavka.IDZahteva = idZahteva;
                db.Dokumentacija.Add(stavka);
            }
        }

        private static void SinhronizujDokumentaciju(
            SportskiKlubKontekst db,
            ZahtevZaUclanjenje zahtev,
            IEnumerable<Dokumentacija> novaDokumentacija)
        {
            var noveStavke = (novaDokumentacija ?? Enumerable.Empty<Dokumentacija>())
                .ToDictionary(d => d.NazivDokumenta, StringComparer.OrdinalIgnoreCase);

            foreach (Dokumentacija postojeca in zahtev.Dokumentacija.ToList())
            {
                Dokumentacija nova;

                if (noveStavke.TryGetValue(postojeca.NazivDokumenta, out nova))
                {
                    postojeca.Dostavljeno = nova.Dostavljeno;
                    noveStavke.Remove(postojeca.NazivDokumenta);
                }
                else
                {
                    db.Dokumentacija.Remove(postojeca);
                }
            }

            foreach (Dokumentacija nova in noveStavke.Values)
            {
                nova.IDZahteva = zahtev.IDZahteva;
                db.Dokumentacija.Add(nova);
            }
        }

        private static void SinhronizujRoditelja(
            SportskiKlubKontekst db,
            ZahtevZaUclanjenje zahtev,
            RoditeljStaratelj noviRoditelj)
        {
            RoditeljStaratelj postojeci = zahtev.RoditeljiStaratelji.FirstOrDefault();

            if (noviRoditelj == null)
            {
                if (postojeci != null)
                {
                    db.RoditeljiStaratelji.Remove(postojeci);
                }

                return;
            }

            if (postojeci == null)
            {
                noviRoditelj.IDZahteva = zahtev.IDZahteva;
                db.RoditeljiStaratelji.Add(noviRoditelj);
                return;
            }

            postojeci.ImePrezime = noviRoditelj.ImePrezime;
            postojeci.JMBG = noviRoditelj.JMBG;
            postojeci.Srodstvo = noviRoditelj.Srodstvo;
            postojeci.KontaktTelefon = noviRoditelj.KontaktTelefon;
            postojeci.Email = noviRoditelj.Email;
        }

        private static void DodajIstoriju(
            SportskiKlubKontekst db,
            int idZahteva,
            string stariStatus,
            string noviStatus,
            string korisnickoIme,
            string napomena)
        {
            db.IstorijaStatusaZahteva.Add(new IstorijaStatusaZahteva
            {
                IDZahteva = idZahteva,
                StariStatus = stariStatus,
                NoviStatus = noviStatus,
                DatumPromene = DateTime.Now,
                KorisnickoIme = korisnickoIme,
                Napomena = napomena
            });
        }

        public override string DajOpisObrade()
        {
            return "Repozitorijum zahteva koristi Entity Framework i transakcije za master-detail podatke.";
        }
    }
}
