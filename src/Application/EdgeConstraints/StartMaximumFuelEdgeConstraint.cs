using EliteBuckyball.Application.Interfaces;
using EliteBuckyball.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace EliteBuckyball.Application.EdgeConstraints
{
    public class StartMaximumFuelEdgeConstraint : BaseEdgeConstraint
    {
        private readonly StarSystem start;
        private readonly double maxFuel;

        public StartMaximumFuelEdgeConstraint(StarSystem start, double maxFuel)
        {
            this.start = start;
            this.maxFuel = maxFuel;
        }

        public override bool ValidAfter(IEdge edge)
        {
            var from = ((Node)edge.From);
            return from.StarSystem != this.start || from.Fuel.Max < this.maxFuel;
        }
    }
}
