﻿namespace Automatica.Core.Control.Base
{
    public interface IDimmer : ISwitch
    {
        public Guid DimmerOutputValueId { get;  }
        public Guid DimmerInputValueId { get; }
    }
}
