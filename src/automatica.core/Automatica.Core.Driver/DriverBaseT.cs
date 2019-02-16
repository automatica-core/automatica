using System.Collections.Generic;

namespace Automatica.Core.Driver
{
    /// <summary>
    /// Generic implementation for <see cref="IDriverNode"/>
    /// </summary>
    /// <typeparam name="T"><see cref="IDriverNode"/></typeparam>
    public abstract class DriverBaseT<T>: DriverBase where T : IDriverNode
    {
        protected  DriverBaseT(IDriverContext driverContext) : base(driverContext)
        {
            ChildrenT = new List<T>();
        }

        /// <summary>
        /// Generic list of Childrens
        /// </summary>
        public IList<T> ChildrenT { get; set; }

        protected override void ChildrenCreated(IDriverNode child)
        {
            if (child is T tNode)
            {
                ChildrenT.Add(tNode);
            }
        }
    }
}
