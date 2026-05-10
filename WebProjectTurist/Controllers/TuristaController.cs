using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebProjectTurist.Models;

namespace WebProjectTurist.Controllers
{
    public class TuristaController : Controller
    {
        
        //  Pregled rezervacija sa pretragom i sortiranjem
        public ActionResult Rezervacije(string pretragaId, string nazivAranzmana, string status, string sort)
        {
            var user = (Korisnik)Session["user"];
            if (user == null || user.Uloga != UlogaKorisnika.Turista)
                return RedirectToAction("Login", "Authentication");

            var rezervacije = ((List<Rezervacija>)HttpContext.Application["rezervacije"])
                .Where(r => r.TuristaKorisnickoIme == user.KorisnickoIme)
                .ToList();

            // --- Pretraga ---
            if (!string.IsNullOrEmpty(pretragaId))
                rezervacije = rezervacije.Where(r => r.Id.Contains(pretragaId)).ToList();

            if (!string.IsNullOrEmpty(nazivAranzmana))
                rezervacije = rezervacije
                    .Where(r => r.AranzmanRezervacije.Naziv.ToLower().Contains(nazivAranzmana.ToLower()))
                    .ToList();

            if (!string.IsNullOrEmpty(status))
            {
                if (Enum.TryParse(status, true, out RezervacijaStatus parsed))
                    rezervacije = rezervacije.Where(r => r.Status == parsed).ToList();
            }

            // --- Sortiranje ---
            switch (sort)
            {
                case "nazivAsc":
                    rezervacije = rezervacije.OrderBy(r => r.AranzmanRezervacije.Naziv).ToList();
                    break;
                case "nazivDesc":
                    rezervacije = rezervacije.OrderByDescending(r => r.AranzmanRezervacije.Naziv).ToList();
                    break;
            }

            return View(rezervacije);
        }

        

        // Kreiranje nove rezervacije (izbor aranzmana)
        public ActionResult NovaRezervacija(string naziv)
        {
            var user = (Korisnik)Session["user"];
            if (user == null || user.Uloga != UlogaKorisnika.Turista)
                return RedirectToAction("Login", "Authentication");

            var aranzmani = (List<Aranzman>)HttpContext.Application["aranzmani"];
            var aranzman = aranzmani.FirstOrDefault(a => a.Naziv == naziv);

            if (aranzman == null)
                return HttpNotFound();

            var slobodneJedinice = aranzman.Smestaji
                .SelectMany(s => s.SmestajneJedinice)
                .Where(j => j.Slobodna && !j.Obrisana)
                .ToList();

            ViewBag.SlobodneJedinice = slobodneJedinice;
            return View(aranzman);
        }


        //  Potvrda i cuvanje nove rezervacije
        [HttpPost]
        public ActionResult NapraviRezervaciju(string nazivAranzmana, string nazivSmestaja, int indexJedinice)
        {
            var user = (Korisnik)Session["user"];
            if (user == null || user.Uloga != UlogaKorisnika.Turista)
                return RedirectToAction("Login", "Authentication");

            var aranzmani = (List<Aranzman>)HttpContext.Application["aranzmani"];
            var rezervacije = (List<Rezervacija>)HttpContext.Application["rezervacije"];

            var aranzman = aranzmani.FirstOrDefault(a => a.Naziv == nazivAranzmana);
            if (aranzman == null)
                return HttpNotFound();

            var smestaj = aranzman.Smestaji.FirstOrDefault(s => s.Naziv == nazivSmestaja);
            if (smestaj == null)
                return HttpNotFound();

            if (indexJedinice < 0 || indexJedinice >= smestaj.SmestajneJedinice.Count)
                return HttpNotFound();

            var jedinica = smestaj.SmestajneJedinice[indexJedinice];
            if (!jedinica.Slobodna)
            {
                ViewBag.Message = "Izabrana smestajna jedinica vise nije dostupna.";
                return View("NovaRezervacija", aranzman);
            }

            //  Kreiranje nove rezervacije 
            string newId = Guid.NewGuid().ToString();

            Rezervacija nova = new Rezervacija()
            {
                Id = newId,
                TuristaKorisnickoIme = user.KorisnickoIme,
                Status = RezervacijaStatus.Aktivna,
                AranzmanRezervacije = aranzman,
                Jedinica = jedinica
            };

            jedinica.Slobodna = false;
            rezervacije.Add(nova);
            user.Rezervacije.Add(nova);

            

            // --- Cuvanje podataka ---
            HttpContext.Application["rezervacije"] = rezervacije;
            HttpContext.Application["aranzmani"] = aranzmani;
            MvcApplication.StoreData("~/App_Data/reservations.json", rezervacije);
            MvcApplication.StoreData("~/App_Data/arrangements.json", aranzmani);
            MvcApplication.StoreData("~/App_Data/users.json", (List<Korisnik>)HttpContext.Application["korisnici"]);

            TempData["Message"] = "Uspesno ste izvrsili rezervaciju!";
            return RedirectToAction("Rezervacije");
        }


