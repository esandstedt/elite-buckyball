﻿using EliteBuckyball.Application.Interfaces;
using EliteBuckyball.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace EliteBuckyball.Application
{
    public class CylinderEdgeConstraint : IEdgeConstraint
    {

        private readonly Vector3 start;
        private readonly Vector3 goal;

        public CylinderEdgeConstraint(StarSystem start, StarSystem goal)
        {
            this.start = start.Coordinates;
            this.goal = goal.Coordinates;
        }

        public bool IsValid(StarSystem from, StarSystem to)
        {
            return this.DistanceSquaredFromCenterLine(to.Coordinates) < Math.Pow(2500, 2);
        }

        private float DistanceSquaredFromCenterLine(Vector3 x0)
        {
            // https://mathworld.wolfram.com/Point-LineDistance3-Dimensional.html

            var x1 = this.start;
            var x2 = this.goal;

            var x1m0 = x1 - x0;
            var x2m1 = x2 - x1;

            var t = -1 * Vector3.Dot(x1m0, x2m1) / (float)Math.Pow(x2m1.Length(), 2);
            if (t < 0)
            {
                return Vector3.DistanceSquared(x0, x1);
            }
            else if (t < 1)
            {
                var x3 = x1 + Vector3.Multiply(t, x2m1);
                return Vector3.DistanceSquared(x0, x3);
            }
            else
            {
                return Vector3.DistanceSquared(x0, x2);
            }
        }

    }
}
