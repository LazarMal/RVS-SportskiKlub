using System.Data.Entity;

namespace KlasePodataka
{
    public class SportskiKlubKontekst : DbContext
    {
        static SportskiKlubKontekst()
        {
            Database.SetInitializer<SportskiKlubKontekst>(null);
        }

        public SportskiKlubKontekst()
            : base("SportskiKlubKonekcija")
        {
        }

        public DbSet<Kandidat> Kandidati { get; set; }

        public DbSet<SportskaDisciplina> SportskeDiscipline { get; set; }

        public DbSet<ZahtevZaUclanjenje> ZahteviZaUclanjenje { get; set; }

        public DbSet<Dokumentacija> Dokumentacija { get; set; }

        public DbSet<RoditeljStaratelj> RoditeljiStaratelji { get; set; }

        public DbSet<IstorijaStatusaZahteva> IstorijaStatusaZahteva { get; set; }

        public DbSet<Korisnik> Korisnici { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<IstorijaStatusaZahteva>()
                .Property(i => i.DatumPromene)
                .HasPrecision(0);

            modelBuilder.Entity<Kandidat>()
                .HasMany(k => k.ZahteviZaUclanjenje)
                .WithRequired(z => z.Kandidat)
                .HasForeignKey(z => z.JMBGKandidata)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<SportskaDisciplina>()
                .HasMany(d => d.ZahteviZaUclanjenje)
                .WithRequired(z => z.SportskaDisciplina)
                .HasForeignKey(z => z.IDSportskeDiscipline)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<ZahtevZaUclanjenje>()
                .HasMany(z => z.Dokumentacija)
                .WithRequired(d => d.ZahtevZaUclanjenje)
                .HasForeignKey(d => d.IDZahteva)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<ZahtevZaUclanjenje>()
                .HasMany(z => z.RoditeljiStaratelji)
                .WithRequired(r => r.ZahtevZaUclanjenje)
                .HasForeignKey(r => r.IDZahteva)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<ZahtevZaUclanjenje>()
                .HasMany(z => z.IstorijaStatusa)
                .WithRequired(i => i.ZahtevZaUclanjenje)
                .HasForeignKey(i => i.IDZahteva)
                .WillCascadeOnDelete(true);

            base.OnModelCreating(modelBuilder);
        }
    }
}
