using EliteBuckyball.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EliteBuckyball.Application
{
    public class ShipHandler
    {

        public Ship Ship { get; }

        public double BestJumpRange { get; }

        private const double JUMPRANGE_CACHE_RESOLUTION = 0.001;

        private readonly double[] jumpRangeCache;
        
        public ShipHandler(Ship ship)
        {
            Ship = ship;
            BestJumpRange = this.GetRange(ship.FSD.MaxFuelPerJump);

            this.jumpRangeCache = Enumerable.Range(0, (int)(ship.FuelCapacity / JUMPRANGE_CACHE_RESOLUTION) + 1)
                .Select(x => this.GetRange(JUMPRANGE_CACHE_RESOLUTION * x))
                .ToArray();
        }

        public double GetJumpRange(double? fuel = null)
        {
            fuel = Math.Max(0, Math.Min(fuel ?? this.Ship.FuelCapacity, this.Ship.FuelCapacity));
            return this.jumpRangeCache[(int)(fuel / JUMPRANGE_CACHE_RESOLUTION)];
            //return this.GetRange(fuel.Value, this.GetBoostedFuelMultiplier(fuel.Value));
        }

        public double GetFuelCost(double fuel, double distance)
        {
            var totalMass = this.Ship.DryMass + fuel;
            return this.GetBoostedFuelMultiplier(fuel) * Math.Pow(distance * totalMass / this.Ship.FSD.OptimisedMass, this.Ship.FSD.FuelPower);
        }

        private double GetRange(double fuel) {
            return this.GetRangeDirect(fuel, this.GetBoostedFuelMultiplier(fuel));
        }

        private double GetBoostedFuelMultiplier(double fuel)
        {
            var baseRange = this.GetRangeDirect(fuel);
            return this.Ship.FSD.FuelMultiplier * Math.Pow(baseRange / (baseRange + this.Ship.GuardianBonus), this.Ship.FSD.FuelPower);
        }

        private double GetRangeDirect(double fuel, double? fuelMultiplier = null)
        {
            fuelMultiplier ??= this.Ship.FSD.FuelMultiplier;

            var maxFuel = Math.Min(this.Ship.FSD.MaxFuelPerJump, fuel);
            var totalMass = this.Ship.DryMass + fuel;

            return (this.Ship.FSD.OptimisedMass / totalMass) * Math.Pow(maxFuel / fuelMultiplier.Value, 1 / this.Ship.FSD.FuelPower);
        }

    }
}
