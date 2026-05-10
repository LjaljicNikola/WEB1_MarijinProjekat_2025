using System;
using System.Collections.Generic;

namespace WebProjectTurist.Models
{
    public enum NivoObrazovanja { OSNOVNO, SREDNJE, VISOKO }

    public class Ucesnik
    {
        public string KorisnickoIme { get; set; }   // jedinstveno
        public string Lozinka { get; set; }
        public string JMBG { get; set; }            // jedinstveno, tacno 13 numeričkih karaktera
        public string Ime { get; set; }
        public string Prezime { get; set; }
        public string DatumRodjenja { get; set; }   // format dd/MM/yyyy
        public string Email { get; set; }
        public NivoObrazovanja NivoObrazovanja { get; set; }
        public List<Prijava> Prijave { get; set; } = new List<Prijava>();

        public Ucesnik()
        {
            Prijave = new List<Prijava>();
        }
    }
}
