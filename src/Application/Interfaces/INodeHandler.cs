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

        double GetShortestDistance(INode a, StarSystem b);

        Task<List<IEdge>> GetEdges(INode node);
    }
}
