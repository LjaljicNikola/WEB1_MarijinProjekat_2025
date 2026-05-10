using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebProjectTurist.Models;

namespace WebProjectTurist.Controllers
{
    public class ProfileController : Controller
    {
        // GET: Profile/View
        public ActionResult ViewProfile()
        {
            var user = (Korisnik)Session["user"];
            if (user == null)
                return RedirectToAction("Login", "Authentication");

            return View(user);
        }

        // GET: Profile/Edit
        public ActionResult EditProfile()
        {
            var user = (Korisnik)Session["user"];
            if (user == null)
                return RedirectToAction("Login", "Authentication");

            return View(user);
        }

        // POST: Profile/Edit
        [HttpPost]
        public ActionResult EditProfile(string ime, string prezime, string pol, string email, string datumRodjenja)
        {
            var user = (Korisnik)Session["user"];
            if (user == null)
                return RedirectToAction("Login", "Authentication");

            var korisnici = (List<Korisnik>)HttpContext.Application["korisnici"];
            var original = korisnici.FirstOrDefault(k => k.KorisnickoIme == user.KorisnickoIme);

            if (original != null)
            {
                original.Ime = ime;
                original.Prezime = prezime;
                original.Pol = pol;
                original.Email = email;
                original.DatumRodjenja = datumRodjenja;

                // Ažuriraj u sesiji i JSON fajlu
                Session["user"] = original;
                HttpContext.Application["korisnici"] = korisnici;
                MvcApplication.StoreData("~/App_Data/users.json", korisnici);
            }

            ViewBag.Message = "Profil uspešno ažuriran!";
            return View("ViewProfile", original);
        }

      
    }
}
