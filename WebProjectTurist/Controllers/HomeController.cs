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
       
        // Helper: pokuša da parsira datum iz formata "dd/MM/yyyy"
        private DateTime? ParseModelDate(string dateStr)
        {
            if (string.IsNullOrEmpty(dateStr))
                return null;

            if (DateTime.TryParseExact(dateStr, "dd/MM/yyyy", CultureInfo.InvariantCulture,
                                       DateTimeStyles.None, out DateTime d))
                return d;

            return null;
        }

        // GET: Home/Index
        public ActionResult Index(
            string searchNaziv,
            string tipPrevoza,
            string tipAranzmana,
            DateTime? datumPocetkaOd,      // donja granica za datum pocetka
            DateTime? datumPocetkaDo,      // gornja granica za datum pocetka
            DateTime? datumZavrsetkaOd,    // donja granica za datum zavrsetka
            DateTime? datumZavrsetkaDo,    // gornja granica za datum zavrsetka
            string sort)
        {
            var aranzmani = (List<Aranzman>)HttpContext.Application["aranzmani"] ?? new List<Aranzman>();

            // filtriranje po nazivu
            if (!string.IsNullOrEmpty(searchNaziv))
                aranzmani = aranzmani
                    .Where(a => !string.IsNullOrEmpty(a.Naziv) &&
                                a.Naziv.IndexOf(searchNaziv, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();

            if (!string.IsNullOrEmpty(tipPrevoza))
                aranzmani = aranzmani
                    .Where(a => a.TipPrevoza.ToString() == tipPrevoza)
                    .ToList();

            if (!string.IsNullOrEmpty(tipAranzmana))
                aranzmani = aranzmani
                    .Where(a => a.TipAranzmana.ToString() == tipAranzmana)
                    .ToList();

            // filtriranje po datumu pocetka (donja i gornja granica)
            if (datumPocetkaOd.HasValue)
            {
                aranzmani = aranzmani
                    .Where(a =>
                    {
                        var d = ParseModelDate(a.DatumPocetka);
                        return d.HasValue && d.Value.Date >= datumPocetkaOd.Value.Date;
                    }).ToList();
            }

            if (datumPocetkaDo.HasValue)
            {
                aranzmani = aranzmani
                    .Where(a =>
                    {
                        var d = ParseModelDate(a.DatumPocetka);
                        return d.HasValue && d.Value.Date <= datumPocetkaDo.Value.Date;
                    }).ToList();
            }

            // filtriranje po datumu zavrsetka (donja i gornja granica)
            if (datumZavrsetkaOd.HasValue)
            {
                aranzmani = aranzmani
                    .Where(a =>
                    {
                        var d = ParseModelDate(a.DatumZavrsetka);
                        return d.HasValue && d.Value.Date >= datumZavrsetkaOd.Value.Date;
                    }).ToList();
            }

            if (datumZavrsetkaDo.HasValue)
            {
                aranzmani = aranzmani
                    .Where(a =>
                    {
                        var d = ParseModelDate(a.DatumZavrsetka);
                        return d.HasValue && d.Value.Date <= datumZavrsetkaDo.Value.Date;
                    }).ToList();
            }

            // sortiranje

            if (string.IsNullOrEmpty(sort))
                sort = "pocetakAsc";

            switch (sort)
            {
                case "nazivAsc":
                    aranzmani = aranzmani.OrderBy(a => a.Naziv).ToList();
                    break;
                case "nazivDesc":
                    aranzmani = aranzmani.OrderByDescending(a => a.Naziv).ToList();
                    break;
                case "pocetakAsc":
                    aranzmani = aranzmani
                        .OrderBy(a =>
                        {
                            var d = ParseModelDate(a.DatumPocetka);
                            return d.HasValue ? d.Value : DateTime.MaxValue;
                        }).ToList();
                    break;
                case "pocetakDesc":
                    aranzmani = aranzmani
                        .OrderByDescending(a =>
                        {
                            var d = ParseModelDate(a.DatumPocetka);
                            return d.HasValue ? d.Value : DateTime.MinValue;
                        }).ToList();
                    break;
                case "krajAsc":
                    aranzmani = aranzmani
                        .OrderBy(a =>
                        {
                            var d = ParseModelDate(a.DatumZavrsetka);
                            return d.HasValue ? d.Value : DateTime.MaxValue;
                        }).ToList();
                    break;
                case "krajDesc":
                    aranzmani = aranzmani
                        .OrderByDescending(a =>
                        {
                            var d = ParseModelDate(a.DatumZavrsetka);
                            return d.HasValue ? d.Value : DateTime.MinValue;
                        }).ToList();
                    break;
            }

            return View(aranzmani);
        }
        public ActionResult Details(
            string naziv,
            string tip,
            string nazivSmestaja,
            bool? bazen,
            bool? spa,
            bool? invaliditet,
            bool? wifi,
            string sortSmestaj,
            int? minGosti,
            int? maxGosti,
            bool? ljubimci,
            double? cena,
            string sortJedinice)
        {
            var arrangements = (List<Aranzman>)HttpContext.Application["aranzmani"];
            var original = arrangements.FirstOrDefault(a => a.Naziv == naziv);
            if (original == null) return HttpNotFound();

            
            var aranzman = new Aranzman
            {
                Naziv = original.Naziv,
                Lokacija = original.Lokacija,
                Opis = original.Opis,
                DatumPocetka = original.DatumPocetka,
                DatumZavrsetka = original.DatumZavrsetka,
                ProgramPutovanja = original.ProgramPutovanja,
                Poster = original.Poster,
                TipAranzmana = original.TipAranzmana,
                TipPrevoza = original.TipPrevoza,
                Smestaji = original.Smestaji?
                    .Select(s => new Smestaj
                    {
                        Naziv = s.Naziv,
                        Tip = s.Tip,
                        BrojZvezdica = s.BrojZvezdica,
                        Bazen = s.Bazen,
                        Spa = s.Spa,
                        Invaliditet = s.Invaliditet,
                        Wifi = s.Wifi,
                        SmestajneJedinice = s.SmestajneJedinice?
                            .Select(j => new SmestajnaJedinica
                            {
                                Cena = j.Cena,
                                DozvoljenBrojGostiju = j.DozvoljenBrojGostiju,
                                DozvoljeniLjubimci = j.DozvoljeniLjubimci
                            }).ToList()
                    }).ToList() ?? new List<Smestaj>()
            };

            var smestaji = aranzman.Smestaji.AsEnumerable();

            // --- Provera da li je neki filter aktivan ---
            bool filterAktivan = !string.IsNullOrEmpty(tip) ||
                                 !string.IsNullOrEmpty(nazivSmestaja) ||
                                 bazen.HasValue || spa.HasValue ||
                                 invaliditet.HasValue || wifi.HasValue ||
                                 !string.IsNullOrEmpty(sortSmestaj) ||
                                 minGosti.HasValue || maxGosti.HasValue ||
                                 ljubimci.HasValue || cena.HasValue ||
                                 !string.IsNullOrEmpty(sortJedinice);

            if (filterAktivan)
            {
                // --- FILTER SMESTAJA ---
                if (!string.IsNullOrEmpty(tip))
                    smestaji = smestaji.Where(s => s.Tip.ToString() == tip);
                if (!string.IsNullOrEmpty(nazivSmestaja))
                    smestaji = smestaji.Where(s => s.Naziv.ToLower().Contains(nazivSmestaja.ToLower()));
                if (bazen.HasValue)
                    smestaji = smestaji.Where(s => s.Bazen == bazen.Value);
                if (spa.HasValue)
                    smestaji = smestaji.Where(s => s.Spa == spa.Value);
                if (invaliditet.HasValue)
                    smestaji = smestaji.Where(s => s.Invaliditet == invaliditet.Value);
                if (wifi.HasValue)
                    smestaji = smestaji.Where(s => s.Wifi == wifi.Value);

                // --- SORTIRANJE SMESTAJA ---
                switch (sortSmestaj)
                {
                    case "nazivAsc": smestaji = smestaji.OrderBy(s => s.Naziv); break;
                    case "nazivDesc": smestaji = smestaji.OrderByDescending(s => s.Naziv); break;
                    case "ukupnoAsc": smestaji = smestaji.OrderBy(s => s.SmestajneJedinice.Count); break;
                    case "ukupnoDesc": smestaji = smestaji.OrderByDescending(s => s.SmestajneJedinice.Count); break;
                    case "slobodneAsc":smestaji = smestaji.OrderBy(s => s.SmestajneJedinice.Count(j => j.Slobodna)).ToList();break;
                    case "slobodneDesc":smestaji = smestaji.OrderByDescending(s => s.SmestajneJedinice.Count(j => j.Slobodna)).ToList();break;
                }

                // --- FILTER I SORTIRANJE SMESTAJNIH JEDINICA ---
                foreach (var s in smestaji)
                {
                    var jedinice = s.SmestajneJedinice.AsEnumerable();

                    if (minGosti.HasValue)
                        jedinice = jedinice.Where(j => j.DozvoljenBrojGostiju >= minGosti.Value);
                    if (maxGosti.HasValue)
                        jedinice = jedinice.Where(j => j.DozvoljenBrojGostiju <= maxGosti.Value);
                    if (ljubimci.HasValue)
                        jedinice = jedinice.Where(j => j.DozvoljeniLjubimci == ljubimci.Value);
                    if (cena.HasValue)
                        jedinice = jedinice.Where(j => j.Cena <= cena.Value);

                    switch (sortJedinice)
                    {
                        case "gostiAsc": jedinice = jedinice.OrderBy(j => j.DozvoljenBrojGostiju); break;
                        case "gostiDesc": jedinice = jedinice.OrderByDescending(j => j.DozvoljenBrojGostiju); break;
                        case "cenaAsc": jedinice = jedinice.OrderBy(j => j.Cena); break;
                        case "cenaDesc": jedinice = jedinice.OrderByDescending(j => j.Cena); break;
                    }

                    s.SmestajneJedinice = jedinice.ToList();
                }
            }

            var comments = (List<Komentar>)HttpContext.Application["komentari"];
            ViewBag.Komentari = comments?
                .Where(c => aranzman.Smestaji.Any(s => s.Naziv == c.SmestajKomentar.Naziv && c.Odobren))
                .ToList() ?? new List<Komentar>();

            
            aranzman.Smestaji = smestaji.ToList();
            return View(aranzman);
        }



    }
}
