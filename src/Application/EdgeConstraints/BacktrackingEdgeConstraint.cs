using EliteBuckyball.Application.Interfaces;
using EliteBuckyball.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace EliteBuckyball.Application.EdgeConstraints
{
    public class BacktrackingEdgeConstraint : BaseEdgeConstraint
    {

        private Vector3 goal;

        public BacktrackingEdgeConstraint(StarSystem goal)
        {
            this.goal = goal.Coordinates;
        }

        public override bool ValidBefore(StarSystem from, StarSystem to)
        {
            return Vector3.DistanceSquared(to.Coordinates, goal) < Vector3.DistanceSquared(from.Coordinates, goal);
        }

    }
}
