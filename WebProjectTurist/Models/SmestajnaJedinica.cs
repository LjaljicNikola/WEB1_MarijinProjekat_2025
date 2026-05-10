using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebProjectTurist.Models
{
    public class SmestajnaJedinica
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public int DozvoljenBrojGostiju { get; set; }
        public bool DozvoljeniLjubimci { get; set; }
        public double Cena { get; set; }
        public bool Slobodna { get; set; } = true;
        public bool Obrisana { get; set; } = false;
    }
}