using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;

namespace WebProjectTurist.Models
{
    public enum AranzmanTip
    {
        NocenjeSaDoruckom,
        Polupansion,
        PunPansion,
        AllInclusive,
        NajamApartmana
    }

    public enum TransportTip
    {
        Autobus,
        Avion,
        AutobusAvion,
        Individualan,
        Ostalo
    }
    public enum LokacijAranzmana
    {
        Grad, 
        Drzava,
        Regija
    }
    public class Aranzman
    {
        public string Naziv { get; set; }
        public AranzmanTip TipAranzmana { get; set; }
        public TransportTip TipPrevoza { get; set; }
        public LokacijAranzmana Lokacija { get; set; }
        public string DatumPocetka { get; set; } //format dd/MM/yyyy
        public string DatumZavrsetka { get; set; } //format dd/MM/yyyy
        public int MaksimalanBrojPutnika { get; set; }
        public string Opis { get; set; }
        public string ProgramPutovanja { get; set; }
        public string Poster { get; set; } //slika
        public List<Smestaj> Smestaji { get; set; } = new List<Smestaj>();
        public bool Obrisan { get; set; } = false;

        public Aranzman()
        {
            Smestaji = new List<Smestaj>();
           
        }
    }
}