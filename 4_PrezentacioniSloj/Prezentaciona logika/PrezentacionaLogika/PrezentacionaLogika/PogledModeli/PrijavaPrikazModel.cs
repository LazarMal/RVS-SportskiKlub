using System.ComponentModel.DataAnnotations;

namespace PrezentacionaLogika.PogledModeli
{
    public class PrijavaPrikazModel
    {
        [Required]
        public string KorisnickoIme { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Sifra { get; set; }
    }
}