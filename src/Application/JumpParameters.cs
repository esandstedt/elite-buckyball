using System;
using System.Collections.Generic;
using System.Text;

namespace EliteBuckyball.Application
{
    public enum RefuelType
    {
        None,
        Initial,
        Scoop,
        ScoopHeatsink,
        Station
    }

    public class JumpParameters
    {
        public readonly RefuelType RefuelType;
        public readonly double? RefuelMin;
        public readonly double? RefuelMax;
        public readonly int JumpsMin;
        public readonly int JumpsMax;
        public readonly double MultiJumpRangeFactor;

        public JumpParameters(
            RefuelType refuelType,
            double? refuelMin = null,
            double? refuelMax = null,
            int? jumpsMin = null,
            int? jumpsMax = null,
            double? multiJumpRangeFactor = null)
        {
            this.RefuelType = refuelType;
            this.RefuelMin = refuelMin;
            this.RefuelMax = refuelMax;
            this.JumpsMin = jumpsMin ?? 0;
            this.JumpsMax = jumpsMax ?? int.MaxValue;
            this.MultiJumpRangeFactor = multiJumpRangeFactor ?? 1.0;
        }
    }
}
