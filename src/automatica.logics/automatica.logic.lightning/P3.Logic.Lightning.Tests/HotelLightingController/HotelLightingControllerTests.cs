using P3.Logic.Lightning.Common;
using P3.Logic.Lightning.HotelLightingController;
using P3.Logic.Lightning.Tests.TestSupport;
using Xunit;
using H = P3.Logic.Lightning.HotelLightingController.HotelLightingControllerLogicFactory;

namespace P3.Logic.Lightning.Tests.HotelLightingController
{
    public class HotelLightingControllerTests : LightingLogicTest<HotelLightingControllerLogicFactory>
    {
        private HotelLightingControllerLogic Hotel => (HotelLightingControllerLogic)Sut;

        private void Configure()
        {
            Param(H.Ids.Moods, "Welcome:1=60");
            Param(H.Ids.MotionMood, 1);
            Param(H.LeaveTimeout, 60.0);
            Param(H.ServiceDuration, 600.0);
            Start();
        }

        [Fact]
        public void Start_VacantAndCleaned()
        {
            Configure();
            Assert.Equal(3, Last(H.RoomStatus));
            Assert.Equal(false, Last(H.Presence));
        }

        [Fact]
        public void CardIn_WelcomeMood_Occupied_NotCleaned_CardOut_OffAfterTheTimeout()
        {
            Configure();

            Input(H.CardSwitch, true);
            Assert.Equal(1, Last(H.Ids.ActiveMood));
            Assert.Equal(60, Last(H.Ids.CircuitOutputs[0]));
            Assert.Equal(true, Last(H.Presence));
            Assert.Equal(2, Last(H.RoomStatus));

            Input(H.CardSwitch, false);
            Assert.Equal(1, Last(H.RoomStatus));
            AdvanceSeconds(59);
            Assert.Equal(1, Last(H.Ids.ActiveMood));
            AdvanceSeconds(1);
            Assert.Equal(Mood.AllOff, Last(H.Ids.ActiveMood));
        }

        [Fact]
        public void CardBackInTime_KeepsTheLights()
        {
            Configure();
            Input(H.CardSwitch, true);
            Input(H.CardSwitch, false);
            AdvanceSeconds(30);
            Input(H.CardSwitch, true);
            AdvanceSeconds(600);
            Assert.Equal(1, Last(H.Ids.ActiveMood));
        }

        [Fact]
        public void Door_WithoutCard_WelcomeLightForTheTimeout()
        {
            Configure();
            Input(H.Door, true);
            Assert.Equal(1, Last(H.Ids.ActiveMood));
            AdvanceSeconds(60);
            Assert.Equal(Mood.AllOff, Last(H.Ids.ActiveMood));
        }

        [Fact]
        public void NotBooked_SwitchesOff_AndBlocksDoorAndMotion()
        {
            Configure();
            Input(H.Door, true);
            Input(H.Door, false);

            Input(H.RoomState, HotelLightingControllerLogic.NotBooked);
            Assert.Equal(Mood.AllOff, Last(H.Ids.ActiveMood));

            Input(H.Door, true);
            Input(H.Ids.Motion, true);
            Assert.Equal(Mood.AllOff, Last(H.Ids.ActiveMood));

            // the card still works (staff)
            Input(H.CardSwitch, true);
            Assert.Equal(1, Last(H.Ids.ActiveMood));
        }

        [Fact]
        public void Service_AllOn_ThenDone_Cleaned_PulseAndOff()
        {
            Configure();
            Input(H.CardSwitch, true);
            Input(H.CardSwitch, false);
            AdvanceSeconds(60);
            Assert.Equal(1, Last(H.RoomStatus));

            Pulse(H.ServiceButton);
            Assert.Equal(true, Last(H.Service));
            Assert.Equal(Mood.AllOn, Last(H.Ids.ActiveMood));

            Pulse(H.ServiceButton);
            Assert.Equal(false, Last(H.Service));
            Assert.Equal(new object[] { true, false }, Values(H.ServiceDone));
            Assert.Equal(3, Last(H.RoomStatus));
            Assert.Equal(Mood.AllOff, Last(H.Ids.ActiveMood));
        }

        [Fact]
        public void Service_EndsAfterTheDuration_StaffCardDoesNotDirtyTheRoom()
        {
            Configure();
            Pulse(H.ServiceButton);
            Input(H.CardSwitch, true);
            Input(H.CardSwitch, false);

            AdvanceSeconds(600);

            Assert.False(Hotel.InService);
            Assert.True(Hotel.Cleaned);
            Assert.Equal(3, Last(H.RoomStatus));
        }

        [Fact]
        public void CleanedAndRoomState_SurviveARestart()
        {
            Configure();
            Input(H.CardSwitch, true);
            Input(H.RoomState, HotelLightingControllerLogic.Booked);

            Restart();

            Assert.False(Hotel.Cleaned);
        }

        [Fact]
        public void LightingControllerFunctions_StillWork()
        {
            Configure();
            Pulse(H.Ids.MoodPlus);
            Assert.Equal(1, Last(H.Ids.ActiveMood));
            Input(H.Ids.Command, "set:1:25");
            Assert.Equal(25, Last(H.Ids.CircuitOutputs[0]));
        }
    }
}
