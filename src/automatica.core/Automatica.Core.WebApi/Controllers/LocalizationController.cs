using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Automatica.Core.Base.Localization;
using Automatica.Core.EF.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Automatica.Core.WebApi.Controllers
{
    [Route("localization")]
    [AllowAnonymous]
    public class LocalizationController : BaseController
    {
        private readonly LocalizationProvider _provider;

        public LocalizationController(LocalizationProvider provider, AutomaticaContext db)
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
