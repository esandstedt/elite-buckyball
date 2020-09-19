using System;
using System.Collections.Generic;
using System.Text;

namespace EliteBuckyball.Application
{
    public enum RefuelType
    {
        None,
        Initial,
        Default,
        Heatsink
    }

    public class RefuelRange
    {
        public readonly RefuelType Type;
        public readonly FuelRange Fuel;

        public RefuelRange(string type, FuelRange fuel)
        {
            this.Type = (RefuelType)Enum.Parse(typeof(RefuelType), type, true);
            this.Fuel = fuel;
        }
    }
}
