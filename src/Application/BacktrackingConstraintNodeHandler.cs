using EliteBuckyball.Application.Interfaces;
using EliteBuckyball.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EliteBuckyball.Application
{
    public class BacktrackingConstraintNodeHandler : INodeHandler
    {
        private readonly INodeHandler handler;
        private Vector goal; 

        public BacktrackingConstraintNodeHandler(
            INodeHandler handler,
            StarSystem goal)
        {
            this.handler = handler;
            this.goal = (Vector)goal;
        }

        public INode Create(StarSystem system)
        {
            return this.handler.Create(system);
        }

        public double Distance(INode a, INode b)
        {
            return this.handler.Distance(a, b);
        }

        public double ShortestDistance(INode a, INode b)
        {
            return this.handler.ShortestDistance(a, b);
        }

        public async Task<List<INode>> Neighbors(INode node)
        {
            var distance = ((Vector)node.StarSystem).Distance(this.goal);

            return (await this.handler.Neighbors(node))
                .Where(neighbor => ((Vector)neighbor.StarSystem).Distance(this.goal) <= distance)
                .ToList();
        }

    }
}
