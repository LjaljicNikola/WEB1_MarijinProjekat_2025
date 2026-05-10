using System;

namespace WebProjectTurist.Models
{
    public enum StatusNaloga { AKTIVAN, BLOKIRAN }
    public enum OblastEkspertize { PROGRAMIRANJE, DIZAJN, UMETNOST, MUZIKA, SPORT, NAUKA, OSTALO }

    public class Predavac
    {
        public string KorisnickoIme { get; set; }   // jedinstveno
        public string Lozinka { get; set; }
        public string JMBG { get; set; }            // jedinstveno, tacno 13 numeričkih karaktera
        public string Ime { get; set; }
        public string Prezime { get; set; }
        public string DatumRodjenja { get; set; }   // format dd/MM/yyyy
        public string Email { get; set; }
        public OblastEkspertize OblastEkspertize { get; set; }
        public StatusNaloga StatusNaloga { get; set; } = StatusNaloga.AKTIVAN;
    }
}
