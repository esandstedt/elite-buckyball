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

        public double GetJumpRange(double? fuel = null)
        {
            fuel = fuel ?? this.FuelCapacity;
            return this.GetRange(fuel.Value, this.GetBoostedFuelMultiplier(fuel.Value));
        }

        public double GetFuelCost(double fuel, double distance)
        {
            var totalMass = this.DryMass + fuel;
            return this.GetBoostedFuelMultiplier(fuel) * Math.Pow(distance * totalMass / this.FSD.OptimisedMass, this.FSD.FuelPower);
        }

        private double GetBoostedFuelMultiplier(double fuel)
        {
            var baseRange = this.GetRange(fuel);
            return this.FSD.FuelMultiplier * Math.Pow(baseRange / (baseRange + this.GuardianBonus), this.FSD.FuelPower);
        }

        private double GetRange(double fuel, double? fuelMultiplier = null)
        {
            fuelMultiplier = fuelMultiplier ?? this.FSD.FuelMultiplier;

            var maxFuel = Math.Min(this.FSD.MaxFuelPerJump, fuel);
            var totalMass = this.DryMass + fuel;

            return (this.FSD.OptimisedMass / totalMass) * Math.Pow(maxFuel / fuelMultiplier.Value, 1 / this.FSD.FuelPower);
        }
    }
}
