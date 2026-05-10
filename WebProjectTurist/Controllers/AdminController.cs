using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebProjectTurist.Models;

namespace WebProjectTurist.Controllers
{
    public class AdminController : Controller
    {
        // Prikaz svih korisnika sa pretragom i filtriranjem
        public ActionResult Korisnici(string pretraga, string uloga)
        {
            var user = (Korisnik)Session["user"];
            if (user == null || user.Uloga != UlogaKorisnika.Administrator)
                return RedirectToAction("Login", "Authentication");

            var korisnici = (List<Korisnik>)HttpContext.Application["korisnici"];

            // Filtriranje po imenu/prezimenu
            if (!string.IsNullOrEmpty(pretraga))
            {
                pretraga = pretraga.ToLower();
                korisnici = korisnici
                    .Where(k => k.Ime.ToLower().Contains(pretraga) || k.Prezime.ToLower().Contains(pretraga))
                    .ToList();
            }

            // Filtriranje po ulozi
            if (!string.IsNullOrEmpty(uloga))
            {
                if (Enum.TryParse(uloga, out UlogaKorisnika u))
                    korisnici = korisnici.Where(k => k.Uloga == u).ToList();
            }

            return View(korisnici);
        }

        // GET: Admin/NoviMenadzer
        public ActionResult NoviMenadzer()
        {
            var user = (Korisnik)Session["user"];
            if (user == null || user.Uloga != UlogaKorisnika.Administrator)
                return RedirectToAction("Login", "Authentication");

            return View();
        }

        // POST: Admin/NoviMenadzer
        [HttpPost]
        public ActionResult NoviMenadzer(Korisnik menadzer)
        {
            var user = (Korisnik)Session["user"];
            if (user == null || user.Uloga != UlogaKorisnika.Administrator)
                return RedirectToAction("Login", "Authentication");

            var korisnici = (List<Korisnik>)HttpContext.Application["korisnici"];

            if (korisnici.Any(k => k.KorisnickoIme == menadzer.KorisnickoIme))
            {
                ViewBag.Error = "Korisničko ime već postoji!";
                return View(menadzer);
            }

            menadzer.Uloga = UlogaKorisnika.Menadzer;
            menadzer.Obrisan = false;

            korisnici.Add(menadzer);
            HttpContext.Application["korisnici"] = korisnici;

            // Čuvanje u JSON fajl
            MvcApplication.StoreData("~/App_Data/users.json", korisnici);

            TempData["Message"] = "Menadžer uspešno registrovan.";
            return RedirectToAction("Korisnici");
        }
    }
}
