using System;

namespace EliteBuckyball.Domain.Entities
{
    public class Ship
    {

        public string Name { get; set; }

        public float DryMass { get; set; }

        public float FuelCapacity { get; set; }

        public FrameShiftDrive FSD { get; set; }

        public float GuardianBonus { get; set; }

        public float FuelScoopRate { get; set; }

    }
}
