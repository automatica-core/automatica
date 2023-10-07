using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using Newtonsoft.Json.Linq;

namespace P3.Logic.Operations.Shutter
{
    public class ShutterLogic: Automatica.Core.Logic.Logic
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

        private bool _locked = false;
        private int _direction = 0;
        private double _position;
        private bool _moving = false;

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
          
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance,
            IDispatchable source, object value)
        {
            var ret = new List<ILogicOutputChanged>();
            if (instance.ObjId == _upInput.ObjId)
            {
                if (!_locked)
                {
                    if (!_moving || _direction != 1)
                    {
                        ret.Add(new LogicOutputChanged(_moveOutput, 0));
                        ret.Add(new LogicOutputChanged(_isMovingOutput, true));
                    }

                    _direction = 1;
                    _moving = true;
                }
            }
            else if (instance.ObjId == _downInput.ObjId)
            {
                if (!_locked)
                {
                    if (!_moving || _direction != 0)
                    {
                        ret.Add(new LogicOutputChanged(_moveOutput, 1));
                        ret.Add(new LogicOutputChanged(_isMovingOutput, true));
                    }

                    _moving = true;
                    _direction = 0;
                }
            }
            else if (instance.ObjId == _stopInput.ObjId)
            {
                if (_moving)
                {
                    var stopValue = _direction == 0 ? true : false;
                    ret.Add(new LogicOutputChanged(_stopOutput, stopValue));
                    ret.Add(new LogicOutputChanged(_isMovingOutput, false));
                }

                _moving = false;
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
            }
            else if (instance.ObjId == _absolutePositionInput.ObjId)
            {
                var dValue = Convert.ToDouble(value);

                if (Math.Ceiling(dValue) >= 100)
                {
                    _moving = false;
                    ret.Add(new LogicOutputChanged(_isMovingOutput, false));
                }
                else if (Math.Floor(dValue) <= 0)
                {
                    _moving = false;
                    ret.Add(new LogicOutputChanged(_isMovingOutput, false));
                }
                _position = dValue;
                ret.Add(new LogicOutputChanged(_absolutePositionOutput, dValue));
            }

            return ret;
        }

        public override object GetDataForVisu()
        {
            return base.GetDataForVisu();
        }
    }
}
