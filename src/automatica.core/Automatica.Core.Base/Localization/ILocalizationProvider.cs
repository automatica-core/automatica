using System;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace Automatica.Core.Base.Localization
{
    /// <summary>
    /// Interface to provide locatization data
    /// </summary>
    public interface ILocalizationProvider
    {
        void LoadFromAssembly(Assembly assembly);
    }
}
