using System;
using System.Collections.Generic;
using System.Text;

namespace EliteBuckyball.Application
{
    public struct RefuelRange
    {

        public static readonly string TYPE_INITIAL = "Initial";
        public static readonly string TYPE_DEFAULT = "Default";
        public static readonly string TYPE_HEATSINK = "Heatsink";


        public readonly string Type;
        public readonly FuelRange Fuel;

        public RefuelRange(string type, FuelRange fuel)
        {
            this.Type = type;
            this.Fuel = fuel;
        }
    }
}
