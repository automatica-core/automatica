using System;
using System.Collections.Generic;
using System.Text;
using Automatica.Core.Driver;
using P3.Driver.EnOcean.DriverFactory.Driver.Learned;

namespace P3.Driver.EnOcean.DriverFactory.Driver.Data
{
    public class EnOceanTmpData : EnOceanDataNode
    {
        public EnOceanTmpData(IDriverContext driverContext, ITeachInManager teachInManager) : base(driverContext, teachInManager)
        {
        }
    }
}
