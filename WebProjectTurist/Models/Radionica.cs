using System;

namespace WebProjectTurist.Models
{
    public enum StatusRadionice { AKTIVNA, OTKAZANA }

    public class Radionica
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string PredavacKorisnickoIme { get; set; }   // vlasnik radionice
        public string Naziv { get; set; }
        public string Opis { get; set; }
        public OblastEkspertize Kategorija { get; set; }
        public string DatumVremepocetka { get; set; }        // format dd/MM/yyyy HH:mm
        public int Trajanje { get; set; }                    // u minutima
        public string MestoOdrzavanja { get; set; }
        public int MaksimalanBrojUcesnika { get; set; }
        public int BrojSlobodnihMesta { get; set; }
        public StatusRadionice Status { get; set; } = StatusRadionice.AKTIVNA;
    }
}
