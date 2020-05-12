using System.Globalization;
using System.Linq;
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
            var ci = CultureInfo.CurrentCulture;

            var message = GreyOTronResources.ResourceManager.GetString(key, ci) ?? key;
            var translatedFormatParameters = formatParameters.Select(formatParameter => GreyOTronResources.ResourceManager.GetString(formatParameter, ci) ?? formatParameter).ToList();
            return string.Format(message, translatedFormatParameters.ToArray<object>());
        }
    }
}
