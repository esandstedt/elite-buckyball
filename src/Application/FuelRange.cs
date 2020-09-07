using System;
using System.Collections.Generic;
using System.Text;

namespace EliteBuckyball.Application
{
    public struct FuelRange
    {
        public double Min { get; set; }

        public double Max { get; set; }

        public double Avg => (this.Min + this.Max) / 2;

        public FuelRange(double min, double max)
        {
            this.Min = min;
            this.Max = max;
        }
    }
}
