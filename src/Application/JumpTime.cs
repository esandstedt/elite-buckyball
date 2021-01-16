using EliteBuckyball.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace EliteBuckyball.Application
{
    public class JumpTime
    {
        private const double TIME_WITCHSPACE = 14;
        private const double TIME_FSD_CHARGE = 20;
        private const double TIME_FSD_COOLDOWN = 10;
        private const double TIME_NEUTRON_BOOST = 7 + 1.8; // charge + repair
        private const double TIME_WHITE_DWARF_BOOST = 15 + 1.8; // charge + repair
        private const double TIME_SYNTHESIS_BOOST = 20;
        private const double TIME_TRAVEL_ZERO = 10;
        //private const double TIME_TRAVEL_MIN = 20;
        private const double TIME_REFUEL_TRAVEL = 14;
        private const double TIME_GALAXY_MAP = 8;
        private const double TIME_VISIT_STATION = 110;

        public const double NeutronWithoutRefuel = TIME_WITCHSPACE + TIME_TRAVEL_ZERO + TIME_NEUTRON_BOOST + TIME_FSD_CHARGE;
        private const double WhiteDwarfWithoutRefuel = TIME_WITCHSPACE + TIME_TRAVEL_ZERO + TIME_WHITE_DWARF_BOOST + TIME_FSD_CHARGE;
        public const double SynthesisWithoutRefuel = TIME_WITCHSPACE + TIME_SYNTHESIS_BOOST + TIME_FSD_CHARGE;
        private const double NormalWithoutRefuel = TIME_WITCHSPACE + TIME_FSD_COOLDOWN + TIME_FSD_CHARGE;

        private readonly Ship ship;

        public JumpTime(Ship ship)
        {
            this.ship = ship;
        }

        public double? Get(StarSystem from, BoostType boost, RefuelType refuelType, double? refuelLevel)
        {
            var distanceToNeutron = from?.DistanceToNeutron ?? 0;
            var distanceToScoopable = from?.DistanceToScoopable ?? 0;
            var distanceToStation = from?.DistanceToStation ?? 0;
            var distanceToWhiteDwarf = from?.DistanceToWhiteDwarf ?? 0;

            if (refuelType == RefuelType.None)
            {
                return GetWithoutRefuel(from, boost);
            }

            // Block A/B refueling
            if (refuelType != RefuelType.None && (boost == BoostType.Neutron || boost == BoostType.WhiteDwarf))
            {
                return null;
            }

            var timeFst = TIME_WITCHSPACE;
            var timeRst = 0.0;

            if (boost == BoostType.None)
            {
                timeRst = TIME_FSD_COOLDOWN + TIME_FSD_CHARGE;
            }
            else if (boost == BoostType.Synthesis)
            {
                timeRst = TIME_SYNTHESIS_BOOST + TIME_FSD_CHARGE;
            }
            else if (boost == BoostType.Neutron)
            {
                timeFst += this.GetSupercruiseTime(distanceToNeutron);
                timeFst += TIME_NEUTRON_BOOST;

                timeRst = TIME_FSD_CHARGE;
            }
            else if (boost == BoostType.WhiteDwarf)
            {
                timeFst += this.GetSupercruiseTime(distanceToWhiteDwarf);
                timeFst += TIME_WHITE_DWARF_BOOST;

                timeRst = TIME_FSD_CHARGE;
            }
            else
            {
                throw new NotImplementedException();
            }

            if (refuelType == RefuelType.None)
            {
                // No added time
            }
            else if (refuelType == RefuelType.Scoop)
            {
                var timeRefuelScoop = refuelLevel.Value / this.ship.FuelScoopRate;

                var timeRefuel = this.GetSupercruiseTime(distanceToScoopable) +
                    timeRefuelScoop +
                    TIME_FSD_CHARGE;

                timeRst = Math.Max(timeRst, timeRefuel);
            }
            else if (refuelType == RefuelType.ScoopHeatsink)
            {
                var timeRefuelScoop = refuelLevel.Value / this.ship.FuelScoopRate;
                var timeRefuelParallel = TIME_GALAXY_MAP + TIME_REFUEL_TRAVEL + TIME_FSD_CHARGE;

                if (timeRefuelScoop < timeRefuelParallel)
                {
                    return null;
                }

                var timeRefuel = this.GetSupercruiseTime(distanceToScoopable) + timeRefuelScoop;

                timeRst = Math.Max(timeRst, timeRefuel);
            } 
            else if (refuelType == RefuelType.Station)
            {
                var timeRefuel = this.GetSupercruiseTime(distanceToStation) + TIME_VISIT_STATION;
                timeRst = Math.Max(timeRst, timeRefuel);
            }
            else
            {
                throw new NotImplementedException();
            }

            return timeFst + timeRst;
        }

        private double GetWithoutRefuel(StarSystem from, BoostType boost)
        {
            if (boost == BoostType.None)
            {
                return NormalWithoutRefuel;
            }
            else if (boost == BoostType.Synthesis)
            {
                return SynthesisWithoutRefuel;
            }
            else if (boost == BoostType.Neutron)
            {
                var distanceToNeutron = from?.DistanceToNeutron ?? 0;

                if (distanceToNeutron == 0)
                {
                    return NeutronWithoutRefuel;
                }
                else
                {
                    return TIME_WITCHSPACE + this.GetSupercruiseTime(distanceToNeutron) + TIME_NEUTRON_BOOST + TIME_FSD_CHARGE;
                }
            }
            else if (boost == BoostType.WhiteDwarf)
            {
                var distanceToWhiteDwarf = from?.DistanceToWhiteDwarf ?? 0;

                if (distanceToWhiteDwarf == 0)
                {
                    return WhiteDwarfWithoutRefuel;
                }
                else
                {
                    return TIME_WITCHSPACE + this.GetSupercruiseTime(distanceToWhiteDwarf) + TIME_NEUTRON_BOOST + TIME_FSD_CHARGE;
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private double GetSupercruiseTime(double distance)
        {
            if (distance < 1)
            {
                return TIME_TRAVEL_ZERO;
            }

            /*
            return Math.Max(
                TIME_TRAVEL_MIN,
                12 * Math.Log(distance)
            );
             */

            return 45 + 0.075 * distance;
        }
    }

    public enum BoostType
    {
        None,
        Synthesis,
        Neutron,
        WhiteDwarf
    }
}
