using System.Collections.Generic;
using Automatica.Core.Base.License;

namespace Automatica.Core.Plugin.Standalone.Factories
{
    internal class RemoteLicenseState : ILicenseState
    {
        public bool IsLicensed => true;
        public int MaxDatapoints => int.MaxValue;
        public int MaxUsers => int.MaxValue;
        public IList<(string message, string howToSolve)> ValidationErrors => new List<(string message, string howToSolve)>();
    }
}
