using EliteBuckyball.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Transactions;

namespace EliteBuckyball.Application
{
    public class JumpTime
    {

        public double ShortestTime { get; private set; }

        private const double TIME_WITCHSPACE = 14;
        private const double TIME_FSD_CHARGE = 20;
        private const double TIME_FSD_COOLDOWN = 10;
        private const double TIME_NEUTRON_BOOST = 13;
        private const double TIME_SYNTHESIS_BOOST = 20;
        private const double TIME_TRAVEL_MIN = 5;
        private const double TIME_PARALLEL_MARGIN = 5;

        private readonly Ship ship;

        public JumpTime(Ship ship)
        {
            this.ship = ship;

            this.ShortestTime = TIME_WITCHSPACE + TIME_TRAVEL_MIN + TIME_NEUTRON_BOOST + TIME_FSD_CHARGE;
        }

        public double Get(StarSystem from, StarSystem to, BoostType boost, double? refuel)
        {
            var timeFst = TIME_WITCHSPACE;

            if (boost == BoostType.Neutron)
            {
                timeFst += this.GetSupercruiseTime(from?.DistanceToNeutron ?? 0);
                timeFst += TIME_NEUTRON_BOOST;
            }

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
                timeRst = TIME_FSD_CHARGE;
            }

            if (refuel.HasValue)
            {
                var timeRefuel = this.GetSupercruiseTime(to?.DistanceToScoopable ?? 0) +
                    refuel.Value / this.ship.FuelScoopRate;

                var timeForParallel = timeRst + TIME_PARALLEL_MARGIN;

                if (timeRefuel < timeForParallel)
                {
                    timeRefuel += TIME_FSD_CHARGE;
                }

                timeRst = Math.Max(timeRst, timeRefuel);
            }

            return timeFst + timeRst;

        }

        private double GetSupercruiseTime(double distance)
        {
            return Math.Max(
                TIME_TRAVEL_MIN,
                12 * Math.Log(Math.Max(1, distance))
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
