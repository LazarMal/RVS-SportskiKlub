using System;

namespace PoslovnaLogika
{
    public class PoslovnaPravilaServisi : IPoslovnaPravilaServisi
    {
        private readonly IParametriPoslovnihPravilaServis servisParametara;

        public PoslovnaPravilaServisi(IParametriPoslovnihPravilaServis servisParametara)
        {
            this.servisParametara = servisParametara;
        }

        public int DajStarosnuGranicu()
        {
            return servisParametara.DajParametre().StarosnaGranicaZaSaglasnost;
        }

        public bool DaLiJeMaloletan(DateTime datumRodjenja)
        {
            int starosnaGranica = DajStarosnuGranicu();
            DateTime danas = DateTime.Today;
            int godine = danas.Year - datumRodjenja.Year;

            if (datumRodjenja.Date > danas.AddYears(-godine))
            {
                godine--;
            }

            return godine < starosnaGranica;
        }

        public bool DaLiSuPotrebniPodaciRoditelja(DateTime datumRodjenja)
        {
            return DaLiJeMaloletan(datumRodjenja);
        }
    }
}
