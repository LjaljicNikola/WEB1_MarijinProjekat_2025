using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using WebProjectTurist.Models;

namespace WebProjectTurist.Controllers
{
    public class UcesnikController : Controller
    {
        private bool IsUcesnik()
        {
            return Session["user"] != null && (string)Session["uloga"] == "Ucesnik";
        }

        // Pregled sopstvenih prijava, sa filtrom i sortiranjem (ocena 7)
        public ActionResult Prijave(string naziv, string kategorija, string datumPocetka,
                                     string sort, string sortDir)
        {
            if (!IsUcesnik())
                return RedirectToAction("Login", "Authentication");

            var ucesnik  = (Ucesnik)Session["user"];
            var prijave  = (List<Prijava>)HttpContext.Application["prijave"];
            var radionice = (List<Radionica>)HttpContext.Application["radionice"];

            // Sve ucesnikove prijave (aktivne i otkazane)
            var mojeQuery = prijave
                .Where(p => p.UcesnikKorisnickoIme == ucesnik.KorisnickoIme)
                .Select(p => new UcesnikPrijavaViewModel
                {
                    Prijava = p,
                    Radionica = radionice.FirstOrDefault(r => r.Id == p.RadionicaId)
                })
                .Where(x => x.Radionica != null)
                .ToList();

            // Filtriranje (ocena 7)
            if (!string.IsNullOrEmpty(naziv))
                mojeQuery = mojeQuery.Where(x => x.Radionica.Naziv.ToLower().Contains(naziv.ToLower())).ToList();
            if (!string.IsNullOrEmpty(kategorija) && Enum.TryParse(kategorija, out OblastEkspertize kat))
                mojeQuery = mojeQuery.Where(x => x.Radionica.Kategorija == kat).ToList();
            if (!string.IsNullOrEmpty(datumPocetka))
                mojeQuery = mojeQuery.Where(x => x.Radionica.DatumVremepocetka.StartsWith(datumPocetka)).ToList();

            // Sortiranje (ocena 7)
            bool asc = sortDir != "desc";
            switch (sort)
            {
                case "naziv":   mojeQuery = asc ? mojeQuery.OrderBy(x => x.Radionica.Naziv).ToList()                : mojeQuery.OrderByDescending(x => x.Radionica.Naziv).ToList(); break;
                case "kat":     mojeQuery = asc ? mojeQuery.OrderBy(x => x.Radionica.Kategorija).ToList()           : mojeQuery.OrderByDescending(x => x.Radionica.Kategorija).ToList(); break;
                case "datum":   mojeQuery = asc ? mojeQuery.OrderBy(x => x.Radionica.DatumVremepocetka).ToList()    : mojeQuery.OrderByDescending(x => x.Radionica.DatumVremepocetka).ToList(); break;
            }

            ViewBag.Sort = sort;
            ViewBag.SortDir = sortDir;
            return View(mojeQuery);
        }

