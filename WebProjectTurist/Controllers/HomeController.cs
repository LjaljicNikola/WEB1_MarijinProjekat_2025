using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using WebProjectTurist.Models;

namespace WebProjectTurist.Controllers
{
    public class HomeController : Controller
    {
        // Pocetna strana - pregled aktivnih predstojecih radionice
        public ActionResult Index()
        {
            var radionice = (List<Radionica>)HttpContext.Application["radionice"];

            var predstojeceAktivne = radionice
                .Where(r => r.Status == StatusRadionice.AKTIVNA)
                .ToList();

            return View(predstojeceAktivne);
        }

        // Detalji jedne radionice
        public ActionResult Detalji(string id)
        {
            var radionice = (List<Radionica>)HttpContext.Application["radionice"];
            var predavaci = (List<Predavac>)HttpContext.Application["predavaci"];

            var radionica = radionice.FirstOrDefault(r => r.Id == id);
            if (radionica == null)
                return HttpNotFound();

            var predavac = predavaci.FirstOrDefault(p => p.KorisnickoIme == radionica.PredavacKorisnickoIme);
            ViewBag.Predavac = predavac;

            return View(radionica);
        }
    }
}
