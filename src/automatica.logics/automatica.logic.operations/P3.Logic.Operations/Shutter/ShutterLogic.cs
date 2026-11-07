using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Control.Base;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Operations.Shutter
{
    public class ShutterLogic: Automatica.Core.Logic.Logic, ISlatBlind
    {

        private readonly RuleInterfaceInstance _moveInput;
        private readonly RuleInterfaceInstance _stopInput;
        private readonly RuleInterfaceInstance _directionInput;
        private readonly RuleInterfaceInstance _absolutePositionInput;
        private readonly RuleInterfaceInstance _upInput;
        private readonly RuleInterfaceInstance _downInput;

        private readonly RuleInterfaceInstance _lockedInput;


        private readonly RuleInterfaceInstance _directionOutput;
        private readonly RuleInterfaceInstance _moveOutput;
        private readonly RuleInterfaceInstance _stopOutput;
        private readonly RuleInterfaceInstance _absolutePositionOutput;

        private readonly RuleInterfaceInstance _isMovingOutput;

        // optional: rule instances created before slats were supported may not have them
        private readonly RuleInterfaceInstance _slatInput;
        private readonly RuleInterfaceInstance _slatOutput;
        private int _slatPosition;

        private bool _locked = false;
        private int _direction = 0;
        private int _position;
        private bool _moving = false;

        private int? _targetPosition;


        public bool IsMoving => _moving;
        public int Direction => _direction;
        public int Position => _position;
        public bool HasSlats => _slatOutput != null;
        public int SlatPosition => _slatPosition;
        public Guid Id => Context.RuleInstance.ObjId;
        public string Name => Context.RuleInstance.Name;

        public ShutterLogic(ILogicContext context) : base(context)
        {
            _moveInput = context.RuleInstance.RuleInterfaceInstance.Single(a =>
                a.This2RuleInterfaceTemplate == ShutterLogicFactory.RuleInputMove);
            _stopInput = context.RuleInstance.RuleInterfaceInstance.Single(a =>
                a.This2RuleInterfaceTemplate == ShutterLogicFactory.RuleInputStop);
            _directionInput = context.RuleInstance.RuleInterfaceInstance.Single(a =>
                a.This2RuleInterfaceTemplate == ShutterLogicFactory.RuleInputDirection);
            _absolutePositionInput = context.RuleInstance.RuleInterfaceInstance.Single(a =>
                a.This2RuleInterfaceTemplate == ShutterLogicFactory.RuleInputAbsolutePercentage);
            _upInput = context.RuleInstance.RuleInterfaceInstance.Single(a =>
                a.This2RuleInterfaceTemplate == ShutterLogicFactory.RuleInputUp);
            _downInput = context.RuleInstance.RuleInterfaceInstance.Single(a =>
                a.This2RuleInterfaceTemplate == ShutterLogicFactory.RuleInputDown);
            _lockedInput = context.RuleInstance.RuleInterfaceInstance.Single(a =>
                a.This2RuleInterfaceTemplate == ShutterLogicFactory.RuleInputLocked);

            _directionOutput = context.RuleInstance.RuleInterfaceInstance.Single(a =>
                a.This2RuleInterfaceTemplate == ShutterLogicFactory.RuleOutputDirection);
            _moveOutput = context.RuleInstance.RuleInterfaceInstance.Single(a =>
                a.This2RuleInterfaceTemplate == ShutterLogicFactory.RuleOutputShutterMove);
            _stopOutput = context.RuleInstance.RuleInterfaceInstance.Single(a =>
                a.This2RuleInterfaceTemplate == ShutterLogicFactory.RuleOutputStop);
            _absolutePositionOutput = context.RuleInstance.RuleInterfaceInstance.Single(a =>
                a.This2RuleInterfaceTemplate == ShutterLogicFactory.RuleOutputAbsolutePercentage);
            _isMovingOutput = context.RuleInstance.RuleInterfaceInstance.Single(a =>
                a.This2RuleInterfaceTemplate == ShutterLogicFactory.RuleOutputMoveState);
            _slatInput = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == ShutterLogicFactory.RuleInputSlatPercentage);
            _slatOutput = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == ShutterLogicFactory.RuleOutputSlatPercentage);
          
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance,
            IDispatchable source, object value)
        {
            var ret = new List<ILogicOutputChanged>();
            if (instance.ObjId == _upInput.ObjId)
            {
                if (!_locked)
                {
                    ret.AddRange(MoveUp());
                }
            }
            else if (instance.ObjId == _downInput.ObjId)
            {
                if (!_locked)
                {
                    ret.AddRange(MoveDown());
                }
            }
            else if (instance.ObjId == _stopInput.ObjId)
            {
                ret.AddRange(Stop());
            }
            else if (instance.ObjId == _lockedInput.ObjId)
            {
                _locked = Convert.ToBoolean(value);
            }
            else if (instance.ObjId == _directionInput.ObjId)
            {
                _direction = Convert.ToInt32(value);
                ret.Add(new LogicOutputChanged(_directionOutput, _direction));
            }
            else if (instance.ObjId == _moveInput.ObjId)
            {
                var moveValue = _direction == 0 ? true : false;
                ret.Add(new LogicOutputChanged(_moveOutput, moveValue));
                ret.Add(new LogicOutputChanged(_isMovingOutput, true));
                _moving = true;
            }
            else if (_slatInput != null && instance.ObjId == _slatInput.ObjId)
            {
                // slat feedback of the actuator
                _slatPosition = ClampPercent(Convert.ToDouble(value));
            }
            else if (instance.ObjId == _absolutePositionInput.ObjId)
            {
                // the position input is the actuator feedback: track it and finish a pending absolute move
                var dValue = Convert.ToDouble(value);
                ret.AddRange(MoveAbsolute(dValue, false));

                if (_targetPosition.HasValue && _targetPosition == Convert.ToInt32(dValue))
                {
                    _targetPosition = null;
                    _moving = false;
                    ret.Add(new LogicOutputChanged(_isMovingOutput, _moving));
                }
            }
            
            return ret;
        }

        private IList<ILogicOutputChanged> MoveAbsolute(double dValue, bool setOutput)
        {
            var ret = new List<ILogicOutputChanged>();

            var intValue = Convert.ToInt32(dValue);

            var moving = false;
            if (Math.Ceiling(dValue) >= 100)
            {
                _moving = false;
            }
            else if (Math.Floor(dValue) <= 0)
            {
                _moving = false;
            }
            else
            {
                moving = true;
            }
            
            
            _position = intValue;
            if (setOutput)
            {
                _targetPosition = intValue;
                _moving = moving;
                ret.Add(new LogicOutputChanged(_isMovingOutput, _moving));
                ret.Add(new LogicOutputChanged(_absolutePositionOutput, dValue));
            }

            return ret;
        }

        private IList<ILogicOutputChanged> MoveUp()
        {
            var ret = new List<ILogicOutputChanged>();
            if (!_moving)
            {
                ret.Add(new LogicOutputChanged(_moveOutput, 0));
                ret.Add(new LogicOutputChanged(_isMovingOutput, true));

                _moving = true;
            }
            else
            {
                ret.AddRange(Stop());
                _moving = false;
            }

            _direction = 1;
            return ret;
        }

        private IList<ILogicOutputChanged> MoveDown()
        {
            var ret = new List<ILogicOutputChanged>();
            if (!_moving)
            {
                ret.Add(new LogicOutputChanged(_moveOutput, 1));
                ret.Add(new LogicOutputChanged(_isMovingOutput, true));
                _moving = true;
            }
            else
            {
                ret.AddRange(Stop());
                _moving = false;
            }

            _direction = 0;
            return ret;
        }

        private IList<ILogicOutputChanged> Stop()
        {
            var ret = new List<ILogicOutputChanged>();
            if (_moving)
            {
                var stopValue = _direction == 0 ? true : false;
                ret.Add(new LogicOutputChanged(_stopOutput, stopValue));
                ret.Add(new LogicOutputChanged(_isMovingOutput, false));
            }

            _moving = false;
            return ret;
        }

        public Task StopAsync(CancellationToken token = new CancellationToken())
        {
            var values = Stop();
            return DispatchValues(values);
        }


        public Task MoveUpAsync(CancellationToken token = new CancellationToken())
        {
            var values = MoveUp();
            return DispatchValues(values);
        }

        public Task MoveDownAsync(CancellationToken token = new CancellationToken())
        {
            var values = MoveDown();
            return DispatchValues(values);
        }

        public Task MoveAbsoluteAsync(int pos, CancellationToken token = new CancellationToken())
        {
            var values = MoveAbsolute(pos, true);
            return DispatchValues(values);
        }

        public Task MoveSlatAsync(int pos, CancellationToken token = new CancellationToken())
        {
            if (_slatOutput == null)
            {
                return Task.CompletedTask;
            }

            _slatPosition = ClampPercent(pos);
            return DispatchValues(new List<ILogicOutputChanged> { new LogicOutputChanged(_slatOutput, (double)_slatPosition) });
        }

        private static int ClampPercent(double value)
        {
            return (int)Math.Round(Math.Clamp(value, 0, 100));
        }

        private async Task DispatchValues(IList<ILogicOutputChanged> list)
        {
            foreach (var l in list)
            {
                await Context.Dispatcher.DispatchValue(l.Instance,
                    new DispatchValue(l.Instance.Id, DispatchableType.RuleInstance, l.Value, DateTime.Now,
                        DispatchValueSource.Read));
            }
        }

    }
}
