using System.Globalization;
using Automatica.Core.Base.Localization;
using Automatica.Core.EF.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Automatica.Core.WebApi.Controllers
{
    [Route("webapi/localization")]
    [AllowAnonymous]
    public class LocalizationController : BaseController
    {
        private readonly ILocalizationProvider _provider;

        public LocalizationController(ILocalizationProvider provider, AutomaticaContext db)
            : base(db)
        {
            _provider = provider;
        }

        [HttpGet]
        public object Get()
        {
            return _provider.ToJson(CultureInfo.CurrentCulture.TwoLetterISOLanguageName);
        }

        [HttpGet]
        [Route("{langCode}")]
        public object Get(string langCode)
        {
            return _provider.ToJson(langCode);
        }
    }
}
