using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using WebProjectTurist.Models;

namespace WebProjectTurist.Controllers
{
    public class AdminController : Controller
    {
        private bool IsAdmin()
        {
            return Session["user"] != null && (string)Session["uloga"] == "Administrator";
        }

        // Pregled svih predavaca sa filtrom i sortiranjem (ocena 7)
        public ActionResult Predavaci(string jmbg, string ime, string prezime, string datumRodjenja,
                                      string email, string oblast, string status, string sort, string sortDir)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Authentication");

            var predavaci = (List<Predavac>)HttpContext.Application["predavaci"];

            // --- Filtriranje ---
            if (!string.IsNullOrEmpty(jmbg))
                predavaci = predavaci.Where(p => p.JMBG.Contains(jmbg)).ToList();
            if (!string.IsNullOrEmpty(ime))
                predavaci = predavaci.Where(p => p.Ime.ToLower().Contains(ime.ToLower())).ToList();
            if (!string.IsNullOrEmpty(prezime))
                predavaci = predavaci.Where(p => p.Prezime.ToLower().Contains(prezime.ToLower())).ToList();
            if (!string.IsNullOrEmpty(datumRodjenja))
                predavaci = predavaci.Where(p => p.DatumRodjenja.Contains(datumRodjenja)).ToList();
            if (!string.IsNullOrEmpty(email))
                predavaci = predavaci.Where(p => p.Email.ToLower().Contains(email.ToLower())).ToList();
            if (!string.IsNullOrEmpty(oblast) && Enum.TryParse(oblast, out OblastEkspertize o))
                predavaci = predavaci.Where(p => p.OblastEkspertize == o).ToList();
            if (!string.IsNullOrEmpty(status) && Enum.TryParse(status, out StatusNaloga s))
                predavaci = predavaci.Where(p => p.StatusNaloga == s).ToList();

            // --- Sortiranje ---
            bool asc = sortDir != "desc";
            switch (sort)
            {
                case "jmbg":        predavaci = asc ? predavaci.OrderBy(p => p.JMBG).ToList()             : predavaci.OrderByDescending(p => p.JMBG).ToList(); break;
                case "ime":         predavaci = asc ? predavaci.OrderBy(p => p.Ime).ToList()              : predavaci.OrderByDescending(p => p.Ime).ToList(); break;
                case "prezime":     predavaci = asc ? predavaci.OrderBy(p => p.Prezime).ToList()          : predavaci.OrderByDescending(p => p.Prezime).ToList(); break;
                case "datum":       predavaci = asc ? predavaci.OrderBy(p => p.DatumRodjenja).ToList()    : predavaci.OrderByDescending(p => p.DatumRodjenja).ToList(); break;
                case "email":       predavaci = asc ? predavaci.OrderBy(p => p.Email).ToList()            : predavaci.OrderByDescending(p => p.Email).ToList(); break;
                case "oblast":      predavaci = asc ? predavaci.OrderBy(p => p.OblastEkspertize).ToList() : predavaci.OrderByDescending(p => p.OblastEkspertize).ToList(); break;
                case "status":      predavaci = asc ? predavaci.OrderBy(p => p.StatusNaloga).ToList()     : predavaci.OrderByDescending(p => p.StatusNaloga).ToList(); break;
            }

            ViewBag.Sort = sort;
            ViewBag.SortDir = sortDir;
            return View(predavaci);
        }

