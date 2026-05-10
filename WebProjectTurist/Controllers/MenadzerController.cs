using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebProjectTurist.Models;

namespace WebProjectTurist.Controllers
{
    public class MenadzerController : Controller
    {
        // --- Helper za parsiranje datuma iz formata "dd/MM/yyyy" ---
        private DateTime? ParseModelDate(string dateStr)
        {
            if (string.IsNullOrEmpty(dateStr))
                return null;

            if (DateTime.TryParseExact(dateStr, "dd/MM/yyyy", CultureInfo.InvariantCulture,
                                       DateTimeStyles.None, out DateTime d))
                return d;

            return null;
        }

        # region ARANZMANI
        
        public ActionResult Aranzmani(string pretraga, string sort)
        {
            var user = (Korisnik)Session["user"];
            if (user == null || user.Uloga != UlogaKorisnika.Menadzer)
                return RedirectToAction("Login", "Authentication");

            var aranzmani = ((List<Aranzman>)HttpContext.Application["aranzmani"])
                .Where(a => !a.Obrisan)
                .ToList();

            // Pretraga
            if (!string.IsNullOrEmpty(pretraga))
            {
                aranzmani = aranzmani.Where(a =>
                    a.Naziv.ToLower().Contains(pretraga.ToLower()) ||
                    a.Lokacija.ToString().ToLower().Contains(pretraga.ToLower()))
                    .ToList();
            }

            // Sortiranje
            switch (sort)
            {
                case "nazivAsc":
                    aranzmani = aranzmani.OrderBy(a => a.Naziv).ToList();
                    break;
                case "nazivDesc":
                    aranzmani = aranzmani.OrderByDescending(a => a.Naziv).ToList();
                    break;
            }

            ViewBag.MojiAranzmani = aranzmani
                .Where(a => user.KreiraniAranzmani.Any(k => k.Naziv == a.Naziv))
                .ToList();

            return View(aranzmani);
        }

        public ActionResult NoviAranzman() => View();

        [HttpPost]
        public ActionResult SacuvajNoviAranzman(Aranzman model, HttpPostedFileBase PosterFile)
        {
            var user = (Korisnik)Session["user"];
            var aranzmani = (List<Aranzman>)HttpContext.Application["aranzmani"];
            var korisnici = (List<Korisnik>)HttpContext.Application["korisnici"];

            if (aranzmani.Any(a => a.Naziv == model.Naziv))
            {
                ViewBag.Message = "Aranzman sa tim nazivom vec postoji.";
                return View("NoviAranzman", model);
            }

            // --- Obrada slike (upload) ---
            if (PosterFile != null && PosterFile.ContentLength > 0)
            {
                string imagesPath = Server.MapPath("~/Content/Images");

                if (!Directory.Exists(imagesPath))
                    Directory.CreateDirectory(imagesPath);

                string fileName = Path.GetFileName(PosterFile.FileName);
                string uniqueName = $"{Guid.NewGuid()}_{fileName}";
                string fullPath = Path.Combine(imagesPath, uniqueName);

                PosterFile.SaveAs(fullPath);
                model.Poster = "../../Content/Images/" + uniqueName;
            }

            model.Obrisan = false;

            
            var menadzerUKorisnicima = korisnici.FirstOrDefault(k => k.KorisnickoIme == user.KorisnickoIme);

            if (menadzerUKorisnicima != null)
            {
                // Osiguraj da lista postoji
                if (menadzerUKorisnicima.KreiraniAranzmani == null)
                    menadzerUKorisnicima.KreiraniAranzmani = new List<Aranzman>();

                menadzerUKorisnicima.KreiraniAranzmani.Add(model);
            }

            model.DatumPocetka = DateTime
                .Parse(model.DatumPocetka, CultureInfo.InvariantCulture)
                .ToString("dd/MM/yyyy");

            model.DatumZavrsetka = DateTime
                .Parse(model.DatumZavrsetka, CultureInfo.InvariantCulture)
                .ToString("dd/MM/yyyy");
            aranzmani.Add(model);

            HttpContext.Application["aranzmani"] = aranzmani;
            HttpContext.Application["korisnici"] = korisnici;

            MvcApplication.StoreData("~/App_Data/arrangements.json", aranzmani);
            MvcApplication.StoreData("~/App_Data/users.json", korisnici);

            Session["user"] = menadzerUKorisnicima;

            TempData["Message"] = "Aranžman uspešno kreiran!";

            MvcApplication.AzurirajISacuvajSvePodatke();
            return RedirectToAction("Aranzmani");
        }


