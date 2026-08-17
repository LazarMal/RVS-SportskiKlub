using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KlasePodataka
{
    [Table("RoditeljStaratelj")]
    public class RoditeljStaratelj
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IDRoditeljaStaratelja { get; set; }

        [Index("UQ_RoditeljStaratelj_Zahtev", IsUnique = true)]
        public int IDZahteva { get; set; }

        [Required]
        [StringLength(100)]
        public string ImePrezime { get; set; }

        [Required]
        [StringLength(13, MinimumLength = 13)]
        [Column(TypeName = "char")]
        public string JMBG { get; set; }

        [Required]
        [StringLength(40)]
        public string Srodstvo { get; set; }

        [Required]
        [StringLength(20)]
        public string KontaktTelefon { get; set; }

        [StringLength(100)]
        public string Email { get; set; }

        [ForeignKey("IDZahteva")]
        public virtual ZahtevZaUclanjenje ZahtevZaUclanjenje { get; set; }
    }
}
