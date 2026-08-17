using System;

namespace DBUtils
{
    public abstract class OsnovnaTehnoloskaKlasa
    {
        public DateTime DatumKreiranja { get; set; }

        public string KreiraoKorisnik { get; set; }

        public string StatusObrade { get; set; }

        protected OsnovnaTehnoloskaKlasa()
        {
            DatumKreiranja = DateTime.Now;
            KreiraoKorisnik = "Sistem";
            StatusObrade = "Aktivan";
        }

        public virtual string DajOpisObrade()
        {
            return "Osnovna tehnološka obrada podataka u sistemu.";
        }

        protected void EvidentirajObradu(string status)
        {
            StatusObrade = status;
        }
    }
}