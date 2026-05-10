using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Script.Serialization;
using WebProjectTurist.Models;

namespace WebProjectTurist
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            //  Uèitavanje svih JSON fajlova pri startu aplikacije
            Application["korisnici"] = LoadData<List<Korisnik>>("~/App_Data/users.json") ?? new List<Korisnik>();
            Application["aranzmani"] = LoadData<List<Aranzman>>("~/App_Data/arrangements.json") ?? new List<Aranzman>();
            Application["komentari"] = LoadData<List<Komentar>>("~/App_Data/comments.json") ?? new List<Komentar>();
            Application["rezervacije"] = LoadData<List<Rezervacija>>("~/App_Data/reservations.json") ?? new List<Rezervacija>();
        }

        // Uèitavanje podataka iz JSON fajla
        private T LoadData<T>(string path)
        {
            string filePath = HttpContext.Current.Server.MapPath(path);

            try
            {
                if (!File.Exists(filePath))
                {
                    File.WriteAllText(filePath, "[]");
                    return Activator.CreateInstance<T>();
                }

                string json = File.ReadAllText(filePath);
                if (string.IsNullOrWhiteSpace(json))
                    return Activator.CreateInstance<T>();

                JavaScriptSerializer serializer = new JavaScriptSerializer();
                return serializer.Deserialize<T>(json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Greška pri uèitavanju " + path + ": " + ex.Message);
                return Activator.CreateInstance<T>();
            }
        }

        //Èuvanje podataka u JSON fajl
        public static void StoreData<T>(string path, List<T> data)
        {
            try
            {
                string filePath = HttpContext.Current.Server.MapPath(path);
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                string json = serializer.Serialize(data);

                string directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Greška pri èuvanju " + path + ": " + ex.Message);
            }
        }

        //  Centralizovana metoda za sinhronizaciju svih JSON fajlova
        public static void AzurirajISacuvajSvePodatke()
        {
            var aranzmani = (List<Aranzman>)HttpContext.Current.Application["aranzmani"];
            var korisnici = (List<Korisnik>)HttpContext.Current.Application["korisnici"];
            var rezervacije = (List<Rezervacija>)HttpContext.Current.Application["rezervacije"];
            var komentari = (List<Komentar>)HttpContext.Current.Application["komentari"];

            if (aranzmani == null || korisnici == null || rezervacije == null || komentari == null)
                return;

            // Sinhronizuj aranžmane kod menadžera
            foreach (var korisnik in korisnici)
            {
                if (korisnik.Uloga == UlogaKorisnika.Menadzer && korisnik.KreiraniAranzmani != null)
                {
                    for (int i = 0; i < korisnik.KreiraniAranzmani.Count; i++)
                    {
                        var novi = aranzmani.FirstOrDefault(a => a.Naziv == korisnik.KreiraniAranzmani[i].Naziv);
                        if (novi != null)
                            korisnik.KreiraniAranzmani[i] = novi;
                    }
                }
            }

            //  Sinhronizuj aranžmane i jedinice u rezervacijama
            foreach (var rez in rezervacije)
            {
                //pronaði ažurirani aranžman
                var noviAranzman = aranzmani.FirstOrDefault(a => a.Naziv == rez.AranzmanRezervacije?.Naziv);
                if (noviAranzman != null)
                    rez.AranzmanRezervacije = noviAranzman;

                //ako rezervacija ima jedinicu — pronaði novu verziju te jedinice
                if (rez.Jedinica != null && rez.AranzmanRezervacije != null)
                {
                    SmestajnaJedinica odgovarajuca = null;

                    // traži po ID-u (najpouzdanije)
                    if (!string.IsNullOrEmpty(rez.Jedinica.Id))
                    {
                        odgovarajuca = rez.AranzmanRezervacije.Smestaji
                            .SelectMany(s => s.SmestajneJedinice)
                            .FirstOrDefault(j => j.Id == rez.Jedinica.Id);
                    }

                    // fallback ako stari JSON nema Id — poredi po kljuènim poljima
                    if (odgovarajuca == null)
                    {
                        odgovarajuca = rez.AranzmanRezervacije.Smestaji
                            .SelectMany(s => s.SmestajneJedinice)
                            .FirstOrDefault(j =>
                                j.DozvoljenBrojGostiju == rez.Jedinica.DozvoljenBrojGostiju &&
                                Math.Abs(j.Cena - rez.Jedinica.Cena) < 0.01 &&
                                j.DozvoljeniLjubimci == rez.Jedinica.DozvoljeniLjubimci
                            );
                    }

                    if (odgovarajuca != null)
                        rez.Jedinica = odgovarajuca;
                }
            }

            //Sinhronizuj smeštaje u komentarima
            foreach (var komentar in komentari)
            {
                if (komentar.SmestajKomentar != null)
                {
                    var noviSmestaj = aranzmani
                        .SelectMany(a => a.Smestaji)
                        .FirstOrDefault(s => s.Naziv == komentar.SmestajKomentar.Naziv);

                    if (noviSmestaj != null)
                        komentar.SmestajKomentar = noviSmestaj;
                }
            }

            //Saèuvaj sve izmene nazad u JSON fajlove
            StoreData("~/App_Data/arrangements.json", aranzmani);
            StoreData("~/App_Data/users.json", korisnici);
            StoreData("~/App_Data/reservations.json", rezervacije);
            StoreData("~/App_Data/comments.json", komentari);

            //Ažuriraj Application promenljive u memoriji
            HttpContext.Current.Application["aranzmani"] = aranzmani;
            HttpContext.Current.Application["korisnici"] = korisnici;
            HttpContext.Current.Application["rezervacije"] = rezervacije;
            HttpContext.Current.Application["komentari"] = komentari;
        }

    }
}
