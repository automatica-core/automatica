using System;
using Automatica.Core.Base.Templates;

namespace Automatica.Core.Base
{
    public enum Language
    {
        [EnumName("COMMON.PROPERTY.LANGUAGE.GERMAN")]
        German = 0,
        [EnumName("COMMON.PROPERTY.LANGUAGE.ENGLISH")]
        English = 1
    }

    public static class LanguageHelper
    {
        public static string GetLanguage(Language language)
        {
            switch (language)
            {
                case Language.German:
                    return "de";
                case Language.English:
                    return "en";
                default:
                    throw new ArgumentOutOfRangeException(nameof(language), language, null);
            }
        }
    }
}