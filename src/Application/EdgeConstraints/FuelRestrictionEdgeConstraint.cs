using EliteBuckyball.Application.Interfaces;
using EliteBuckyball.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace EliteBuckyball.Application.EdgeConstraints
{
    public class FuelRestrictionEdgeConstraint : BaseEdgeConstraint
    {
        private readonly StarSystem system;
        private readonly double? min;
        private readonly double? max;

        public FuelRestrictionEdgeConstraint(
            StarSystem system,
            double? min,
            double? max)
        {
            this.system = system;
            this.min = min;
            this.max = max;
        }

        public override bool ValidAfter(IEdge edge)
        {
            return this.ValidAfter((Node)edge.From) && this.ValidAfter((Node)edge.To);
        }

        private bool ValidAfter(Node node)
        {
            if (node.StarSystem.Id == this.system.Id)
            {
                if (this.min.HasValue && node.Fuel.Min < this.min.Value)
                {
                    return false;
                }

                if (this.max.HasValue && this.max.Value < node.Fuel.Max)
                {
                    return false;
                }
            }

            return true;
        }

    }
}
