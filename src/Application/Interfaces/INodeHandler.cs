using EliteBuckyball.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EliteBuckyball.Application.Interfaces
{
    public interface INodeHandler
    {
        IEnumerable<INode> GetInitialNodes();
        double GetShortestDistanceToGoal(INode node);
        IEnumerable<IEdge> GetEdges(INode node);
    }
}
