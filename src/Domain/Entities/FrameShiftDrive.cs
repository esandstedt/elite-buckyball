using System;
using System.Collections.Generic;
using System.Text;

namespace EliteBuckyball.Domain.Entities
{
    public class FrameShiftDrive
    {
        /*
         * 2: 2
         * 3: 2.15
         * 4: 2.3
         * 5: 2.45
         * 6: 2.6
         * 7: 2.75
         */
        public double FuelPower { get; set; }

        /*
         * A: 0.012
         * B: 0.010
         * C: 0.008
         * D: 0.010
         * E: 0.011
         */
        public double FuelMultiplier { get; set; }
     
        public double MaxFuelPerJump { get; set; }

        public double OptimisedMass { get; set; }
    }
}
