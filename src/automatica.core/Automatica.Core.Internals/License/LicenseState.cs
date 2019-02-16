using Automatica.Core.Base.License;
using System;
using System.Collections.Generic;
using System.Text;

namespace Automatica.Core.Internals.License
{
    public class LicenseState : ILicenseState
    {
        public LicenseState(ILicenseContext context)
        {
            Context = context;
        }
        public bool IsLicensed => Context.IsLicensed;

        public int MaxDatapoints => Context.MaxDatapoints;

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
