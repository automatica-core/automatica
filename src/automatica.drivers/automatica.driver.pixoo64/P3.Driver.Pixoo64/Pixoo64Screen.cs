using System.Threading.Tasks;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using P3.Driver.Pixoo64.Screens;

namespace P3.Driver.Pixoo64
{
    internal abstract class Pixoo64Screen : DriverBase
    {
        public abstract Task SetScreenValue(object value, NodeInstance node);

        public virtual BaseScreen BaseScreen { get; protected set; }

        protected Pixoo64Screen(IDriverContext driverContext) : base(driverContext)
        {
        }
    }

    internal abstract class Pixoo64Screen<T> : Pixoo64Screen where T : BaseScreen
    {
        public PixooSharp.Pixoo64 Pixoo { get; }

        public T Screen { get; private set; }

        protected Pixoo64Screen(IDriverContext driverContext, PixooSharp.Pixoo64 pixoo) : base(driverContext)
        {
            Pixoo = pixoo;
        }

        protected abstract T CreateScreen();

        public sealed override bool Init()
        {
            Screen = CreateScreen();
            BaseScreen = Screen;

            var screenTime = GetPropertyValueInt("screen-time");

            Screen.ScreenTime = screenTime;

            Screen.Title = DriverContext.NodeInstance.Name;


            return base.Init();
        }

        

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return new Pixoo64AttributeNode(ctx, this);
        }
    }
}
