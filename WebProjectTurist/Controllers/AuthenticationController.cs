using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebProjectTurist.Models;

namespace WebProjectTurist.Controllers
{
    public class AuthenticationController : Controller
    {
        // GET: Authentication/Login
        public ActionResult Login()
        {
            return View();
        }

        // GET: Registration form
        public ActionResult Register()
        {
            return View();
        }

        // POST: Register new user
        [HttpPost]
        public ActionResult Register(string korisnickoIme, string lozinka, string ime, string prezime,
                                     string pol, string email, string datumRodjenja)
        {
            var korisnici = (List<Korisnik>)HttpContext.Application["korisnici"];

            if (korisnici.Any(k => k.KorisnickoIme.Equals(korisnickoIme, StringComparison.OrdinalIgnoreCase)))
            {
                ViewBag.Message = "Korisnicko ime vec postoji!";
                return View();
            }

            Korisnik novi = new Korisnik()
            {
                KorisnickoIme = korisnickoIme,
                Lozinka = lozinka,
                Ime = ime,
                Prezime = prezime,
                Pol = pol,
                Email = email,
                DatumRodjenja = datumRodjenja,
                Uloga = UlogaKorisnika.Turista,
                
            };

            korisnici.Add(novi);
            HttpContext.Application["korisnici"] = korisnici;

            MvcApplication.StoreData("~/App_Data/users.json", korisnici);

            Session["user"] = novi;
            return RedirectToAction("Index", "Home");
        }

        // POST: Login
        [HttpPost]
        public ActionResult Login(string korisnickoIme, string lozinka)
        {
            var korisnici = (List<Korisnik>)HttpContext.Application["korisnici"];

            Korisnik user = korisnici.FirstOrDefault(k =>
                k.KorisnickoIme.Equals(korisnickoIme, StringComparison.OrdinalIgnoreCase)
                && k.Lozinka == lozinka && !k.Obrisan);

            if (user == null)
            {
                ViewBag.Message = "Pogrešno korisničko ime ili lozinka!";
                return View("Login");
            }

            Session["user"] = user;
            return RedirectToAction("Index", "Home");
        }

        // GET: Logout
        public ActionResult Logout()
        {
            Session["user"] = null;
            return RedirectToAction("Index", "Home");
        }
    }
}
