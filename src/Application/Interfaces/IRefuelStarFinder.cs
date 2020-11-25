using EliteBuckyball.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace EliteBuckyball.Application.Interfaces
{
    public interface IRefuelStarFinder
    {
        IEnumerable<Node> Invoke(List<Node> nodes);
        StarSystem GetCandidate(Node from, Node to);
    }
}
