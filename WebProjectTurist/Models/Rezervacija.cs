using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebProjectTurist.Models
{
    public enum RezervacijaStatus { Aktivna, Otkazana }
    public class Rezervacija
    {
        public string Id { get; set; }  // jedinstveni identifikator
        public string TuristaKorisnickoIme { get; set; }
        public RezervacijaStatus Status { get; set; }
        public  Aranzman AranzmanRezervacije { get; set; }
        public SmestajnaJedinica Jedinica { get; set; }

        public Rezervacija()
        {
            AranzmanRezervacije = new Aranzman();
            Jedinica = new SmestajnaJedinica();
        }
    }
}