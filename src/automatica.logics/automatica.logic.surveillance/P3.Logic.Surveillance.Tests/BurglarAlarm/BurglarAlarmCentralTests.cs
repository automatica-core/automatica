using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Control.Base;
using Automatica.Core.Logic;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Surveillance.BurglarAlarm;
using P3.Logic.Surveillance.Security;
using P3.Logic.Surveillance.Tests.TestSupport;
using Xunit;
using F = P3.Logic.Surveillance.BurglarAlarm.BurglarAlarmCentralLogicFactory;
using BA = P3.Logic.Surveillance.BurglarAlarm.BurglarAlarmLogicFactory;

namespace P3.Logic.Surveillance.Tests.BurglarAlarm
{
    public class BurglarAlarmCentralTests : SecurityLogicTest<BurglarAlarmCentralLogicFactory, BurglarAlarmCentralLogic>
    {
        private readonly List<BurglarAlarmLogic> _alarms = new();

        protected override BurglarAlarmCentralLogic Create(ILogicContext context, ISecurityStateStore store) => new(context, store);

        private BurglarAlarmLogic AddAlarm(string name, int armingDelay = 60)
        {
            var factory = new BurglarAlarmLogicFactory();
            var templates = new LogicTemplateFactoryMock();
            factory.InitTemplates(templates);
            var instance = templates.CreateRuleInstanceFromTemplate(factory.LogicGuid);
            instance.Name = name;
            // the mock gives every instance the template id
            instance.ObjId = Guid.NewGuid();
            instance.RuleInterfaceInstance.Single(a => a.This2RuleInterfaceTemplate == BA.ParamArmingDelay).Value = armingDelay;
            var alarm = new BurglarAlarmLogic(new LogicContextMock(instance, templates, Recorder), Store);
            alarm.Start().GetAwaiter().GetResult();
            Controls.Add(alarm);
            _alarms.Add(alarm);
            return alarm;
        }

        private void Link(params BurglarAlarmLogic[] alarms) => SetParam(F.ParamAlarms, ControlsOf(alarms));

        private static void AlarmTick(BurglarAlarmLogic alarm) =>
            alarm.GetType().GetMethod("Tick", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(alarm, null);

        private void AdvanceAll(double seconds)
        {
            Advance(seconds);
            _alarms.ForEach(AlarmTick);
            Tick();
        }

        [Fact]
        public void Arm_ArmsAllLinkedAlarms()
        {
            var ground = AddAlarm("Ground floor");
            var upper = AddAlarm("Upper floor");
            var other = AddAlarm("Garage");
            Link(ground, upper);
            Start();
            _alarms.ForEach(AlarmTick);

            Pulse(F.InArm);

            Assert.True(ground.IsArmed);
            Assert.True(upper.IsArmed);
            Assert.False(other.IsArmed);
            Assert.Equal(2, LastNumber(F.OutArmedCount));
        }

        [Fact]
        public void ArmDelayed_UsesEachAlarmsOwnDelay()
        {
            var a = AddAlarm("A", 30);
            var b = AddAlarm("B", 90);
            Link(a, b);
            Start();
            _alarms.ForEach(AlarmTick);

            Pulse(F.InArmDelayed);
            AdvanceAll(31);
            Assert.True(a.IsArmed);
            Assert.False(b.IsArmed);
            Assert.Equal(1, LastNumber(F.OutArmedCount));
            AdvanceAll(60);
            Assert.True(b.IsArmed);
            Assert.Equal(2, LastNumber(F.OutArmedCount));
        }

        [Fact]
        public void Toggle_DisarmsAllWhenAnyIsArmed()
        {
            var a = AddAlarm("A");
            var b = AddAlarm("B");
            Link(a, b);
            Start();
            _alarms.ForEach(AlarmTick);

            a.CentralCommand(BurglarAlarmLogic.CmdArm, "test");
            Pulse(F.InToggle);
            Assert.False(a.IsArmed);
            Assert.False(b.IsArmed);

            Pulse(F.InToggle);
            Assert.True(a.IsArmed);
            Assert.True(b.IsArmed);
        }

        [Fact]
        public void Central_IsNotBlockedByDisablePeripheryOfTheAlarm()
        {
            var a = AddAlarm("A");
            Link(a);
            Start();
            a.ValueChanged(a.Context.RuleInstance.RuleInterfaceInstance.Single(x => x.This2RuleInterfaceTemplate == BA.InDisablePeriphery), new TestSource("Child lock"), true);

            Pulse(F.InArm);
            Assert.True(a.IsArmed);
        }

        [Fact]
        public void AcknowledgeAndOff_ReachAllAlarms()
        {
            var a = AddAlarm("A");
            Link(a);
            Start();
            Pulse(F.InArm);
            a.ValueChanged(a.Context.RuleInstance.RuleInterfaceInstance.Single(x => x.This2RuleInterfaceTemplate == BA.InWindow), new TestSource("Window"), true);
            Tick();
            Assert.True(a.IsAlarm);
            Assert.True(LastBool(F.OutAlarm));

            Pulse(F.InConfirm);
            Assert.False(a.IsAlarm);
            Assert.True(a.IsArmed);

            Pulse(F.InOff);
            Assert.False(a.IsArmed);
        }

        [Fact]
        public void Visu_ArmAll_And_Details()
        {
            var a = AddAlarm("A");
            var b = AddAlarm("B");
            Link(a, b);
            Start();

            Assert.Equal(SecurityCommandResult.Ok, Command(BurglarAlarmLogic.CmdArm, user: "anna"));
            Assert.True(a.IsArmed && b.IsArmed);
            var state = VisuState();
            Assert.Equal("armed", (string)state["status"]);
            Assert.Equal(2, state["details"]!.Count());
            Assert.Contains(a.CurrentState.Log, x => x.Event == "armed" && x.Detail == "anna");
        }

        [Fact]
        public void Visu_NoLinkedAlarms_NotPossible()
        {
            Start();
            Assert.Equal(SecurityCommandResult.NotPossible, Command(BurglarAlarmLogic.CmdArm));
        }

        public override void Dispose()
        {
            foreach (var alarm in _alarms)
            {
                alarm.Stop().GetAwaiter().GetResult();
            }
            base.Dispose();
        }
    }
}
