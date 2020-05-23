using EliteBuckyball.Application.Interfaces;
using EliteBuckyball.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace EliteBuckyball.Application
{
    public class BacktrackingConstraintNodeHandler : INodeHandler
    {
        private readonly INodeHandler handler;
        private Vector3 goal; 

        public BacktrackingConstraintNodeHandler(
            INodeHandler handler,
            StarSystem goal)
        {
            this.handler = handler;
            this.goal = goal.Coordinates;
        }

        public List<INode> GetInitialNodes()
        {
            return this.handler.GetInitialNodes();
        }

        public double GetShortestDistance(INode a, StarSystem b)
        {
            return this.handler.GetShortestDistance(a, b);
        }

        public async Task<List<IEdge>> GetEdges(INode node)
        {
            var distanceSquared = Vector3.DistanceSquared(node.StarSystem.Coordinates, this.goal);

            return (await this.handler.GetEdges(node))
                .Where(edge => Vector3.DistanceSquared(edge.To.StarSystem.Coordinates, this.goal) <= distanceSquared)
                .ToList();
        }
    }
}
