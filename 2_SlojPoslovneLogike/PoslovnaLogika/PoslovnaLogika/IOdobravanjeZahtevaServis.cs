namespace PoslovnaLogika
{
    public interface IOdobravanjeZahtevaServis
    {
        RezultatPoslovnogPravila ProveriIOdobri(int idZahteva, string korisnickoIme);
    }
}
