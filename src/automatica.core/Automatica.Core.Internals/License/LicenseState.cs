using Automatica.Core.Base.License;
using System.Collections.Generic;

namespace Automatica.Core.Internals.License
{
    public class LicenseState : ILicenseState
    {
        public LicenseState(ILicenseContext context)
        {
            Context = context;
        }
        public bool IsLicensed => Context.IsLicensed;

        public int MaxDataPoints => Context.MaxDataPoints;

        public int MaxUsers => Context.MaxUsers;

        public IList<(string message, string howToSolve)> ValidationErrors
        {
            get
            {
                var list = new List<(string, string)>();

                foreach(var error in Context.ValidationErrors)
                {
                    list.Add((error.Message, error.HowToResolve));
                }
                return list;
            }
        }

        public ILicenseContext Context { get; }
    }
}
