//using System;
//using Automatica.Core.Base.Templates;
//using Automatica.Core.Driver;

//namespace P3.Driver.Knx.DriverFactory
//{
//    public class KnxIpSecureDriverFactory : Automatica.Core.Driver.DriverFactory
//    {
//        public override string DriverName => "P3.Driver.KnxSecure";
//        public override Guid DriverGuid => KnxSecureGatway;
//        public override Version DriverVersion => new Version(0, 1, 0, 0);

//        //node types
//        internal static readonly Guid KnxSecureGatway = new Guid("ee218537-9b35-4e10-b929-418748dc1728");


//        public override void InitNodeTemplates(INodeTemplateFactory factory)
//        {
            
//        }

//        public override IDriver CreateDriver(IDriverContext config)
//        {
//            return new KnxDriver(config, true);
//        }
//    }
//}
