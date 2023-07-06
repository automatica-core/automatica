using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using P3.Driver.Pixoo64.Screens;

namespace P3.Driver.Pixoo64
{
    internal abstract class Pixoo64Screen : DriverBase
    {
        protected readonly int TimeZoneOffset = 0;

        public async Task SetValue(object value, NodeInstance node)
        {
            await SetScreenValue(value, node);
        }


        protected abstract Task SetScreenValue(object value, NodeInstance node);

        public virtual BaseScreen BaseScreen { get; protected set; }

        protected Pixoo64Screen(IDriverContext driverContext) : base(driverContext)
        {
            var timeZoneOffset = driverContext.NodeTemplateFactory.GetSetting("timezoneOffset");

            try
            {
                if (timeZoneOffset != null)
                {
                    TimeZoneOffset = timeZoneOffset.ValueInt.Value;
                }
                else
                {
                    TimeZoneOffset = 0;
                }
            }
            catch
            {
                //ignore exception
            }
        }
    }

    internal abstract class Pixoo64Screen<T> : Pixoo64Screen where T : BaseScreen
    {
        public IList<PixooSharp.Pixoo64> Pixoo { get; }

        public T Screen { get; private set; }

        protected Pixoo64Screen(IDriverContext driverContext, IList<PixooSharp.Pixoo64> pixoo) : base(driverContext)
        {
            Pixoo = pixoo;
        }

        protected abstract T CreateScreen();

        public sealed override Task<bool> Init(CancellationToken token = default)
        {
            Screen = CreateScreen();
            BaseScreen = Screen;

            var screenTime = GetPropertyValueInt("screen-time");

            Screen.ScreenTime = screenTime;

            Screen.Title = DriverContext.NodeInstance.Name;
            Screen.DateTimeHourOffset = TimeZoneOffset;


            return base.Init(token);
        }

        

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return new Pixoo64AttributeNode(ctx, this);
        }
    }
}
