using System.Collections.Generic;
using KlasePodataka;

namespace DBUtils.Repozitorijumi
{
    public interface ISportskaDisciplinaRepozitorijum
    {
        List<SportskaDisciplina> DajAktivne();

        SportskaDisciplina DajPoId(int idSportskeDiscipline);
    }
}

