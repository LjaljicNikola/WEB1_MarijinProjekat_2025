using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebProjectTurist.Models
{
    public class Komentar
    {
        public Korisnik Turista { get; set; }
        public Smestaj SmestajKomentar{ get; set; }
        public string Tekst { get; set; }
        public int Ocena { get; set; } // 1–5
        public bool Odobren { get; set; } = false; // Vidljivost zavisi od menadzera

        public Komentar()
        {
            Turista = new Korisnik();
            SmestajKomentar = new Smestaj();
        }
    }
}