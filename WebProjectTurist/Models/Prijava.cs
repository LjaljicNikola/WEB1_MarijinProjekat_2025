using System;

namespace WebProjectTurist.Models
{
    public enum StatusPrijave { AKTIVNA, OTKAZANA }

    public class Prijava
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UcesnikKorisnickoIme { get; set; }
        public string RadionicaId { get; set; }
        public StatusPrijave Status { get; set; } = StatusPrijave.AKTIVNA;
        public string DatumVremeKreiranja { get; set; }  // format dd/MM/yyyy HH:mm
    }
}