        public ActionResult IzmeniAranzman(string naziv)
        {
            var aranzmani = (List<Aranzman>)HttpContext.Application["aranzmani"];
            var aranzman = aranzmani.FirstOrDefault(a => a.Naziv == naziv);
            if (aranzman == null)
                return HttpNotFound();

            return View(aranzman);
        }

        [HttpPost]
        public ActionResult SacuvajIzmenjenAranzman(Aranzman model, HttpPostedFileBase PosterFile)
        {
            var aranzmani = (List<Aranzman>)HttpContext.Application["aranzmani"];
            var aranzman = aranzmani.FirstOrDefault(a => a.Naziv == model.Naziv);

            if (aranzman == null)
                return HttpNotFound();

            // --- Obrada slike (upload) ako je uploadovana nova ---
            if (PosterFile != null && PosterFile.ContentLength > 0)
            {
                string imagesPath = Server.MapPath("~/Content/Images");

                if (!Directory.Exists(imagesPath))
                    Directory.CreateDirectory(imagesPath);

                string fileName = Path.GetFileName(PosterFile.FileName);
                string uniqueName = $"{Guid.NewGuid()}_{fileName}";
                string fullPath = Path.Combine(imagesPath, uniqueName);

                PosterFile.SaveAs(fullPath);
                model.Poster = "../../Content/Images/" + uniqueName;
            }
            else
            {
                // Zadrži postojeći poster ako nije uploadovana nova slika
                model.Poster = aranzman.Poster;
            }

            // --- Ažuriranje samo unetih polja ---
            

            // Tip aranžmana i prevoza
            if (model.TipAranzmana != 0)
                aranzman.TipAranzmana = model.TipAranzmana;

            if (model.TipPrevoza != 0)
                aranzman.TipPrevoza = model.TipPrevoza;

            // Lokacija
            aranzman.Lokacija = model.Lokacija;

            // Opis
            if (!string.IsNullOrWhiteSpace(model.Opis))
                aranzman.Opis = model.Opis;

            // Program putovanja
            if (!string.IsNullOrWhiteSpace(model.ProgramPutovanja))
                aranzman.ProgramPutovanja = model.ProgramPutovanja;

            // Maksimalan broj putnika (ako je > 0)
            if (model.MaksimalanBrojPutnika > 0)
                aranzman.MaksimalanBrojPutnika = model.MaksimalanBrojPutnika;

            model.DatumPocetka = DateTime.Parse(model.DatumPocetka, CultureInfo.InvariantCulture).ToString("dd/MM/yyyy");
            model.DatumZavrsetka = DateTime.Parse(model.DatumZavrsetka, CultureInfo.InvariantCulture).ToString("dd/MM/yyyy");
            // Datumi (samo ako su uneti novi)
            if (!string.IsNullOrWhiteSpace(model.DatumPocetka))
                aranzman.DatumPocetka = model.DatumPocetka;

            if (!string.IsNullOrWhiteSpace(model.DatumZavrsetka))
                aranzman.DatumZavrsetka = model.DatumZavrsetka;

            // Poster (uvek zadrži već odlučenu vrednost)
            aranzman.Poster = model.Poster;

            // --- Snimanje promena ---
            MvcApplication.StoreData("~/App_Data/arrangements.json", aranzmani);

            TempData["Message"] = "Aranžman uspešno izmenjen!";

            MvcApplication.AzurirajISacuvajSvePodatke();
            return RedirectToAction("Aranzmani");
        }

