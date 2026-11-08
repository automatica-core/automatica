using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.Operations.Scenes
{
    /// <summary>
    /// Scene (Loxone "Scene"): a button for the visualization and an input that start a scene. The action list itself is an
    /// automation with the trigger "scene is activated" (type "scene"), so the block only signals the activation.
    /// </summary>
    public class SceneLogicFactory : LogicFactory
    {
        /// <summary>
        /// Key of the activation output. The automation trigger "scene" listens to the output with this key, keep both in sync
        /// (Automatica.Core.Automation SceneTrigger.ActivatedOutputKey).
        /// </summary>
        public const string ActivatedKey = "activated";

        public static readonly Guid RuleActivate = new Guid("5f0d7b2e-8f7c-4c43-9d55-2a8b1f0c6e11");
        public static readonly Guid RuleOff = new Guid("0b6c2f1d-3e4a-4f57-8a9b-7c1d2e3f4a52");
        public static readonly Guid RuleActivated = new Guid("a7e3c9b1-2d4f-4e6a-8b0c-1d2e3f4a5b63");
        public static readonly Guid RuleLocked = new Guid("c4d5e6f7-8091-4a2b-9c3d-4e5f6a7b8c74");

        public override string LogicName => "P3.Logic.Operations.Scene";
        public override Guid LogicGuid => new Guid("e2b8f4a6-1c3d-4e5f-9a7b-8c9d0e1f2a85");
        public override Version LogicVersion => new Version(1, 0, 0, 0);

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "OPERATIONS.SCENE.NAME", "OPERATIONS.SCENE.DESCRIPTION",
                "operations-scene", "OPERATIONS.NAME", 100, 100);

            // RuleInterfaceType.Input: the push button of the visualization writes here
            factory.CreateLogicInterfaceTemplate(RuleActivate, "Act", "OPERATIONS.SCENE.ACTIVATE.DESCRIPTION", "activate", LogicGuid,
                LogicInterfaceDirection.Input, 0, 1, RuleInterfaceType.Input);
            factory.CreateLogicInterfaceTemplate(RuleOff, "Off", "OPERATIONS.SCENE.OFF.DESCRIPTION", "off", LogicGuid,
                LogicInterfaceDirection.Input, 0, 2, RuleInterfaceType.Unknown);

            factory.CreateLogicInterfaceTemplate(RuleActivated, "Q", "OPERATIONS.SCENE.ACTIVATED.DESCRIPTION", ActivatedKey, LogicGuid,
                LogicInterfaceDirection.Output, 0, 1, RuleInterfaceType.Output);
            factory.CreateLogicInterfaceTemplate(RuleLocked, "L", "OPERATIONS.SCENE.LOCKED.DESCRIPTION", "locked", LogicGuid,
                LogicInterfaceDirection.Output, 0, 2, RuleInterfaceType.Status);

            factory.ChangeDefaultVisuTemplate(LogicGuid, VisuMobileObjectTemplateTypes.PushButton);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new Scene(context);
        }
    }
}
