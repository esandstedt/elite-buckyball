using System;
using System.Collections.Generic;
using System.Text;

namespace EliteBuckyball.Application
{
    public class FuelRange
    {
        public readonly float Min;
        public readonly float Max;

        public float Avg => (this.Min + this.Max) / 2; 

        public FuelRange(double min, double max)
        {
            this.Min = (float)min;
            this.Max = (float)max;
        }
    }
}
