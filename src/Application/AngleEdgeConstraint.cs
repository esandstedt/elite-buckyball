using EliteBuckyball.Application.Interfaces;
using EliteBuckyball.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace EliteBuckyball.Application
{
    public class AngleEdgeConstraint : IEdgeConstraint
    {

        private readonly Vector3 goal;
        private readonly double maxAngle;

        public AngleEdgeConstraint(StarSystem goal, double maxAngle)
        {
            this.goal = goal.Coordinates;
            this.maxAngle = maxAngle;
        }

        public bool IsValid(StarSystem from, StarSystem to)
        {
            var v1 = Vector3.Normalize(this.goal - from.Coordinates);
            var v2 = Vector3.Normalize(to.Coordinates - from.Coordinates);

            var dot = Vector3.Dot(v1, v2);
            var radians = dot < 0 ? Math.PI - Math.Acos(-dot) : Math.Acos(dot);
            var angle = 180 * radians / Math.PI;

            return angle < this.maxAngle;
        }
    }
}
