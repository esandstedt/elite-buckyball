using EliteBuckyball.Application.Interfaces;
using EliteBuckyball.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace EliteBuckyball.Application.EdgeConstraints
{
    public class MinimumDistanceEdgeConstraint : BaseEdgeConstraint
    {

        private readonly double distanceSquared;

        public MinimumDistanceEdgeConstraint(double distance)
        {
            this.distanceSquared = distance * distance;
        }

        public override bool ValidBefore(StarSystem from, StarSystem to)
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
