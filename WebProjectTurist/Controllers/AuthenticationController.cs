using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using WebProjectTurist.Models;

namespace WebProjectTurist.Controllers
{
    public class AuthenticationController : Controller
    {
        // GET: Login (pocetna strana)
        public ActionResult Login()
        {
            return View();
        }

        // GET: Register
        public ActionResult Register()
        {
            return View();
        }

        // POST: Register - registracija novog Ucesnika
        [HttpPost]
        public ActionResult Register(string korisnickoIme, string lozinka, string jmbg,
                                     string ime, string prezime, string datumRodjenja,
                                     string email, string nivoObrazovanja)
        {
            var ucesnici = (List<Ucesnik>)HttpContext.Application["ucesnici"];
            var predavaci = (List<Predavac>)HttpContext.Application["predavaci"];

            // Validacija jedinstvenosti korisnickog imena
            if (ucesnici.Any(u => u.KorisnickoIme.Equals(korisnickoIme, StringComparison.OrdinalIgnoreCase))
             || predavaci.Any(p => p.KorisnickoIme.Equals(korisnickoIme, StringComparison.OrdinalIgnoreCase)))
            {
                ViewBag.Message = "Korisničko ime već postoji!";
                return View();
            }

            // Validacija JMBG - tacno 13 numeričkih karaktera, jedinstven
            if (string.IsNullOrEmpty(jmbg) || jmbg.Length != 13 || !Regex.IsMatch(jmbg, @"^\d{13}$"))
            {
                ViewBag.Message = "JMBG mora biti tačno 13 numeričkih karaktera!";
                return View();
            }
            if (ucesnici.Any(u => u.JMBG == jmbg) || predavaci.Any(p => p.JMBG == jmbg))
            {
                ViewBag.Message = "JMBG već postoji u sistemu!";
                return View();
            }

            // Validacija email jedinstvenosti
            if (ucesnici.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase))
             || predavaci.Any(p => p.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
            {
                ViewBag.Message = "Email adresa već postoji u sistemu!";
                return View();
            }

            if (!Enum.TryParse(nivoObrazovanja, out NivoObrazovanja nivo))
            {
                ViewBag.Message = "Neispravan nivo obrazovanja!";
                return View();
            }

            Ucesnik novi = new Ucesnik()
            {
                KorisnickoIme = korisnickoIme,
                Lozinka = lozinka,
                JMBG = jmbg,
                Ime = ime,
                Prezime = prezime,
                DatumRodjenja = datumRodjenja,
                Email = email,
                NivoObrazovanja = nivo
            };

            ucesnici.Add(novi);
            HttpContext.Application["ucesnici"] = ucesnici;
            MvcApplication.StoreData("~/App_Data/ucesnici.json", ucesnici);

            Session["user"] = novi;
            Session["uloga"] = "Ucesnik";
            return RedirectToAction("Index", "Home");
        }

        // POST: Login
        [HttpPost]
        public ActionResult Login(string korisnickoIme, string lozinka)
        {
            var ucesnici     = (List<Ucesnik>)HttpContext.Application["ucesnici"];
            var predavaci    = (List<Predavac>)HttpContext.Application["predavaci"];
            var administratori = (List<Administrator>)HttpContext.Application["administratori"];

            // Provjera u ucesnicima
            var ucesnik = ucesnici.FirstOrDefault(u =>
                u.KorisnickoIme.Equals(korisnickoIme, StringComparison.OrdinalIgnoreCase)
                && u.Lozinka == lozinka);
            if (ucesnik != null)
            {
                Session["user"] = ucesnik;
                Session["uloga"] = "Ucesnik";
                return RedirectToAction("Index", "Home");
            }

            // Provjera u predavacima - blokirani ne mogu da se prijave
            var predavac = predavaci.FirstOrDefault(p =>
                p.KorisnickoIme.Equals(korisnickoIme, StringComparison.OrdinalIgnoreCase)
                && p.Lozinka == lozinka);
            if (predavac != null)
            {
                if (predavac.StatusNaloga == StatusNaloga.BLOKIRAN)
                {
                    ViewBag.Message = "Vaš nalog je blokiran i ne možete se prijaviti na sistem.";
                    return View();
                }
                Session["user"] = predavac;
                Session["uloga"] = "Predavac";
                return RedirectToAction("Index", "Home");
            }

            // Provjera u administratorima
            var admin = administratori.FirstOrDefault(a =>
                a.KorisnickoIme.Equals(korisnickoIme, StringComparison.OrdinalIgnoreCase)
                && a.Lozinka == lozinka);
            if (admin != null)
            {
                Session["user"] = admin;
                Session["uloga"] = "Administrator";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Message = "Pogrešno korisničko ime ili lozinka!";
            return View();
        }

        // GET: Logout
        public ActionResult Logout()
        {
            Session["user"] = null;
            Session["uloga"] = null;
            return RedirectToAction("Login");
        }
    }
}