        [HttpPost]
        public ActionResult ObrisiAranzman(string naziv)
        {
            var aranzmani = (List<Aranzman>)HttpContext.Application["aranzmani"];
            var rezervacije = (List<Rezervacija>)HttpContext.Application["rezervacije"];

            var aranzman = aranzmani.FirstOrDefault(a => a.Naziv == naziv);
            if (aranzman == null)
                return HttpNotFound();

            bool imaRezervacija = rezervacije.Any(r => r.AranzmanRezervacije.Naziv == naziv);
            if (imaRezervacija)
            {
                TempData["Message"] = "Nije moguće obrisati aranžman jer postoje rezervacije za njega.";
                return RedirectToAction("Aranzmani");
            }

            aranzman.Obrisan = true;

            MvcApplication.StoreData("~/App_Data/arrangements.json", aranzmani);
            TempData["Message"] = "Aranžman je uspešno obrisan (logički).";

            MvcApplication.AzurirajISacuvajSvePodatke();
            return RedirectToAction("Aranzmani");
        }
        #endregion

        #region SMESTAJI
        public ActionResult Smestaji(string nazivAranzmana)
        {
            var aranzmani = (List<Aranzman>)HttpContext.Application["aranzmani"];
            var aranzman = aranzmani.FirstOrDefault(a => a.Naziv == nazivAranzmana);
            if (aranzman == null)
                return HttpNotFound();

            return View(aranzman.Smestaji.Where(s => !s.Obrisan).ToList());
        }

        public ActionResult NoviSmestaj(string nazivAranzmana)
        {
            ViewBag.NazivAranzmana = nazivAranzmana;
            return View();
        }

        [HttpPost]
        public ActionResult SacuvajNoviSmestaj(Smestaj model, string nazivAranzmana)
        {
            var aranzmani = (List<Aranzman>)HttpContext.Application["aranzmani"];
            var korisnici = (List<Korisnik>)HttpContext.Application["korisnici"];
            var aranzman = aranzmani.FirstOrDefault(a => a.Naziv == nazivAranzmana);

            if (aranzman == null)
                return HttpNotFound();

            
            if (model.Tip == SmestajTip.Vila || model.Tip == SmestajTip.Motel)
            {
                model.BrojZvezdica = 0;
            }

            aranzman.Smestaji.Add(model);

            // Osveži i kod menadžera
            foreach (var korisnik in korisnici.Where(k => k.Uloga == UlogaKorisnika.Menadzer))
            {
                var mojAranzman = korisnik.KreiraniAranzmani.FirstOrDefault(a => a.Naziv == nazivAranzmana);
                if (mojAranzman != null)
                    mojAranzman.Smestaji = aranzman.Smestaji;
            }

            MvcApplication.StoreData("~/App_Data/arrangements.json", aranzmani);
            MvcApplication.StoreData("~/App_Data/users.json", korisnici);

            TempData["Message"] = "Smeštaj uspešno dodat.";

            MvcApplication.AzurirajISacuvajSvePodatke();
            return RedirectToAction("Smestaji", new { nazivAranzmana });
        }


        public ActionResult IzmeniSmestaj(string nazivSmestaja, string nazivAranzmana)
        {
            var aranzmani = (List<Aranzman>)HttpContext.Application["aranzmani"];
            var smestaj = aranzmani
                .SelectMany(a => a.Smestaji)
                .FirstOrDefault(s => s.Naziv == nazivSmestaja && !s.Obrisan);

            if (smestaj == null)
                return HttpNotFound();

            ViewBag.NazivAranzmana = nazivAranzmana;

            return View(smestaj);
        }