        // GET: Kreiranje novog predavaca
        public ActionResult NoviPredavac()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Authentication");
            return View();
        }

        // POST: Kreiranje novog predavaca
        [HttpPost]
        public ActionResult NoviPredavac(string korisnickoIme, string lozinka, string jmbg,
                                          string ime, string prezime, string datumRodjenja,
                                          string email, string oblastEkspertize)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Authentication");

            var predavaci = (List<Predavac>)HttpContext.Application["predavaci"];
            var ucesnici  = (List<Ucesnik>)HttpContext.Application["ucesnici"];

            // Jedinstvenost korisnickog imena
            if (predavaci.Any(p => p.KorisnickoIme.Equals(korisnickoIme, StringComparison.OrdinalIgnoreCase))
             || ucesnici.Any(u => u.KorisnickoIme.Equals(korisnickoIme, StringComparison.OrdinalIgnoreCase)))
            {
                ViewBag.Error = "Korisničko ime već postoji!";
                return View();
            }

            // JMBG validacija
            if (string.IsNullOrEmpty(jmbg) || jmbg.Length != 13 || !Regex.IsMatch(jmbg, @"^\d{13}$"))
            {
                ViewBag.Error = "JMBG mora biti tačno 13 numeričkih karaktera!";
                return View();
            }
            if (predavaci.Any(p => p.JMBG == jmbg) || ucesnici.Any(u => u.JMBG == jmbg))
            {
                ViewBag.Error = "JMBG već postoji u sistemu!";
                return View();
            }

            // Email jedinstvenost
            if (predavaci.Any(p => p.Email.Equals(email, StringComparison.OrdinalIgnoreCase))
             || ucesnici.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
            {
                ViewBag.Error = "Email adresa već postoji!";
                return View();
            }

            if (!Enum.TryParse(oblastEkspertize, out OblastEkspertize oblast))
            {
                ViewBag.Error = "Neispravna oblast ekspertize!";
                return View();
            }

            var novi = new Predavac()
            {
                KorisnickoIme = korisnickoIme,
                Lozinka = lozinka,
                JMBG = jmbg,
                Ime = ime,
                Prezime = prezime,
                DatumRodjenja = datumRodjenja,
                Email = email,
                OblastEkspertize = oblast,
                StatusNaloga = StatusNaloga.AKTIVAN
            };

            predavaci.Add(novi);
            HttpContext.Application["predavaci"] = predavaci;
            MvcApplication.StoreData("~/App_Data/predavaci.json", predavaci);

            TempData["Message"] = "Predavač uspešno kreiran.";
            return RedirectToAction("Predavaci");
        }

        // GET: Azuriranje predavaca
        public ActionResult AzurirajPredavaca(string korisnickoIme)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Authentication");

            var predavaci = (List<Predavac>)HttpContext.Application["predavaci"];
            var predavac = predavaci.FirstOrDefault(p => p.KorisnickoIme == korisnickoIme);
            if (predavac == null)
                return HttpNotFound();

            return View(predavac);
        }

        // POST: Azuriranje predavaca (ne menja se korisnickoIme ni JMBG)
        [HttpPost]
        public ActionResult AzurirajPredavaca(string korisnickoIme, string lozinka, string ime,
                                               string prezime, string datumRodjenja, string email,
                                               string oblastEkspertize)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Authentication");

            var predavaci = (List<Predavac>)HttpContext.Application["predavaci"];
            var ucesnici  = (List<Ucesnik>)HttpContext.Application["ucesnici"];
            var predavac = predavaci.FirstOrDefault(p => p.KorisnickoIme == korisnickoIme);
            if (predavac == null)
                return HttpNotFound();

            // Email jedinstvenost (iskljucuje samog sebe)
            if (predavaci.Any(p => p.KorisnickoIme != korisnickoIme && p.Email.Equals(email, StringComparison.OrdinalIgnoreCase))
             || ucesnici.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
            {
                ViewBag.Error = "Email adresa već postoji!";
                return View(predavac);
            }

            if (!Enum.TryParse(oblastEkspertize, out OblastEkspertize oblast))
            {
                ViewBag.Error = "Neispravna oblast ekspertize!";
                return View(predavac);
            }

            predavac.Lozinka = lozinka;
            predavac.Ime = ime;
            predavac.Prezime = prezime;
            predavac.DatumRodjenja = datumRodjenja;
            predavac.Email = email;
            predavac.OblastEkspertize = oblast;

            HttpContext.Application["predavaci"] = predavaci;
            MvcApplication.StoreData("~/App_Data/predavaci.json", predavaci);

            TempData["Message"] = "Predavač uspešno ažuriran.";
            return RedirectToAction("Predavaci");
        }

        // POST: Blokiranje predavaca
        [HttpPost]
        public ActionResult BlokirajPredavaca(string korisnickoIme)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Authentication");

            var predavaci = (List<Predavac>)HttpContext.Application["predavaci"];
            var radionice = (List<Radionica>)HttpContext.Application["radionice"];
            var prijave   = (List<Prijava>)HttpContext.Application["prijave"];

            var predavac = predavaci.FirstOrDefault(p => p.KorisnickoIme == korisnickoIme);
            if (predavac == null)
                return HttpNotFound();

            // Blokiranje
            predavac.StatusNaloga = StatusNaloga.BLOKIRAN;

            // Brisanje svih radionice predavaca i otkazivanje aktivnih prijava
            var radionicePredavaca = radionice.Where(r => r.PredavacKorisnickoIme == korisnickoIme).ToList();
            foreach (var rad in radionicePredavaca)
            {
                // Otkazivanje aktivnih prijava
                foreach (var prijava in prijave.Where(p => p.RadionicaId == rad.Id && p.Status == StatusPrijave.AKTIVNA))
                {
                    prijava.Status = StatusPrijave.OTKAZANA;
                }
                // Fizicko brisanje radionice
                radionice.Remove(rad);
            }

            HttpContext.Application["predavaci"] = predavaci;
            HttpContext.Application["radionice"] = radionice;
            HttpContext.Application["prijave"] = prijave;

            MvcApplication.StoreData("~/App_Data/predavaci.json", predavaci);
            MvcApplication.StoreData("~/App_Data/radionice.json", radionice);
            MvcApplication.StoreData("~/App_Data/prijave.json", prijave);

            TempData["Message"] = "Predavač je blokiran, sve njegove radionice su obrisane i aktivne prijave otkazane.";
            return RedirectToAction("Predavaci");
        }
    }
}
