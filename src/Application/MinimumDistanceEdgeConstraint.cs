using EliteBuckyball.Application.Interfaces;
using EliteBuckyball.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace EliteBuckyball.Application
{
    public class MinimumDistanceEdgeConstraint : IEdgeConstraint
    {

        private readonly double distanceSquared;

        public MinimumDistanceEdgeConstraint(double distance)
        {
            this.distanceSquared = distance * distance;
        }

        public bool IsValid(StarSystem from, StarSystem to)
        {
            // Only applies for neutron jumps.
            if (!from.HasNeutron)
            {
                return true;
            }

            return this.distanceSquared < Vector3.DistanceSquared(from.Coordinates, to.Coordinates);
        }

    }
}
