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
        private const double TIME_NEUTRON_BOOST = 10;
        private const double TIME_SYNTHESIS_BOOST = 20;
        private const double TIME_TRAVEL_ZERO = 6;
        private const double TIME_TRAVEL_MIN = 15;
        private const double TIME_PARALLEL_MARGIN = 0;
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
            var fromDistanceToNeutron = from?.DistanceToNeutron ?? 0;
            var toDistanceToScoopable = to?.DistanceToScoopable ?? 0;

            if (!refuel.HasValue)
            {
                switch (boost)
                {
                    case BoostType.None:
                    {
                        return NormalWithoutRefuel;
                    }
                    case BoostType.Neutron:
                    if (fromDistanceToNeutron == 0)
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
                timeFst += this.GetSupercruiseTime(fromDistanceToNeutron);
                timeFst += TIME_NEUTRON_BOOST;

                timeRst = TIME_FSD_CHARGE;
            }

            if (refuel.HasValue)
            {
                var timeRefuel = refuel.Value / this.ship.FuelScoopRate;

                var timeRefuelFull = TIME_GALAXY_MAP +
                    this.GetSupercruiseTime(toDistanceToScoopable) +
                    timeRefuel;

                var timeForParallel = timeRst + TIME_PARALLEL_MARGIN;

                if (TIME_FSD_CHARGE < timeRefuel || timeRefuelFull < timeForParallel)
                {
                    timeRefuelFull += TIME_FSD_CHARGE;
                }

                timeRst = Math.Max(timeRst, timeRefuelFull);
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
