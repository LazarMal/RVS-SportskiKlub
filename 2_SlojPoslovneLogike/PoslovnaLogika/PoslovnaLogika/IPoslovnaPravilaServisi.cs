using System;

namespace PoslovnaLogika
{
    public interface IPoslovnaPravilaServisi
    {
        int DajStarosnuGranicu();

        bool DaLiJeMaloletan(DateTime datumRodjenja);

        bool DaLiSuPotrebniPodaciRoditelja(DateTime datumRodjenja);
    }
}
