using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using System.Runtime.Caching;

using System.IO;
using System.Configuration;

namespace MessengerBot.Controllers
{
    public class ManageController : Controller
    {
        private const string cacheFormatKey = "CacheKey_RegexFormat";
        private const string cacheCardInfoKey = "CacheKey_RegexCardInfo";
        private const string cacheBadWordsKey = "CacheKey_RegexBadWords";
        private const string cacheValidationObjKey = "CacheKey_Validation";
        string NASPath = AppDomain.CurrentDomain.BaseDirectory;// ConfigurationManager.AppSettings["NASPATH"];      

        public ActionResult Index()
        {
            //return GetCacheData();
            return View();
        }

        private string GetCacheData()
        {
            //return "Clicked";
            string cacheVals = string.Empty;

            ObjectCache cache = MemoryCache.Default;
            List<string> cacheKeys = cache.Select(kvp => kvp.Key).ToList();
            foreach (string cacheKey in cacheKeys)
            {
                // cache.Remove(cacheKey);
                cacheVals += cacheKey + " - " + cache[cacheKey].ToString();
            }

            return "vals: " + cacheVals;
        }
        
        public string Configure()
        {
            return GetCacheData();
        }

        public string ReadLogs()
        {
            string logPath = Path.Combine(NASPath, Path.Combine(ConfigurationManager.AppSettings["WPFOLDER"], ConfigurationManager.AppSettings["MONITORLOG"]));
            string[] lines = System.IO.File.ReadAllLines(logPath);

            string msg = string.Empty;
            foreach (string line in lines)
            {
                // Use a tab to indent each line of the file.
                msg += line + "\n\r";

            }

            //return View(msg);
            return msg;
        }
    }
}