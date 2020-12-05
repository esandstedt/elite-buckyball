using System;

namespace EliteBuckyball.Domain.Entities
{
    public class Ship
    {

        public string Name { get; set; }

        public double DryMass { get; set; }

        public double FuelCapacity { get; set; }

        public FrameShiftDrive FSD { get; set; }

        public double GuardianBonus { get; set; }

        public double FuelScoopRate { get; set; }

    }
}
