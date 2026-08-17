using KlasePodataka;

namespace DBUtils.Repozitorijumi
{
    public interface IKandidatRepozitorijum
    {
        Kandidat DajPoJMBG(string jmbg);

        void Sacuvaj(Kandidat kandidat);
    }
}

