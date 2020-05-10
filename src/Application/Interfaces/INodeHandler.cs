using EliteBuckyball.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EliteBuckyball.Application.Interfaces
{
    public interface INodeHandler
    {
        INode Create(StarSystem system);

        double GetDistance(INode a, INode b);

        double GetShortestDistance(INode a, INode b);

        Task<List<IEdge>> GetEdges(INode node);
    }
}
