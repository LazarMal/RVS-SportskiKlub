using System;
using System.Collections.Generic;
using System.Text;
using DBUtils.Repozitorijumi;
using KlasePodataka;
using PoslovnaLogika;

namespace PoslovnaLogikaTestovi
{
    internal static class Program
    {
        private static int Main()
        {
            Console.OutputEncoding = Encoding.UTF8;

            try
            {
                Proveri("validan zahtev", DateTime.Today.AddMonths(-1), "Položen", true);
                Proveri("pregled stariji od X meseci", DateTime.Today.AddMonths(-6).AddDays(-1), "Položen", false);
                Proveri("test nije položen", DateTime.Today.AddMonths(-1), "Nije položen", false);
                Proveri("test nije realizovan", DateTime.Today.AddMonths(-1), "Nije realizovan", false);
                Proveri("tačno X meseci - inkluzivna granica", DateTime.Today.AddMonths(-6), "Položen", true);

                Console.WriteLine("SVI TESTOVI SU PROŠLI.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("TEST NIJE PROŠAO: " + ex.Message);
                return 1;
            }
        }

        private static void Proveri(
            string naziv,
            DateTime datumPregleda,
            string rezultatTesta,
            bool ocekujeUspeh)
        {
            var repozitorijum = new LazniRepozitorijum
            {
                Zahtev = KreirajZahtev(datumPregleda, rezultatTesta)
            };
            var parametri = new LazniServisParametara();
            var servis = new OdobravanjeZahtevaServis(repozitorijum, parametri);

            RezultatPoslovnogPravila rezultat = servis.ProveriIOdobri(1, "test");

            Zahtev(rezultat.Uspesno == ocekujeUspeh, naziv + ": neočekivan rezultat pravila.");
            Zahtev(repozitorijum.BrojPozivaDajPoId == 1, naziv + ": data sloj nije pozvan tačno jednom.");
            Zahtev(parametri.BrojPoziva == 1, naziv + ": servis parametara nije pozvan tačno jednom.");
            Zahtev(repozitorijum.Odobren == ocekujeUspeh, naziv + ": status odobrenja nije očekivan.");

            Console.WriteLine("PROŠAO: " + naziv);
        }

        private static ZahtevZaUclanjenje KreirajZahtev(DateTime datumPregleda, string rezultatTesta)
        {
            return new ZahtevZaUclanjenje
            {
                IDZahteva = 1,
                DatumSportskogPregleda = datumPregleda,
                RezultatTestaSposobnosti = rezultatTesta,
                Dokumentacija = new List<Dokumentacija>
                {
                    new Dokumentacija
                    {
                        NazivDokumenta = OdobravanjeZahtevaServis.PotvrdaPregleda,
                        Dostavljeno = true
                    },
                    new Dokumentacija
                    {
                        NazivDokumenta = OdobravanjeZahtevaServis.EvidencijaTesta,
                        Dostavljeno = true
                    }
                }
            };
        }

        private static void Zahtev(bool uslov, string poruka)
        {
            if (!uslov)
            {
                throw new InvalidOperationException(poruka);
            }
        }

        private sealed class LazniServisParametara : IParametriPoslovnihPravilaServis
        {
            public int BrojPoziva { get; private set; }

            public ParametriPoslovnihPravila DajParametre()
            {
                BrojPoziva++;
                return new ParametriPoslovnihPravila
                {
                    MaksimalnaStarostSportskogPregledaMeseci = 6,
                    StarosnaGranicaZaSaglasnost = 18
                };
            }
        }

        private sealed class LazniRepozitorijum : IZahtevZaUclanjenjeRepozitorijum
        {
            public ZahtevZaUclanjenje Zahtev { get; set; }

            public int BrojPozivaDajPoId { get; private set; }

            public bool Odobren { get; private set; }

            public List<ZahtevZaUclanjenje> DajSve(string filter = null)
            {
                throw new NotSupportedException();
            }

            public ZahtevZaUclanjenje DajPoId(int idZahteva)
            {
                BrojPozivaDajPoId++;
                return Zahtev;
            }

            public int DodajSaDetaljima(
                Kandidat kandidat,
                ZahtevZaUclanjenje zahtev,
                IList<Dokumentacija> dokumentacija,
                RoditeljStaratelj roditeljStaratelj,
                string korisnickoIme)
            {
                throw new NotSupportedException();
            }

            public void IzmeniSaDetaljima(
                Kandidat kandidat,
                ZahtevZaUclanjenje zahtev,
                IList<Dokumentacija> dokumentacija,
                RoditeljStaratelj roditeljStaratelj,
                string korisnickoIme)
            {
                throw new NotSupportedException();
            }

            public void PromeniStatusBezOdobravanja(
                int idZahteva,
                string noviStatus,
                string korisnickoIme,
                string napomena)
            {
                throw new NotSupportedException();
            }

            public void PotvrdiOdobrenjePoslePoslovneProvere(
                int idZahteva,
                string korisnickoIme,
                string napomena)
            {
                Odobren = true;
            }

            public void Obrisi(int idZahteva)
            {
                throw new NotSupportedException();
            }
        }
    }
}