        //  Otkazivanje rezervacije (ako aranzman nije prosao)
        [HttpPost]
        public ActionResult Otkazi(string id)
        {
            var user = (Korisnik)Session["user"];
            if (user == null || user.Uloga != UlogaKorisnika.Turista)
                return RedirectToAction("Login", "Authentication");

            var rezervacije = (List<Rezervacija>)HttpContext.Application["rezervacije"];
            var rezervacija = rezervacije.FirstOrDefault(r => r.Id == id);

            if (rezervacija == null)
                return HttpNotFound();

            DateTime datumZavrsetka = DateTime.ParseExact(
                rezervacija.AranzmanRezervacije.DatumZavrsetka, "dd/MM/yyyy", CultureInfo.InvariantCulture);

            if (datumZavrsetka < DateTime.Now)
            {
                TempData["Message"] = "Aranzman je vec prosao, ne moze se otkazati.";
                return RedirectToAction("Rezervacije");
            }

            rezervacija.Status = RezervacijaStatus.Otkazana;
            rezervacija.Jedinica.Slobodna = true;

            HttpContext.Application["rezervacije"] = rezervacije;
            MvcApplication.StoreData("~/App_Data/reservations.json", rezervacije);
            MvcApplication.StoreData("~/App_Data/arrangements.json", (List<Aranzman>)HttpContext.Application["aranzmani"]);

            TempData["Message"] = "Rezervacija je uspesno otkazana.";
            return RedirectToAction("Rezervacije");
        }


        //  Pregled detalja rezervacije
        public ActionResult Detalji(string id)
        {
            var user = (Korisnik)Session["user"];
            if (user == null || user.Uloga != UlogaKorisnika.Turista)
                return RedirectToAction("Login", "Authentication");

            var rezervacije = (List<Rezervacija>)HttpContext.Application["rezervacije"];
            var rezervacija = rezervacije.FirstOrDefault(r => r.Id == id);

            if (rezervacija == null)
                return HttpNotFound();

            return View(rezervacija);
        }


        //  Ostavljanje komentara (nakon zavrsetka aranzmana)
        [HttpGet]
        public ActionResult OstaviKomentar(string id)
        {
            var user = (Korisnik)Session["user"];
            if (user == null || user.Uloga != UlogaKorisnika.Turista)
                return RedirectToAction("Login", "Authentication");

            var rezervacije = (List<Rezervacija>)HttpContext.Application["rezervacije"];
            var rezervacija = rezervacije.FirstOrDefault(r => r.Id == id);

            if (rezervacija == null)
                return HttpNotFound();

            DateTime datumZavrsetka = DateTime.ParseExact(
                rezervacija.AranzmanRezervacije.DatumZavrsetka, "dd/MM/yyyy", CultureInfo.InvariantCulture);

            if (datumZavrsetka > DateTime.Now)
            {
                TempData["Message"] = "Komentar možete ostaviti tek nakon završetka aranžmana.";
                return RedirectToAction("Rezervacije");
            }

            MvcApplication.AzurirajISacuvajSvePodatke(); 
            return View(rezervacija);
        }

        [HttpPost]
        public ActionResult OstaviKomentar(string id, string tekst, int ocena)
        {
            var user = (Korisnik)Session["user"];
            if (user == null || user.Uloga != UlogaKorisnika.Turista)
                return RedirectToAction("Login", "Authentication");

            var rezervacije = (List<Rezervacija>)HttpContext.Application["rezervacije"];
            var komentari = (List<Komentar>)HttpContext.Application["komentari"];
            var rezervacija = rezervacije.FirstOrDefault(r => r.Id == id);

            if (rezervacija == null)
                return HttpNotFound();

            // Proverite da li komentari lista postoji
            if (komentari == null)
            {
                komentari = new List<Komentar>();
                HttpContext.Application["komentari"] = komentari;
            }

            // Pronađite smeštaj koji sadrži ovu jedinicu
            Smestaj smestajZaKomentar = null;
            foreach (var smestaj in rezervacija.AranzmanRezervacije.Smestaji)
            {
                // Uporedite po karakteristikama umesto Contains()
                var jedinicaUSmestaju = smestaj.SmestajneJedinice.FirstOrDefault(j =>
                    j.DozvoljenBrojGostiju == rezervacija.Jedinica.DozvoljenBrojGostiju &&
                    j.DozvoljeniLjubimci == rezervacija.Jedinica.DozvoljeniLjubimci &&
                    j.Cena == rezervacija.Jedinica.Cena);

                if (jedinicaUSmestaju != null)
                {
                    smestajZaKomentar = smestaj;
                    break;
                }
            }

            Komentar novi = new Komentar
            {
                Turista = user,
                SmestajKomentar = smestajZaKomentar,
                Tekst = tekst,
                Ocena = ocena,
                Odobren = false
            };

            komentari.Add(novi);
            HttpContext.Application["komentari"] = komentari;
            MvcApplication.StoreData("~/App_Data/comments.json", komentari);

            TempData["Message"] = "Komentar je poslat menadžeru na odobrenje.";

            MvcApplication.AzurirajISacuvajSvePodatke();
            return RedirectToAction("Rezervacije");
        }
    }
}
