using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using P3.Logic.Surveillance.Security;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.Surveillance.Sia
{
    /// <summary>
    /// SIA DC-09 (Loxone "SIA DC-09" message object): sends alarm events to a monitoring station over TCP, with optional
    /// AES encryption, a backup receiver, supervision polling and a queue that survives restarts.
    /// </summary>
    public class SiaLogicFactory : LogicFactory
    {
        public const int TriggerCount = 4;

        public static readonly Guid[] InTriggers = { new Guid("2c6b47c8-1eff-4cb3-a504-900470145498"), new Guid("f6782a7d-d8f9-406e-ada8-4706e9984324"), new Guid("c7102271-4c23-4c0f-855c-9a3384fa4f19"), new Guid("b884017d-80da-499b-abab-b9e465a1bd98") };
        public static readonly Guid InMessage = new Guid("cd4f4601-5e1d-4f0c-a276-755070b13150");
        public static readonly Guid InOff = new Guid("929e5e53-6db1-4282-bcb7-f22863b723e2");

        public static readonly Guid OutOk = new Guid("7901d207-101d-4249-953a-5418aff80b8a");
        public static readonly Guid OutError = new Guid("5c2b3ba1-5b14-4cc8-805c-6e6df943dcab");
        public static readonly Guid OutQueue = new Guid("bfa3dba4-951c-4e7b-b3b0-6c791d03bc13");
        public static readonly Guid OutLastEvent = new Guid("b921cdf0-e2ab-40b6-b8ea-37ae52e4662b");

        public static readonly Guid ParamServer = new Guid("002b6933-ec2f-4d2d-bfd7-1711dbacfd6e");
        public static readonly Guid ParamBackupServer = new Guid("df902e16-8a92-40e4-9730-200df277cdcd");
        public static readonly Guid ParamPolling = new Guid("7cec9c39-4c85-4d50-ba61-047d02bf2b6e");
        public static readonly Guid ParamBackupPolling = new Guid("fc911c71-97b8-4c1e-b0a5-87c5520d2c28");
        public static readonly Guid ParamTimeout = new Guid("3628879b-86a3-4f19-96a1-c04bc35c2b8e");
        public static readonly Guid ParamFormat = new Guid("d68267f3-5c5f-48a2-b25d-a1fffe925c7f");
        public static readonly Guid ParamKey = new Guid("737adb13-0271-4e8f-800f-5bff1f612c12");
        public static readonly Guid ParamAccount = new Guid("0bc83aaf-cfd3-4c5d-bb0d-ed0fe9f1fa99");
        public static readonly Guid ParamPrefix = new Guid("cfc66701-4db1-43d7-8201-4ad529f1f57f");
        public static readonly Guid ParamReceiver = new Guid("b5f8319c-0ced-4614-b163-c0d8df833eb2");
        public static readonly Guid ParamMinInterval = new Guid("b8be0867-68e4-4bea-975d-ff7f5f85ecf9");
        public static readonly Guid[] ParamOnMessages = { new Guid("087d0dac-f0e5-46ab-8a03-5be8292960f1"), new Guid("d888dadc-704b-4c85-84b9-56124cd57c1d"), new Guid("3e5ecdc1-8afb-4ff6-adc8-e0014a0a6554"), new Guid("182e7f17-da50-4926-ac7a-321f392a5397") };
        public static readonly Guid[] ParamOffMessages = { new Guid("ff07a13e-36ab-484c-abc6-8e74ee13a18d"), new Guid("4b2e3bd2-80e2-4d13-a1ab-0c9222064e8f"), new Guid("b422e73a-1546-4eba-8515-bd6f501eb478"), new Guid("2602f99c-f1c1-4a39-b971-ef89d185e1f2") };

        public override string LogicName => "Surveillance.SiaDc09";
        public override Guid LogicGuid => new Guid("fe7c800e-2f46-4fde-a92d-7a5c981d029b");
        public override Version LogicVersion => new Version(1, 0, 0, 0);

        private const string P = "SURVEILLANCE.SIA.";

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, $"{P}NAME", $"{P}DESCRIPTION", "surveillance.sia_dc09", "SURVEILLANCE.NAME", 100, 100);

            var sort = 1;
            for (var i = 0; i < TriggerCount; i++)
            {
                factory.CreateLogicInterfaceTemplate(InTriggers[i], $"I{i + 1}", $"{P}TRIGGER.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, int.MaxValue, sort++);
            }
            factory.CreateLogicInterfaceTemplate(InMessage, "Msg", $"{P}MESSAGE_IN.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, int.MaxValue, sort++);
            factory.CreateLogicInterfaceTemplate(InOff, "Off", $"{P}OFF.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, int.MaxValue, sort);

            factory.CreateLogicInterfaceTemplate(OutOk, "Ok", $"{P}OK.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1, RuleInterfaceType.Status);
            factory.CreateLogicInterfaceTemplate(OutError, "Err", $"{P}ERROR.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 2, RuleInterfaceType.Status);
            factory.CreateLogicInterfaceTemplate(OutQueue, "Q", $"{P}QUEUE.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 3, RuleInterfaceType.Status);
            factory.CreateLogicInterfaceTemplate(OutLastEvent, "Last", $"{P}LAST_EVENT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 4, RuleInterfaceType.Status);

            sort = 1;
            void Param(Guid id, string key, RuleInterfaceParameterDataType type, object value) =>
                factory.CreateParameterLogicInterfaceTemplate(id, P + key + ".NAME", P + key + ".DESCRIPTION", LogicGuid, sort++, type, value);

            Param(ParamServer, "SERVER", RuleInterfaceParameterDataType.Text, "");
            Param(ParamBackupServer, "BACKUP_SERVER", RuleInterfaceParameterDataType.Text, "");
            Param(ParamPolling, "POLLING", RuleInterfaceParameterDataType.Integer, 90);
            Param(ParamBackupPolling, "BACKUP_POLLING", RuleInterfaceParameterDataType.Integer, 1800);
            Param(ParamTimeout, "TIMEOUT", RuleInterfaceParameterDataType.Integer, 3);
            Param(ParamFormat, "FORMAT", RuleInterfaceParameterDataType.Integer, 0);
            Param(ParamKey, "KEY", RuleInterfaceParameterDataType.Text, "");
            Param(ParamAccount, "ACCOUNT", RuleInterfaceParameterDataType.Text, "");
            Param(ParamPrefix, "PREFIX", RuleInterfaceParameterDataType.Text, "0");
            Param(ParamReceiver, "RECEIVER", RuleInterfaceParameterDataType.Text, "");
            Param(ParamMinInterval, "MIN_INTERVAL", RuleInterfaceParameterDataType.Integer, 5);
            for (var i = 0; i < TriggerCount; i++)
            {
                Param(ParamOnMessages[i], $"ON_MESSAGE_{i + 1}", RuleInterfaceParameterDataType.Text, "");
                Param(ParamOffMessages[i], $"OFF_MESSAGE_{i + 1}", RuleInterfaceParameterDataType.Text, "");
            }
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new SiaLogic(context, FileSecurityStateStore.Default, TcpSiaTransport.Instance);
        }
    }
}
