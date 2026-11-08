using System;
using Automatica.Core.Base.IO;
using Automatica.Core.Logic;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using P3.Logic.Lightning.Common;
using P3.Logic.Lightning.LightingController;
using F = P3.Logic.Lightning.HotelLightingController.HotelLightingControllerLogicFactory;

namespace P3.Logic.Lightning.HotelLightingController
{
    /// <summary>
    /// <list type="bullet">
    /// <item>key card in: presence, the welcome (motion) mood when everything is off, the room is no longer clean;
    /// card out: everything off after the leave timeout (0 = never)</item>
    /// <item>room state 0 (not booked) switches off and blocks motion and the door light; 1 booked, 2 checked in</item>
    /// <item>door opened without card while dark: the welcome mood for the leave timeout (until the card is inserted)</item>
    /// <item>service button: service mode on (all on when dark) until pressed again or the service duration ends; at the end the
    /// room is clean, "service done" pulses and the lights go off when no card is inserted</item>
    /// <item>room status: 1 vacant / not cleaned, 2 occupied / not cleaned, 3 vacant / cleaned, 4 occupied / cleaned</item>
    /// </list>
    /// </summary>
    public class HotelLightingControllerLogic : LightingControllerLogic
    {
        public const int NotBooked = 0;
        public const int Booked = 1;
        public const int CheckedIn = 2;

        private double _leaveTimeout = F.DefaultLeaveTimeout;
        private double _serviceDuration = F.DefaultServiceDuration;

        private bool _card;
        private bool _door;
        private bool _serviceButton;
        private bool _service;
        private bool _cleaned = true;
        private int _roomState = CheckedIn;
        private DateTime? _leaveAt;
        private DateTime? _serviceEndAt;
        private RemanentStore _hotelStore;

        public HotelLightingControllerLogic(ILogicContext context) : base(context, F.Ids)
        {
        }

        internal bool InService => _service;
        internal bool Cleaned => _cleaned;
        internal DateTime? LeaveAt => _leaveAt;

        internal int Status => (_card ? 2 : 1) + (_cleaned ? 2 : 0);

        protected override void OnStart()
        {
            base.OnStart();
            _hotelStore = new RemanentStore(Context.Dispatcher, Context.RuleInstance.ObjId, "hotel-lighting-controller");
            Restore(_hotelStore.Load());
            PublishHotel();
        }

        protected override void ApplyParameter(Guid template, object value)
        {
            if (template == F.LeaveTimeout)
            {
                _leaveTimeout = Math.Max(LightingValues.ToDouble(value, F.DefaultLeaveTimeout), 0);
            }
            else if (template == F.ServiceDuration)
            {
                _serviceDuration = Math.Max(LightingValues.ToDouble(value, F.DefaultServiceDuration), 1);
            }
            else
            {
                base.ApplyParameter(template, value);
            }
        }

        protected override bool MotionMaySwitchOn() => _roomState != NotBooked && base.MotionMaySwitchOn();

        protected override void OnOtherInput(Guid template, object value, IDispatchable source, DateTime now)
        {
            if (template == F.CardSwitch)
            {
                var card = LightingValues.IsTruthy(value);
                if (card && !_card)
                {
                    _card = true;
                    _leaveAt = null;
                    if (!_service)
                    {
                        _cleaned = false;
                    }
                    if (!AnyLightOn)
                    {
                        ActivateMood(MotionMood, now, ChangeSource.Motion);
                    }
                }
                else if (!card && _card)
                {
                    _card = false;
                    _leaveAt = _leaveTimeout > 0 ? now.AddSeconds(_leaveTimeout) : null;
                }
                PublishHotel();
            }
            else if (template == F.RoomState)
            {
                var state = LightingValues.ToInt(value, -1);
                if (state < NotBooked || state > CheckedIn)
                {
                    return;
                }
                _roomState = state;
                if (state == NotBooked && !_card && !_service)
                {
                    _leaveAt = null;
                    ActivateMood(Mood.AllOff, now, ChangeSource.Motion);
                }
                PublishHotel();
            }
            else if (template == F.Door)
            {
                var open = LightingValues.IsTruthy(value);
                if (open && !_door && !_card && !_service && _roomState != NotBooked && !AnyLightOn)
                {
                    ActivateMood(MotionMood, now, ChangeSource.Motion);
                    _leaveAt = _leaveTimeout > 0 ? now.AddSeconds(_leaveTimeout) : null;
                }
                _door = open;
            }
            else if (template == F.ServiceButton)
            {
                var pressed = LightingValues.IsTruthy(value);
                if (pressed && !_serviceButton)
                {
                    if (_service)
                    {
                        EndService(now);
                    }
                    else
                    {
                        _service = true;
                        _serviceEndAt = now.AddSeconds(_serviceDuration);
                        _leaveAt = null;
                        if (!AnyLightOn)
                        {
                            ActivateMood(Mood.AllOn, now, ChangeSource.Motion);
                        }
                        PublishHotel();
                    }
                }
                _serviceButton = pressed;
            }
        }

        private void EndService(DateTime now)
        {
            _service = false;
            _serviceEndAt = null;
            _cleaned = true;
            var done = Interface(F.ServiceDone);
            Pulse(done, true);
            Pulse(done, false);
            if (!_card)
            {
                ActivateMood(Mood.AllOff, now, ChangeSource.Motion);
            }
            PublishHotel();
        }

        protected override DateTime? OnOtherTick(DateTime now)
        {
            if (_serviceEndAt.HasValue && now >= _serviceEndAt.Value)
            {
                EndService(now);
            }
            if (_leaveAt.HasValue && now >= _leaveAt.Value)
            {
                _leaveAt = null;
                if (!_card && !_service)
                {
                    ActivateMood(Mood.AllOff, now, ChangeSource.Motion);
                }
            }
            return Earliest(_leaveAt, _serviceEndAt);
        }

        private void PublishHotel()
        {
            Output(Interface(F.Presence), _card);
            Output(Interface(F.Service), _service);
            Output(Interface(F.RoomStatus), Status);
            _hotelStore?.Save(JsonConvert.SerializeObject(new HotelState { Cleaned = _cleaned, RoomState = _roomState }));
        }

        private void Restore(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }
            try
            {
                var state = JsonConvert.DeserializeObject<HotelState>(json);
                if (state != null)
                {
                    _cleaned = state.Cleaned;
                    _roomState = state.RoomState is >= NotBooked and <= CheckedIn ? state.RoomState : CheckedIn;
                }
            }
            catch (Exception e)
            {
                Context.Logger.LogWarning(e, $"Hotel lighting controller {Context.RuleInstance.Name}: could not restore the state");
            }
        }

        private sealed class HotelState
        {
            public bool Cleaned { get; set; }
            public int RoomState { get; set; }
        }
    }
}
