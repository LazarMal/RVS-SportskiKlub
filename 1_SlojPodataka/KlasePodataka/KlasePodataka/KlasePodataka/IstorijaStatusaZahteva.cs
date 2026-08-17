using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KlasePodataka
{
    [Table("IstorijaStatusaZahteva")]
    public class IstorijaStatusaZahteva
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IDIstorije { get; set; }

        public int IDZahteva { get; set; }

        [StringLength(20)]
        public string StariStatus { get; set; }

        [Required]
        [StringLength(20)]
        public string NoviStatus { get; set; }

        public DateTime DatumPromene { get; set; }

        [Required]
        [StringLength(50)]
        public string KorisnickoIme { get; set; }

        [StringLength(250)]
        public string Napomena { get; set; }

        [ForeignKey("IDZahteva")]
        public virtual ZahtevZaUclanjenje ZahtevZaUclanjenje { get; set; }
    }
}
