using EliteBuckyball.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace EliteBuckyball.Application.EdgeConstraints
{
    public class MaximumJumpsEdgeConstraint : BaseEdgeConstraint
    {

        private readonly int max;

        public MaximumJumpsEdgeConstraint(int inclusiveMax)
        {
            this.max = inclusiveMax;
        }

        public override bool ValidAfter(IEdge baseEdge)
        {
            return ((Edge)baseEdge).Jumps <= this.max;
        }
    }
}
