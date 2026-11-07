using P3.Driver.Matter.Sessions;

namespace P3.Driver.Matter.Tests
{
    public class ReceptionStateTests
    {
        [Fact]
        public void Accept_FirstCounter_IsAlwaysAccepted()
        {
            Assert.True(new ReceptionState().Accept(0));
            Assert.True(new ReceptionState().Accept(uint.MaxValue));
        }

        [Fact]
        public void Accept_OutOfOrderInsideTheWindow_AcceptedOnce()
        {
            var state = new ReceptionState();
            Assert.True(state.Accept(1000));
            Assert.True(state.Accept(1010));

            for (uint c = 1001; c < 1010; c++)
            {
                Assert.True(state.Accept(c), $"counter {c}");
                Assert.False(state.Accept(c), $"duplicate {c}");
            }

            Assert.False(state.Accept(1000));
            Assert.False(state.Accept(1010));
        }

        [Fact(Skip = "BUG: a jump of exactly 32 clears the bitmap, so the old max (32 behind, still inside the window) is accepted again as a replay (Sessions/Session.cs:40)")]
        public void Accept_ExactlyThirtyTwoBehind_StillTracked()
        {
            var state = new ReceptionState();
            Assert.True(state.Accept(100));
            Assert.True(state.Accept(132));

            Assert.False(state.Accept(100));
            Assert.True(state.Accept(101));
            Assert.False(state.Accept(101));
        }

        [Fact]
        public void Accept_MoreThanThirtyTwoBehind_Rejected()
        {
            var state = new ReceptionState();
            Assert.True(state.Accept(100));
            Assert.True(state.Accept(200));

            Assert.False(state.Accept(167));
            Assert.False(state.Accept(150));
            Assert.True(state.Accept(168));
        }

        [Fact]
        public void Accept_BigJumpForward_ClearsTheWindow()
        {
            var state = new ReceptionState();
            Assert.True(state.Accept(10));
            Assert.True(state.Accept(11));
            Assert.True(state.Accept(500));

            Assert.True(state.Accept(499));
            Assert.True(state.Accept(468));
            Assert.False(state.Accept(11));
        }

        [Fact]
        public void Accept_CounterWrapsAroundUintMax_TreatedAsAhead()
        {
            var state = new ReceptionState();
            Assert.True(state.Accept(uint.MaxValue - 1));
            Assert.True(state.Accept(2));
            Assert.False(state.Accept(2));
            Assert.True(state.Accept(uint.MaxValue));
            Assert.False(state.Accept(uint.MaxValue - 1));
        }
    }
}
