using System;

namespace WebProjectTurist.Models
{
    public class Administrator
    {
        public string KorisnickoIme { get; set; }
        public string Lozinka { get; set; }
        public string Ime { get; set; }
        public string Prezime { get; set; }
        public string DatumRodjenja { get; set; }   // format dd/MM/yyyy
    }
}
