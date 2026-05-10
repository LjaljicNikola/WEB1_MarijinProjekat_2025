using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using WebProjectTurist.Models;

namespace WebProjectTurist.Controllers
{
    public class PredavacController : Controller
    {
        private bool IsPredavac()
        {
            return Session["user"] != null && (string)Session["uloga"] == "Predavac";
        }

        // Pregled sopstvenih radionice
        public ActionResult Radionice()
        {
            if (!IsPredavac())
                return RedirectToAction("Login", "Authentication");

            var predavac  = (Predavac)Session["user"];
            var radionice = (List<Radionica>)HttpContext.Application["radionice"];

            var moje = radionice
                .Where(r => r.PredavacKorisnickoIme == predavac.KorisnickoIme)
                .ToList();

            return View(moje);
        }

        // GET: Kreiranje nove radionice
        public ActionResult NovaRadionica()
        {
            if (!IsPredavac())
                return RedirectToAction("Login", "Authentication");
            return View();
        }

        // POST: Kreiranje nove radionice
        [HttpPost]
        public ActionResult NovaRadionica(string naziv, string opis, string kategorija,
                                           string datumVremePocetka, int trajanje,
                                           string mestoOdrzavanja, int maksimalanBrojUcesnika)
        {
            if (!IsPredavac())
                return RedirectToAction("Login", "Authentication");

            var predavac  = (Predavac)Session["user"];
            var radionice = (List<Radionica>)HttpContext.Application["radionice"];

            // Datum ne sme biti u proslosti
            if (!DateTime.TryParseExact(datumVremePocetka, "dd/MM/yyyy HH:mm",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime pocetak))
            {
                ViewBag.Error = "Neispravan format datuma i vremena (dd/MM/yyyy HH:mm)!";
                return View();
            }
            if (pocetak <= DateTime.Now)
            {
                ViewBag.Error = "Datum i vreme početka ne mogu biti u prošlosti!";
                return View();
            }

            if (!Enum.TryParse(kategorija, out OblastEkspertize kat))
            {
                ViewBag.Error = "Neispravna kategorija!";
                return View();
            }

            var nova = new Radionica()
            {
                Id = Guid.NewGuid().ToString(),
                PredavacKorisnickoIme = predavac.KorisnickoIme,
                Naziv = naziv,
                Opis = opis,
                Kategorija = kat,
                DatumVremepocetka = datumVremePocetka,
                Trajanje = trajanje,
                MestoOdrzavanja = mestoOdrzavanja,
                MaksimalanBrojUcesnika = maksimalanBrojUcesnika,
                BrojSlobodnihMesta = maksimalanBrojUcesnika,
                Status = StatusRadionice.AKTIVNA
            };

            radionice.Add(nova);
            HttpContext.Application["radionice"] = radionice;
            MvcApplication.StoreData("~/App_Data/radionice.json", radionice);

            TempData["Message"] = "Radionica uspešno kreirana!";
            return RedirectToAction("Radionice");
        }

        // POST: Brisanje radionice (fizicko, aktivne prijave se otkazuju)
        [HttpPost]
        public ActionResult ObrisiRadionicu(string id)
        {
            if (!IsPredavac())
                return RedirectToAction("Login", "Authentication");

            var predavac  = (Predavac)Session["user"];
            var radionice = (List<Radionica>)HttpContext.Application["radionice"];
            var prijave   = (List<Prijava>)HttpContext.Application["prijave"];

            var radionica = radionice.FirstOrDefault(r => r.Id == id && r.PredavacKorisnickoIme == predavac.KorisnickoIme);
            if (radionica == null)
                return HttpNotFound();

            // Otkazivanje aktivnih prijava
            foreach (var p in prijave.Where(p => p.RadionicaId == id && p.Status == StatusPrijave.AKTIVNA))
            {
                p.Status = StatusPrijave.OTKAZANA;
            }

            // Fizicko brisanje radionice
            radionice.Remove(radionica);

            HttpContext.Application["radionice"] = radionice;
            HttpContext.Application["prijave"] = prijave;
            MvcApplication.StoreData("~/App_Data/radionice.json", radionice);
            MvcApplication.StoreData("~/App_Data/prijave.json", prijave);

            TempData["Message"] = "Radionica je obrisana i sve aktivne prijave su otkazane.";
            return RedirectToAction("Radionice");
        }