        [HttpPost]
        public ActionResult SacuvajIzmeneSmestaja(Smestaj model)
        {
            var aranzmani = (List<Aranzman>)HttpContext.Application["aranzmani"];
            var korisnici = (List<Korisnik>)HttpContext.Application["korisnici"];

            var smestaj = aranzmani
                .SelectMany(a => a.Smestaji)
                .FirstOrDefault(s => s.Naziv == model.Naziv);

            if (smestaj == null)
                return HttpNotFound();

            if (model.Tip == SmestajTip.Vila || model.Tip == SmestajTip.Motel)
            {
                model.BrojZvezdica = 0;
            }
            else
            {
                smestaj.BrojZvezdica = model.BrojZvezdica; // samo ako nije vila/motel
            }

            // --- Ažuriranje ostalih informacija ---
            smestaj.Tip = model.Tip;
            smestaj.Bazen = model.Bazen;
            smestaj.Spa = model.Spa;
            smestaj.Invaliditet = model.Invaliditet;
            smestaj.Wifi = model.Wifi;

            // --- Sinhronizuj kod menadžera ---
            foreach (var korisnik in korisnici.Where(k => k.Uloga == UlogaKorisnika.Menadzer))
            {
                foreach (var a in korisnik.KreiraniAranzmani)
                {
                    var s = a.Smestaji.FirstOrDefault(x => x.Naziv == model.Naziv);
                    if (s != null)
                    {
                        s.Tip = smestaj.Tip;
                        s.BrojZvezdica = smestaj.BrojZvezdica;
                        s.Bazen = smestaj.Bazen;
                        s.Spa = smestaj.Spa;
                        s.Invaliditet = smestaj.Invaliditet;
                        s.Wifi = smestaj.Wifi;
                    }
                }
            }

            MvcApplication.StoreData("~/App_Data/arrangements.json", aranzmani);
            MvcApplication.StoreData("~/App_Data/users.json", korisnici);

            TempData["Message"] = "Smeštaj uspešno izmenjen.";

            var aranzman = aranzmani.FirstOrDefault(a => a.Smestaji.Any(s => s.Naziv == model.Naziv));

            MvcApplication.AzurirajISacuvajSvePodatke();
            return RedirectToAction("Smestaji", new { nazivAranzmana = aranzman?.Naziv });
        }

        [HttpPost]
        public ActionResult ObrisiSmestaj(string nazivSmestaja)
        {
            var aranzmani = (List<Aranzman>)HttpContext.Application["aranzmani"];
            var danas = DateTime.Now;

            foreach (var a in aranzmani)
            {
                var smestaj = a.Smestaji.FirstOrDefault(s => s.Naziv == nazivSmestaja);
                if (smestaj != null)
                {
                    // --- Provera da li je aranžman sa ovim smeštajem u budućnosti ---
                    if (DateTime.TryParseExact(a.DatumPocetka, "dd/MM/yyyy", CultureInfo.InvariantCulture,
                                               DateTimeStyles.None, out DateTime datumPocetka))
                    {
                        if (datumPocetka > danas)
                        {
                            TempData["Message"] = "Nije dozvoljeno obrisati smeštaj jer postoji budući aranžman koji ga koristi.";
                            return RedirectToAction("Smestaji", new { nazivAranzmana = a.Naziv });
                        }
                    }

                    // Logičko brisanje
                    smestaj.Obrisan = true;

                    MvcApplication.StoreData("~/App_Data/arrangements.json", aranzmani);
                    TempData["Message"] = "Smeštaj je obrisan (logički).";

                    MvcApplication.AzurirajISacuvajSvePodatke();
                    return RedirectToAction("Smestaji", new { nazivAranzmana = a.Naziv });
                }
            }

            return HttpNotFound();
        }
        #endregion

        #region SMESTAJNE JEDINICE
        public ActionResult SmestajneJedinice(string nazivSmestaja, string nazivAranzmana)
        {
            var aranzmani = (List<Aranzman>)HttpContext.Application["aranzmani"];
            var smestaj = aranzmani.SelectMany(a => a.Smestaji).FirstOrDefault(s => s.Naziv == nazivSmestaja);
            if (smestaj == null)
                return HttpNotFound();

            ViewBag.NazivAranzmana = nazivAranzmana;
            ViewBag.NazivSmestaja = nazivSmestaja;

            return View(smestaj.SmestajneJedinice.Where(j => !j.Obrisana).ToList());
        }

        public ActionResult NovaJedinica(string nazivSmestaja)
        {
            ViewBag.NazivSmestaja = nazivSmestaja;
            return View();
        }