        // POST: Prijava na radionicu
        [HttpPost]
        public ActionResult PrijaviSe(string radionicaId)
        {
            if (!IsUcesnik())
                return RedirectToAction("Login", "Authentication");

            var ucesnik  = (Ucesnik)Session["user"];
            var radionice = (List<Radionica>)HttpContext.Application["radionice"];
            var prijave  = (List<Prijava>)HttpContext.Application["prijave"];

            var radionica = radionice.FirstOrDefault(r => r.Id == radionicaId && r.Status == StatusRadionice.AKTIVNA);
            if (radionica == null)
            {
                TempData["Error"] = "Radionica nije pronađena ili nije aktivna.";
                return RedirectToAction("Index", "Home");
            }

            // Provera da radionica nije pocela
            if (DateTime.TryParseExact(radionica.DatumVremepocetka, "dd/MM/yyyy HH:mm",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime pocetak))
            {
                if (pocetak <= DateTime.Now)
                {
                    TempData["Error"] = "Radionica je već počela, nije moguće prijaviti se.";
                    return RedirectToAction("Index", "Home");
                }
            }

            // Provera slobodnih mesta
            if (radionica.BrojSlobodnihMesta <= 0)
            {
                TempData["Error"] = "Nema slobodnih mesta na radionici.";
                return RedirectToAction("Index", "Home");
            }

            // Provera da ucesnik nije vec prijavljen (osim ako je prethodno otkazao)
            bool vecPrijavljen = prijave.Any(p =>
                p.UcesnikKorisnickoIme == ucesnik.KorisnickoIme &&
                p.RadionicaId == radionicaId &&
                p.Status == StatusPrijave.AKTIVNA);

            if (vecPrijavljen)
            {
                TempData["Error"] = "Već ste prijavljeni na ovu radionicu.";
                return RedirectToAction("Index", "Home");
            }

            // Kreiranje prijave
            var novaPrijava = new Prijava()
            {
                Id = Guid.NewGuid().ToString(),
                UcesnikKorisnickoIme = ucesnik.KorisnickoIme,
                RadionicaId = radionicaId,
                Status = StatusPrijave.AKTIVNA,
                DatumVremeKreiranja = DateTime.Now.ToString("dd/MM/yyyy HH:mm")
            };

            radionica.BrojSlobodnihMesta--;
            prijave.Add(novaPrijava);

            HttpContext.Application["radionice"] = radionice;
            HttpContext.Application["prijave"] = prijave;
            MvcApplication.StoreData("~/App_Data/radionice.json", radionice);
            MvcApplication.StoreData("~/App_Data/prijave.json", prijave);

            TempData["Message"] = "Uspešno ste se prijavili na radionicu!";
            return RedirectToAction("Prijave");
        }

        // POST: Otkazivanje prijave (najkasnije 24h pre pocetka radionice)
        [HttpPost]
        public ActionResult OtkaziPrijavu(string prijavaId)
        {
            if (!IsUcesnik())
                return RedirectToAction("Login", "Authentication");

            var ucesnik  = (Ucesnik)Session["user"];
            var radionice = (List<Radionica>)HttpContext.Application["radionice"];
            var prijave  = (List<Prijava>)HttpContext.Application["prijave"];

            var prijava = prijave.FirstOrDefault(p =>
                p.Id == prijavaId &&
                p.UcesnikKorisnickoIme == ucesnik.KorisnickoIme &&
                p.Status == StatusPrijave.AKTIVNA);

            if (prijava == null)
            {
                TempData["Error"] = "Prijava nije pronađena.";
                return RedirectToAction("Prijave");
            }

            var radionica = radionice.FirstOrDefault(r => r.Id == prijava.RadionicaId);
            if (radionica == null)
            {
                TempData["Error"] = "Radionica nije pronađena.";
                return RedirectToAction("Prijave");
            }

            // Provera 24h
            if (DateTime.TryParseExact(radionica.DatumVremepocetka, "dd/MM/yyyy HH:mm",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime pocetak))
            {
                if ((pocetak - DateTime.Now).TotalHours < 24)
                {
                    TempData["Error"] = "Nije moguće otkazati prijavu manje od 24h pre početka radionice.";
                    return RedirectToAction("Prijave");
                }
            }

            prijava.Status = StatusPrijave.OTKAZANA;
            radionica.BrojSlobodnihMesta++;

            HttpContext.Application["radionice"] = radionice;
            HttpContext.Application["prijave"] = prijave;
            MvcApplication.StoreData("~/App_Data/radionice.json", radionice);
            MvcApplication.StoreData("~/App_Data/prijave.json", prijave);

            TempData["Message"] = "Prijava je uspešno otkazana.";
            return RedirectToAction("Prijave");
        }
    }

    // ViewModel za prikaz prijava ucesnika
    public class UcesnikPrijavaViewModel
    {
        public Prijava Prijava { get; set; }
        public Radionica Radionica { get; set; }
    }
}
