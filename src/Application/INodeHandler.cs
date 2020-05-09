using EliteBuckyball.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EliteBuckyball.Application
{
    public interface INodeHandler
    {
        INode Create(StarSystem system);

        double Distance(INode a, INode b);

        double ShortestDistance(INode a, INode b);

        Task<List<INode>> Neighbors(INode node);
    }
}
