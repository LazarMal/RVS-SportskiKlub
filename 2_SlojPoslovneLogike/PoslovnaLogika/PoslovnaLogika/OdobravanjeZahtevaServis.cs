using System;
using System.Linq;
using DBUtils.Repozitorijumi;
using KlasePodataka;

namespace PoslovnaLogika
{
    public class OdobravanjeZahtevaServis : IOdobravanjeZahtevaServis
    {
        public const string PotvrdaPregleda = "Potvrda o sportskom pregledu";
        public const string EvidencijaTesta = "Evidencija o položenom testu sposobnosti";

        private readonly IZahtevZaUclanjenjeRepozitorijum repozitorijum;
        private readonly IParametriPoslovnihPravilaServis servisParametara;

        public OdobravanjeZahtevaServis(
            IZahtevZaUclanjenjeRepozitorijum repozitorijum,
            IParametriPoslovnihPravilaServis servisParametara)
        {
            this.repozitorijum = repozitorijum;
            this.servisParametara = servisParametara;
        }

        public RezultatPoslovnogPravila ProveriIOdobri(
            int idZahteva,
            string korisnickoIme)
        {
            // Obavezno se poziva sloj podataka.
            ZahtevZaUclanjenje zahtev = repozitorijum.DajPoId(idZahteva);

            if (zahtev == null)
            {
                return RezultatPoslovnogPravila.Neuspeh("Zahtev ne postoji.");
            }

            // Obavezno se poziva servis koji parametre čita iz spoljnog JSON fajla.
            ParametriPoslovnihPravila parametri = servisParametara.DajParametre();
            DateTime danas = DateTime.Today;
            DateTime najstarijiDozvoljeniDatum = danas.AddMonths(
                -parametri.MaksimalnaStarostSportskogPregledaMeseci);

            if (zahtev.DatumSportskogPregleda.Date > danas)
            {
                return RezultatPoslovnogPravila.Neuspeh(
                    "Sportski pregled ne može imati datum u budućnosti.");
            }

            // Tačno X meseci je dozvoljeno; neispravan je tek stariji datum.
            if (zahtev.DatumSportskogPregleda.Date < najstarijiDozvoljeniDatum)
            {
                return RezultatPoslovnogPravila.Neuspeh(
                    "Sportski pregled je stariji od dozvoljenih " +
                    parametri.MaksimalnaStarostSportskogPregledaMeseci + " meseci.");
            }

            if (!string.Equals(
                zahtev.RezultatTestaSposobnosti,
                "Položen",
                StringComparison.Ordinal))
            {
                return RezultatPoslovnogPravila.Neuspeh(
                    "Test sposobnosti za izabranu disciplinu nije položen.");
            }

            if (!DaLiJeDokumentDostavljen(zahtev, PotvrdaPregleda))
            {
                return RezultatPoslovnogPravila.Neuspeh(
                    "Nije dostavljena potvrda o sportskom pregledu.");
            }

            if (!DaLiJeDokumentDostavljen(zahtev, EvidencijaTesta))
            {
                return RezultatPoslovnogPravila.Neuspeh(
                    "Nije dostavljena evidencija o položenom testu sposobnosti.");
            }

            repozitorijum.PotvrdiOdobrenjePoslePoslovneProvere(
                idZahteva,
                string.IsNullOrWhiteSpace(korisnickoIme) ? "SISTEM" : korisnickoIme,
                "Odobreno nakon provere sportskog pregleda, testa sposobnosti i dokumentacije.");

            return RezultatPoslovnogPravila.Uspeh(
                "Zahtev ispunjava poslovno pravilo i odobren je.");
        }

        private static bool DaLiJeDokumentDostavljen(
            ZahtevZaUclanjenje zahtev,
            string nazivDokumenta)
        {
            return zahtev.Dokumentacija.Any(d =>
                d.Dostavljeno &&
                string.Equals(d.NazivDokumenta, nazivDokumenta, StringComparison.OrdinalIgnoreCase));
        }
    }
}