        [HttpPost]
        public ActionResult SacuvajJedinicu(SmestajnaJedinica model, string nazivSmestaja)
        {
            var aranzmani = (List<Aranzman>)HttpContext.Application["aranzmani"];
            var korisnici = (List<Korisnik>)HttpContext.Application["korisnici"];

            var smestaj = aranzmani.SelectMany(a => a.Smestaji).FirstOrDefault(s => s.Naziv == nazivSmestaja);
            if (smestaj == null)
                return HttpNotFound();

            smestaj.SmestajneJedinice.Add(model);

            foreach (var korisnik in korisnici.Where(k => k.Uloga == UlogaKorisnika.Menadzer))
            {
                foreach (var a in korisnik.KreiraniAranzmani)
                {
                    var s = a.Smestaji.FirstOrDefault(x => x.Naziv == nazivSmestaja);
                    if (s != null)
                        s.SmestajneJedinice = smestaj.SmestajneJedinice;
                }
            }

            MvcApplication.StoreData("~/App_Data/arrangements.json", aranzmani);
            MvcApplication.StoreData("~/App_Data/users.json", korisnici);

            TempData["Message"] = "Smeštajna jedinica uspešno dodata.";
            // Pronađi aranzman koji sadrži taj smeštaj
            var pronadjeniAranzman = aranzmani.FirstOrDefault(a => a.Smestaji.Any(s => s.Naziv == nazivSmestaja));
            if (pronadjeniAranzman == null)
                return HttpNotFound();

            MvcApplication.AzurirajISacuvajSvePodatke();
            return RedirectToAction("SmestajneJedinice", new { nazivSmestaja, nazivAranzmana = pronadjeniAranzman.Naziv });

            
        }

        public ActionResult IzmeniJedinicu(string nazivSmestaja, int index)
        {
            var aranzmani = (List<Aranzman>)HttpContext.Application["aranzmani"];
            var smestaj = aranzmani.SelectMany(a => a.Smestaji)
                                   .FirstOrDefault(s => s.Naziv == nazivSmestaja);

            if (smestaj == null || index < 0 || index >= smestaj.SmestajneJedinice.Count)
                return HttpNotFound();

            ViewBag.NazivSmestaja = nazivSmestaja;
            ViewBag.Index = index;

            var jedinica = smestaj.SmestajneJedinice[index];
            return View(jedinica);
        }

        [HttpPost]
        public ActionResult SacuvajIzmeneJedinice(string nazivSmestaja, int index, SmestajnaJedinica model)
        {
            var aranzmani = (List<Aranzman>)HttpContext.Application["aranzmani"];
            var rezervacije = (List<Rezervacija>)HttpContext.Application["rezervacije"];
            var danas = DateTime.Now;

            var smestaj = aranzmani.SelectMany(a => a.Smestaji)
                                   .FirstOrDefault(s => s.Naziv == nazivSmestaja);

            if (smestaj == null || index < 0 || index >= smestaj.SmestajneJedinice.Count)
                return HttpNotFound();

            var jedinica = smestaj.SmestajneJedinice[index];

            // --- Provera aktivne buduće rezervacije ---
            bool imaAktivnih = rezervacije.Any(r =>
                r.Jedinica != null &&
                r.Jedinica == jedinica &&
                DateTime.TryParseExact(r.AranzmanRezervacije.DatumPocetka, "dd/MM/yyyy",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime pocetak) &&
                pocetak > danas);

            var aranzman = aranzmani.FirstOrDefault(a => a.Smestaji.Contains(smestaj));
            string nazivAranzmana = aranzman != null ? aranzman.Naziv : null;
            if (imaAktivnih && model.DozvoljenBrojGostiju != jedinica.DozvoljenBrojGostiju)
            {
                TempData["Message"] = "Nije dozvoljeno menjati broj kreveta jer postoji aktivna buduća rezervacija!";
                

                return RedirectToAction("SmestajneJedinice", new { nazivSmestaja, nazivAranzmana });
            }

            // --- Ažuriranje podataka ---
            jedinica.DozvoljenBrojGostiju = model.DozvoljenBrojGostiju;
            jedinica.DozvoljeniLjubimci = model.DozvoljeniLjubimci;
            jedinica.Cena = model.Cena;
            jedinica.Slobodna = model.Slobodna;

            MvcApplication.StoreData("~/App_Data/arrangements.json", aranzmani);
            TempData["Message"] = "Smeštajna jedinica uspešno izmenjena.";

            
            MvcApplication.AzurirajISacuvajSvePodatke();
            return RedirectToAction("SmestajneJedinice", new { nazivSmestaja, nazivAranzmana });
        }

