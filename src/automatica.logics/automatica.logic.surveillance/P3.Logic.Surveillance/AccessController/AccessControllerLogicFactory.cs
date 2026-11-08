using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using P3.Logic.Surveillance.Security;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.Surveillance.AccessController
{
    /// <summary>
    /// Access controller (Loxone "Access Controller" / "NFC Code Touch" authentication): users with codes and tags,
    /// user groups with access times, a door output and an access log. Users are managed by administrators in the
    /// visualization; codes and tags are stored hashed.
    /// </summary>
    public class AccessControllerLogicFactory : LogicFactory
    {
        public static readonly Guid InCode = new Guid("ba7af8df-bdd6-4dd0-8de6-9eade69c0a2d");
        public static readonly Guid InTag = new Guid("5bbb10dc-2ded-4699-a8f3-0cd0799d888d");
        public static readonly Guid InOpen = new Guid("57cb823f-1180-4e8d-afeb-af384c1d07c9");
        public static readonly Guid InOff = new Guid("49a1f111-a79b-4f1e-8d32-dd297c72dfea");

        public static readonly Guid OutDoor = new Guid("d83de0fb-2b1f-4ba7-8434-d320bcc08431");
        public static readonly Guid OutGranted = new Guid("759148cb-fa27-462b-9c9f-e060036876bf");
        public static readonly Guid OutDenied = new Guid("2c4b89dd-b734-4da9-a699-25251031f417");
        public static readonly Guid OutUser = new Guid("87b8cae9-bf67-4f46-84da-b3977e2094a5");
        public static readonly Guid OutTime = new Guid("d63b1612-f0cb-427b-a38d-2784a734bb86");
        public static readonly Guid OutCause = new Guid("49721f54-a20c-4ad3-847a-3187f2e4a260");
        public static readonly Guid OutLocked = new Guid("59f38f77-b14e-43f3-b45e-aa96408d58c4");

        public static readonly Guid ParamGroups = new Guid("a038e5fe-8cc2-442f-998d-868200f9c66f");
        public static readonly Guid ParamAuthMode = new Guid("6960b672-a22b-48ac-8f72-dbfff3e7bc0a");
        public static readonly Guid ParamDoorOn = new Guid("d9c01d14-4845-4112-b221-3c0fed997db7");
        public static readonly Guid ParamMaxFailures = new Guid("137acec3-6523-47f5-98b0-3b21c9b775a9");
        public static readonly Guid ParamLockout = new Guid("e4bea97f-0bef-432b-b45c-b5c72e503729");
        public static readonly Guid ParamTwoFactorWindow = new Guid("c2370b13-4c41-424a-9797-31894071c9bf");

        public override string LogicName => "Surveillance.AccessController";
        public override Guid LogicGuid => new Guid("9b9158ed-a624-4296-a7d9-26796e7b66bc");
        public override Version LogicVersion => new Version(1, 0, 0, 0);

        private const string P = "SURVEILLANCE.ACCESS_CONTROLLER.";

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, $"{P}NAME", $"{P}DESCRIPTION", "surveillance.access_controller", "SURVEILLANCE.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(InCode, "C", $"{P}CODE.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, int.MaxValue, 1);
            factory.CreateLogicInterfaceTemplate(InTag, "Nfc", $"{P}TAG.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, int.MaxValue, 2);
            factory.CreateLogicInterfaceTemplate(InOpen, "O", $"{P}OPEN.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, int.MaxValue, 3);
            factory.CreateLogicInterfaceTemplate(InOff, "Off", $"{P}OFF.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, int.MaxValue, 4);

            factory.CreateLogicInterfaceTemplate(OutDoor, "Q", $"{P}DOOR.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1, RuleInterfaceType.Status);
            factory.CreateLogicInterfaceTemplate(OutGranted, "Ag", $"{P}GRANTED.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 2, RuleInterfaceType.Status);
            factory.CreateLogicInterfaceTemplate(OutDenied, "Ad", $"{P}DENIED.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 3, RuleInterfaceType.Status);
            factory.CreateLogicInterfaceTemplate(OutUser, "Un", $"{P}USER.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 4, RuleInterfaceType.Status);
            factory.CreateLogicInterfaceTemplate(OutTime, "Ta", $"{P}TIME.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 5, RuleInterfaceType.Status);
            factory.CreateLogicInterfaceTemplate(OutCause, "Ca", $"{P}CAUSE.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 6, RuleInterfaceType.Status);
            factory.CreateLogicInterfaceTemplate(OutLocked, "L", $"{P}LOCKED.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 7, RuleInterfaceType.Status);

            var sort = 1;
            void Param(Guid id, string key, RuleInterfaceParameterDataType type, object value) =>
                factory.CreateParameterLogicInterfaceTemplate(id, P + key + ".NAME", P + key + ".DESCRIPTION", LogicGuid, sort++, type, value);

            Param(ParamGroups, "GROUPS", RuleInterfaceParameterDataType.Text, AccessControllerLogic.DefaultGroups);
            Param(ParamAuthMode, "AUTH_MODE", RuleInterfaceParameterDataType.Integer, AccessControllerLogic.AuthCodeOrTag);
            Param(ParamDoorOn, "DOOR_ON", RuleInterfaceParameterDataType.Integer, 3);
            Param(ParamMaxFailures, "MAX_FAILURES", RuleInterfaceParameterDataType.Integer, 5);
            Param(ParamLockout, "LOCKOUT", RuleInterfaceParameterDataType.Integer, 300);
            Param(ParamTwoFactorWindow, "TWO_FACTOR_WINDOW", RuleInterfaceParameterDataType.Integer, 30);

            factory.ChangeDefaultVisuTemplate(LogicGuid, VisuMobileObjectTemplateTypes.Security);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new AccessControllerLogic(context, FileSecurityStateStore.Default);
        }
    }
}
