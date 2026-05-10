using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebProjectTurist.Models
{
    public enum UlogaKorisnika { Administrator, Menadzer, Turista }
    public class Korisnik
    {
        public string KorisnickoIme { get; set; } //Jedinstveno
        public string Lozinka { get; set; }
        public string Ime { get; set; }
        public string Prezime { get; set; }
        public string Pol { get; set; }
        public string Email { get; set; }
        public string DatumRodjenja { get; set; } //format dd/MM/yyyy
        public  UlogaKorisnika Uloga  { get; set; }
        public List<Rezervacija> Rezervacije { get; set; } = new List<Rezervacija>(); //Turista
        public List<Aranzman> KreiraniAranzmani { get; set; } = new List<Aranzman>(); //Menadzer
        public bool Obrisan { get; set; } = false;

        public Korisnik()
        {
            Rezervacije = new List<Rezervacija>();
            KreiraniAranzmani = new List<Aranzman>();
            
        }
    }
}