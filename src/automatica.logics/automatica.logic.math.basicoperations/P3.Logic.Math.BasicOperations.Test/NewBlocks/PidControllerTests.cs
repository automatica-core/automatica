using System;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Math.BasicOperations.Pid;
using Xunit;

namespace P3.Logic.Math.BasicOperations.Tests.NewBlocks
{
    public class PidControllerTests : LogicTest<PidControllerLogicFactory>
    {
        private PidControllerRule Rule => (PidControllerRule)Logic;

        private void Send(Guid template, object value) => Assert.Empty(Logic.ValueChanged(GetLogicInterfaceByTemplate(template), Dispatchable, value));

        private void Gains(double kp, double ki, double kd = 0, double cycle = 1)
        {
            Send(PidControllerLogicFactory.RuleParamKp, kp);
            Send(PidControllerLogicFactory.RuleParamKi, ki);
            Send(PidControllerLogicFactory.RuleParamKd, kd);
            Send(PidControllerLogicFactory.RuleParamCycle, cycle);
        }

        [Fact]
        public void WithoutActualOrSetpoint_NoOutput()
        {
            Assert.Null(Rule.Compute());
            Send(PidControllerLogicFactory.RuleActual, 20);
            Assert.Null(Rule.Compute());
        }

        [Fact]
        public void ProportionalOnly()
        {
            Gains(10, 0);
            Send(PidControllerLogicFactory.RuleSetpoint, 21);
            Send(PidControllerLogicFactory.RuleActual, 20);
            Assert.Equal(10, Rule.Compute());

            Send(PidControllerLogicFactory.RuleActual, 22);
            Assert.Equal(0, Rule.Compute());   // limited to min
        }

        [Fact]
        public void Integral_AccumulatesPerCycle()
        {
            Gains(0, 1, cycle: 2);
            Send(PidControllerLogicFactory.RuleSetpoint, 21);
            Send(PidControllerLogicFactory.RuleActual, 20);

            Assert.Equal(2, Rule.Compute());
            Assert.Equal(4, Rule.Compute());
            Assert.Equal(6, Rule.Compute());
        }

        [Fact]
        public void AntiWindup_IntegralStopsAtTheLimit()
        {
            Gains(0, 10);
            Send(PidControllerLogicFactory.RuleParamMax, 50);
            Send(PidControllerLogicFactory.RuleSetpoint, 30);
            Send(PidControllerLogicFactory.RuleActual, 20);

            // the first step would give 100 - the integral stops where the output reaches the limit
            Assert.Equal(50, Rule.Compute());
            Assert.Equal(5, Rule.Integral, 9);

            for (var i = 0; i < 20; i++)
            {
                Assert.Equal(50, Rule.Compute());
            }
            Assert.Equal(5, Rule.Integral, 9);

            // the output leaves the limit as soon as the error turns, not after unwinding a huge integral
            Send(PidControllerLogicFactory.RuleActual, 31);
            Assert.Equal(40, Rule.Compute().Value, 9);
        }

        [Fact]
        public void AntiWindup_ProportionalAlreadySaturated_IntegralDoesNotShrink()
        {
            Gains(100, 1);
            Send(PidControllerLogicFactory.RuleSetpoint, 30);
            Send(PidControllerLogicFactory.RuleActual, 20);

            Assert.Equal(100, Rule.Compute());
            Assert.Equal(0, Rule.Integral, 9);
        }

        [Fact]
        public void Derivative_OnMeasurement()
        {
            Gains(0, 0, kd: 5);
            Send(PidControllerLogicFactory.RuleParamMin, -100);
            Send(PidControllerLogicFactory.RuleSetpoint, 20);
            Send(PidControllerLogicFactory.RuleActual, 20);
            Assert.Equal(0, Rule.Compute());

            // the actual value falls by 2 in one second -> +10
            Send(PidControllerLogicFactory.RuleActual, 18);
            Assert.Equal(10, Rule.Compute());

            // a setpoint step alone does not kick the derivative
            Send(PidControllerLogicFactory.RuleSetpoint, 30);
            Assert.Equal(0, Rule.Compute());
        }

        [Fact]
        public void Disabled_SendsMinAndClearsIntegral()
        {
            Gains(0, 1);
            Send(PidControllerLogicFactory.RuleParamMin, 5);
            Send(PidControllerLogicFactory.RuleSetpoint, 21);
            Send(PidControllerLogicFactory.RuleActual, 20);
            Rule.Compute();
            Rule.Compute();

            Send(PidControllerLogicFactory.RuleEnable, false);
            Assert.Equal(5, Rule.Compute());
            Assert.Equal(0, Rule.Integral);

            Send(PidControllerLogicFactory.RuleEnable, true);
            Assert.Equal(5, Rule.Compute());   // integral 1 -> 1, limited to min 5
        }

        [Fact]
        public void NegativeKp_ReversesDirection()
        {
            Gains(-10, 0);
            Send(PidControllerLogicFactory.RuleSetpoint, 24);
            Send(PidControllerLogicFactory.RuleActual, 26);
            Assert.Equal(20, Rule.Compute());
        }
    }
}