        [HttpPost]
        public ActionResult ObrisiJedinicu(string nazivSmestaja, int index)
        {
            var aranzmani = (List<Aranzman>)HttpContext.Application["aranzmani"];
            var rezervacije = (List<Rezervacija>)HttpContext.Application["rezervacije"];
            var danas = DateTime.Now;

            var smestaj = aranzmani.SelectMany(a => a.Smestaji)
                                   .FirstOrDefault(s => s.Naziv == nazivSmestaja);
            if (smestaj == null || index < 0 || index >= smestaj.SmestajneJedinice.Count)
                return HttpNotFound();

            var jedinica = smestaj.SmestajneJedinice[index];

            bool imaAktivnih = rezervacije.Any(r =>
                r.Jedinica == jedinica &&
                DateTime.TryParseExact(r.AranzmanRezervacije.DatumPocetka, "dd/MM/yyyy",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime pocetak) &&
                pocetak > danas);

            if (imaAktivnih)
            {
                TempData["Message"] = "Nije dozvoljeno obrisati jedinicu jer postoji aktivna buduća rezervacija.";
                return RedirectToAction("SmestajneJedinice", new { nazivSmestaja });
            }

            jedinica.Obrisana = true;

            MvcApplication.StoreData("~/App_Data/arrangements.json", aranzmani);
            TempData["Message"] = "Smeštajna jedinica obrisana (logički).";

            MvcApplication.AzurirajISacuvajSvePodatke();
            return RedirectToAction("SmestajneJedinice", new { nazivSmestaja });
        }
        #endregion

        #region REZERVACIJE
        public ActionResult Rezervacije()
        {
            var user = (Korisnik)Session["user"];
            var rezervacije = (List<Rezervacija>)HttpContext.Application["rezervacije"];

            var menadzeroveRezervacije = rezervacije
                .Where(r => user.KreiraniAranzmani.Any(a => a.Naziv == r.AranzmanRezervacije.Naziv))
                .ToList();

            return View(menadzeroveRezervacije);
        }

        
        public ActionResult DetaljiRezervacije(string id)
        {
            var rezervacije = (List<Rezervacija>)HttpContext.Application["rezervacije"];
            var rezervacija = rezervacije.FirstOrDefault(r => r.Id == id);

            if (rezervacija == null)
            {
                return HttpNotFound("Rezervacija nije pronađena.");
            }

            return View(rezervacija);
        }
        #endregion

        #region KOMENTARI
        public ActionResult Komentari()
        {
            var user = (Korisnik)Session["user"];
            var komentari = (List<Komentar>)HttpContext.Application["komentari"];
            var aranzmani = (List<Aranzman>)HttpContext.Application["aranzmani"];

            if (komentari == null)
            {
                komentari = new List<Komentar>();
                return View(komentari);
            }

            var menadzeroviSmestaji = user.KreiraniAranzmani
                .SelectMany(a => a.Smestaji)
                .Select(s => s.Naziv)
                .ToList();

            var mojiKomentari = komentari
                .Where(k => k.SmestajKomentar != null && menadzeroviSmestaji.Contains(k.SmestajKomentar.Naziv))
                .ToList();

            return View(mojiKomentari);
        }

        [HttpPost]
        public ActionResult OdobriKomentar(int index)
        {
            var komentari = (List<Komentar>)HttpContext.Application["komentari"];
            if (index >= 0 && index < komentari.Count)
                komentari[index].Odobren = true;

            MvcApplication.StoreData("~/App_Data/comments.json", komentari);
            TempData["Message"] = "Komentar je odobren.";

            MvcApplication.AzurirajISacuvajSvePodatke();
            return RedirectToAction("Komentari");
        }

        [HttpPost]
        public ActionResult OdbijKomentar(int index)
        {
            var komentari = (List<Komentar>)HttpContext.Application["komentari"];
            if (index >= 0 && index < komentari.Count)
                komentari[index].Odobren = false;

            MvcApplication.StoreData("~/App_Data/comments.json", komentari);
            TempData["Message"] = "Komentar je odbijen.";

            MvcApplication.AzurirajISacuvajSvePodatke();
            return RedirectToAction("Komentari");
        }

        #endregion
    }
}
