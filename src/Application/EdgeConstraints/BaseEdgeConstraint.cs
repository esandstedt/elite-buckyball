using EliteBuckyball.Application.Interfaces;
using EliteBuckyball.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace EliteBuckyball.Application.EdgeConstraints
{
    public class BaseEdgeConstraint : IEdgeConstraint
    {
        public virtual bool ValidAfter(IEdge edge)
        {
            return true;
        }

        public virtual bool ValidBefore(StarSystem from, StarSystem to)
        {
            return true;
        }
    }
}
