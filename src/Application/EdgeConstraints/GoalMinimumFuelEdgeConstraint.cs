using EliteBuckyball.Application.Interfaces;
using EliteBuckyball.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace EliteBuckyball.Application.EdgeConstraints
{
    public class GoalMinimumFuelEdgeConstraint : BaseEdgeConstraint
    {
        private readonly StarSystem goal;
        private readonly double minFuel;

        public GoalMinimumFuelEdgeConstraint(
            StarSystem goal,
            double minFuel)
        {
            this.goal = goal;
            this.minFuel = minFuel;
        }

        public override bool ValidAfter(IEdge edge)
        {
            var to = ((Node)edge.To);
            if (to.StarSystem != this.goal || this.minFuel < to.Fuel.Min)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
