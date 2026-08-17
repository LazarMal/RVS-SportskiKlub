using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KlasePodataka
{
    [Table("Dokumentacija")]
    public class Dokumentacija
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IDDokumentacije { get; set; }

        [Index("UQ_Dokumentacija_Zahtev_Naziv", 1, IsUnique = true)]
        public int IDZahteva { get; set; }

        [Required]
        [StringLength(100)]
        [Index("UQ_Dokumentacija_Zahtev_Naziv", 2, IsUnique = true)]
        public string NazivDokumenta { get; set; }

        public bool Dostavljeno { get; set; }

        [ForeignKey("IDZahteva")]
        public virtual ZahtevZaUclanjenje ZahtevZaUclanjenje { get; set; }
    }
}