        // Pregled aktivnih prijava za izabranu radionicu, sa filterom i sortiranjem (ocena 7)
        public ActionResult Prijave(string radionicaId, string jmbg, string ime, string prezime,
                                     string status, string sort, string sortDir)
        {
            if (!IsPredavac())
                return RedirectToAction("Login", "Authentication");

            var predavac  = (Predavac)Session["user"];
            var radionice = (List<Radionica>)HttpContext.Application["radionice"];
            var prijave   = (List<Prijava>)HttpContext.Application["prijave"];
            var ucesnici  = (List<Ucesnik>)HttpContext.Application["ucesnici"];

            var radionica = radionice.FirstOrDefault(r => r.Id == radionicaId && r.PredavacKorisnickoIme == predavac.KorisnickoIme);
            if (radionica == null)
                return HttpNotFound();

            ViewBag.Radionica = radionica;

            // Spoji prijave sa ucesnicima
            var query = prijave
                .Where(p => p.RadionicaId == radionicaId)
                .Select(p => new
                {
                    Prijava = p,
                    Ucesnik = ucesnici.FirstOrDefault(u => u.KorisnickoIme == p.UcesnikKorisnickoIme)
                })
                .Where(x => x.Ucesnik != null)
                .ToList();

            // Filtriranje (ocena 7)
            if (!string.IsNullOrEmpty(jmbg))
                query = query.Where(x => x.Ucesnik.JMBG.Contains(jmbg)).ToList();
            if (!string.IsNullOrEmpty(ime))
                query = query.Where(x => x.Ucesnik.Ime.ToLower().Contains(ime.ToLower())).ToList();
            if (!string.IsNullOrEmpty(prezime))
                query = query.Where(x => x.Ucesnik.Prezime.ToLower().Contains(prezime.ToLower())).ToList();
            if (!string.IsNullOrEmpty(status) && Enum.TryParse(status, out StatusPrijave sp))
                query = query.Where(x => x.Prijava.Status == sp).ToList();

            // Sortiranje (ocena 7)
            bool asc = sortDir != "desc";
            switch (sort)
            {
                case "jmbg":    query = asc ? query.OrderBy(x => x.Ucesnik.JMBG).ToList()    : query.OrderByDescending(x => x.Ucesnik.JMBG).ToList(); break;
                case "ime":     query = asc ? query.OrderBy(x => x.Ucesnik.Ime).ToList()     : query.OrderByDescending(x => x.Ucesnik.Ime).ToList(); break;
                case "prezime": query = asc ? query.OrderBy(x => x.Ucesnik.Prezime).ToList() : query.OrderByDescending(x => x.Ucesnik.Prezime).ToList(); break;
                case "status":  query = asc ? query.OrderBy(x => x.Prijava.Status).ToList()  : query.OrderByDescending(x => x.Prijava.Status).ToList(); break;
            }

            // Prebaci u ViewBag kao listu anonimnih objekata nisu podrzani direktno - koristimo tuple-like
            ViewBag.PrijaveDetalji = query.Select(x => new PredavacPrijavaViewModel
            {
                Prijava = x.Prijava,
                Ucesnik = x.Ucesnik
            }).ToList();

            ViewBag.Sort = sort;
            ViewBag.SortDir = sortDir;

            return View();
        }
    }

    // ViewModel za prikaz prijava u view-u predavaca
    public class PredavacPrijavaViewModel
    {
        public Prijava Prijava { get; set; }
        public Ucesnik Ucesnik { get; set; }
    }
}
