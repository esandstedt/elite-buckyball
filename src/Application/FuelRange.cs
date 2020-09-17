using System;
using System.Collections.Generic;
using System.Text;

namespace EliteBuckyball.Application
{
    public class FuelRange
    {
        public readonly double Min;
        public readonly double Max;

        public double Avg => (this.Min + this.Max) / 2; 

        public FuelRange(double min, double max)
        {
            this.Min = min;
            this.Max = max;
        }
    }
}
