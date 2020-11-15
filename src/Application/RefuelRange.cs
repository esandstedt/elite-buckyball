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
        public readonly double? FuelMin;
        public readonly double? FuelMax;

        public RefuelRange(string type, double? fuelMin, double? fuelMax)
        {
            this.Type = (RefuelType)Enum.Parse(typeof(RefuelType), type, true);
            this.FuelMin = fuelMin;
            this.FuelMax = fuelMax;
        }
    }
}
