using EliteBuckyball.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Transactions;

namespace EliteBuckyball.Application
{
    public class JumpTime
    {
        private const double TIME_WITCHSPACE = 14;
        private const double TIME_FSD_CHARGE = 20;
        private const double TIME_FSD_COOLDOWN = 10;
        private const double TIME_NEUTRON_BOOST = 8; 
        private const double TIME_SYNTHESIS_BOOST = 20;
        private const double TIME_TRAVEL_ZERO = 10;
        private const double TIME_TRAVEL_MIN = 20;
        private const double TIME_REFUEL_TRAVEL = 14;
        private const double TIME_GALAXY_MAP = 8;

        public const double NeutronWithoutRefuel = TIME_WITCHSPACE + TIME_TRAVEL_ZERO + TIME_NEUTRON_BOOST + TIME_FSD_CHARGE;
        public const double SynthesisWithoutRefuel = TIME_WITCHSPACE + TIME_SYNTHESIS_BOOST + TIME_FSD_CHARGE;
        public const double NormalWithoutRefuel = TIME_WITCHSPACE + TIME_FSD_COOLDOWN + TIME_FSD_CHARGE;

        private readonly Ship ship;

        public JumpTime(Ship ship)
        {
            this.ship = ship;
        }

        public double Get(StarSystem from, StarSystem to, BoostType boost, double? refuel)
        {
            var distanceToNeutron = from?.DistanceToNeutron ?? 0;
            var distanceToScoopable = from?.DistanceToScoopable ?? 0;

            if (!refuel.HasValue)
            {
                switch (boost)
                {
                    case BoostType.None:
                    {
                        return NormalWithoutRefuel;
                    }
                    case BoostType.Neutron:
                    if (distanceToNeutron == 0)
                    {
                        return NeutronWithoutRefuel;
                    }
                    else
                    {
                        break;
                    }
                    case BoostType.Synthesis:
                    {
                        return SynthesisWithoutRefuel;
                    }
                    default:
                    {
                        break;
                    }
                }
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

            if (refuel.HasValue)
            {
                var timeRefuelScoop = refuel.Value / this.ship.FuelScoopRate;
                var timeRefuelParallel = TIME_GALAXY_MAP + TIME_REFUEL_TRAVEL + TIME_FSD_CHARGE;

                var timeRefuelFinal = this.GetSupercruiseTime(distanceToScoopable) + timeRefuelScoop;
                if (timeRefuelScoop < timeRefuelParallel)
                {
                    timeRefuelFinal += TIME_FSD_CHARGE;
                }

                timeRst = Math.Max(timeRst, timeRefuelFinal);
            }

            return timeFst + timeRst;
        }

        private double GetSupercruiseTime(double distance)
        {
            if (distance < 1)
            {
                return TIME_TRAVEL_ZERO;
            }

            return Math.Max(
                TIME_TRAVEL_MIN,
                12 * Math.Log(distance)
            );
        }
    }

    public enum BoostType
    {
        None,
        Synthesis,
        Neutron
    }
}
