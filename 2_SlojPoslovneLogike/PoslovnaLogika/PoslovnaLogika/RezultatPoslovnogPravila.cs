namespace PoslovnaLogika
{
    public class RezultatPoslovnogPravila
    {
        public bool Uspesno { get; private set; }

        public string Poruka { get; private set; }

        public static RezultatPoslovnogPravila Uspeh(string poruka)
        {
            return new RezultatPoslovnogPravila { Uspesno = true, Poruka = poruka };
        }

        public static RezultatPoslovnogPravila Neuspeh(string poruka)
        {
            return new RezultatPoslovnogPravila { Uspesno = false, Poruka = poruka };
        }
    }
}
