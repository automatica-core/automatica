using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using System;
using System.Collections.Generic;
using System.Text;

namespace P3.Driver.EnOcean.DriverFactory.Driver.Simulated
{
    public class EnOceanSimulatedNode : DriverBase
    {
        private readonly P3.Driver.EnOcean.Driver _driver;

        public EnOceanSimulatedNode(IDriverContext driverContext, P3.Driver.EnOcean.Driver driver) : base(driverContext)
        {
            _driver = driver;
        }

        protected P3.Driver.EnOcean.Driver Driver => _driver;

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            if (ctx.NodeInstance.This2NodeTemplateNavigation.This2NodeDataType == (long)NodeDataType.NoAttribute)
            {
                return new EnOceanSimulatedNode(ctx, _driver);
            }
            switch (ctx.NodeInstance.This2NodeTemplateNavigation.Key)
            {
                case "enocean-simulated-relay-value":
                    {
                        return new EnOceanSimulatedRelay(ctx, _driver);
                    }
            }
            return null;
        }
    }
}
