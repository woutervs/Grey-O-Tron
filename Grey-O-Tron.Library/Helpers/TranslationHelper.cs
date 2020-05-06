using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using GreyOTron.Library.Translations;

namespace GreyOTron.Library.Helpers
{
    public class TranslationHelper
    {
        public TranslationHelper()
        {
            
        }

        public string Translate(ulong userId, string key, params string[] formatParameters)
        {
            //get language.
            var message = GreyOTronResources.ResourceManager.GetString("", CultureInfo.CurrentCulture);
            if (message == null) return key;
            return string.Format(message, formatParameters.ToArray<object>());
        }
    }
}
