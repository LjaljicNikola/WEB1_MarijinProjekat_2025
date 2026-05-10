using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebProjectTurist.Models
{
    public enum SmestajTip{ Hotel, Motel, Vila }
    public class Smestaj
    {
        public SmestajTip Tip { get; set; }
        public string Naziv { get; set; }
        public int BrojZvezdica { get; set; } //za hotele
        public bool Bazen { get; set; }
        public bool Spa { get; set; }
        public bool Invaliditet { get; set; }
        public bool Wifi { get; set; }
        public List<SmestajnaJedinica> SmestajneJedinice { get; set; } = new List<SmestajnaJedinica>(); //minimalno jedna
        public bool Obrisan { get; set; } = false;

        public Smestaj()
        {
            SmestajneJedinice = new List<SmestajnaJedinica>();
           
        }
    }
}