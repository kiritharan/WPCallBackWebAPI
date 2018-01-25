using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Runtime.Caching;

namespace MessengerBot.Pages
{
    public partial class Hello : System.Web.UI.Page
    {
        private const string cacheFormatKey = "CacheKey_RegexFormat";
        private const string cacheCardInfoKey = "CacheKey_RegexCardInfo";
        private const string cacheBadWordsKey = "CacheKey_RegexBadWords";
        private const string cacheValidationObjKey = "CacheKey_Validation";

        protected void Page_Load(object sender, EventArgs e)
        {

        }

        protected void cleanCache_Click(object sender, EventArgs e)
        {
            try
            {
                ObjectCache cache = MemoryCache.Default;
                List<string> cacheKeys = cache.Select(k => k.Key).ToList();
                 foreach (string item in cacheKeys)
                {
                    if (item == cacheFormatKey)
                        cache.Remove(item);
                    if (item == cacheCardInfoKey)
                        cache.Remove(item);
                    if (item == cacheBadWordsKey)
                        cache.Remove(item);
                    if (item == cacheValidationObjKey)
                        cache.Remove(item);
                }

            }
            catch (Exception ex) { }
        }
    }
}
