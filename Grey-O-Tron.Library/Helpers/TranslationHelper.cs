using System.Globalization;
using System.Linq;
using GreyOTron.Library.Services;
using GreyOTron.Resources;

namespace GreyOTron.Library.Helpers
{
    public class TranslationHelper
    {
        private readonly LanguagesService languageService;

        public TranslationHelper(LanguagesService languageService)
        {
            this.languageService = languageService;
        }

        public string Translate(ulong userId, ulong? serverId, string key, params string[] formatParameters)
        {
            var ci = languageService.GetForUserId(userId);
            if (ci.Equals(CultureInfo.InvariantCulture) && serverId.HasValue)
            {
                ci = languageService.GetForServerId(serverId.Value);
            }

            var message = GreyOTronResources.ResourceManager.GetString(key, ci) ?? key;
            var translatedFormatParameters = formatParameters.Select(formatParameter => GreyOTronResources.ResourceManager.GetString(formatParameter, ci) ?? formatParameter).ToList();
            return string.Format(message, translatedFormatParameters.ToArray<object>());
        }
    }
}
