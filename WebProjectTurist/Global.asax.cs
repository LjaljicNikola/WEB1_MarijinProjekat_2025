using System;
using System.Collections.Generic;
using System.IO;
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

            // Učitavanje svih JSON fajlova pri startu aplikacije
            Application["ucesnici"] = LoadData<List<Ucesnik>>("~/App_Data/ucesnici.json") ?? new List<Ucesnik>();
            Application["predavaci"] = LoadData<List<Predavac>>("~/App_Data/predavaci.json") ?? new List<Predavac>();
            Application["administratori"] = LoadData<List<Administrator>>("~/App_Data/administratori.json") ?? new List<Administrator>();
            Application["radionice"] = LoadData<List<Radionica>>("~/App_Data/radionice.json") ?? new List<Radionica>();
            Application["prijave"] = LoadData<List<Prijava>>("~/App_Data/prijave.json") ?? new List<Prijava>();
        }

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
                System.Diagnostics.Debug.WriteLine("Greška pri učitavanju " + path + ": " + ex.Message);
                return Activator.CreateInstance<T>();
            }
        }

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
                System.Diagnostics.Debug.WriteLine("Greška pri čuvanju " + path + ": " + ex.Message);
            }
        }
    }
}
