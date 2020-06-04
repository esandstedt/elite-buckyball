using EliteBuckyball.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace EliteBuckyball.Application.Interfaces
{
    public interface IEdgeConstraint
    {
        bool ValidBefore(StarSystem from, StarSystem to);
        bool ValidAfter(IEdge edge);
    }
}
