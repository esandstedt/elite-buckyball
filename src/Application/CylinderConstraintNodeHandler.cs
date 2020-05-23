using EliteBuckyball.Application.Interfaces;
using EliteBuckyball.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace EliteBuckyball.Application
{
    public class CylinderConstraintNodeHandler : INodeHandler
    {
        private readonly INodeHandler handler;
        private readonly Vector3 start;
        private readonly Vector3 goal;

        private readonly Dictionary<INode, bool> cache;

        public CylinderConstraintNodeHandler(
            INodeHandler handler,
            StarSystem start,
            StarSystem goal)
        {
            this.handler = handler;
            this.start = start.Coordinates;
            this.goal = goal.Coordinates;

            this.cache = new Dictionary<INode, bool>();
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
            return (await this.handler.GetEdges(node))
                .Where(edge => {
                    var node = edge.To;

                    if (!this.cache.ContainsKey(node))
                    {
                        this.cache[node] = this.DistanceSquaredFromCenterLine(node) < Math.Pow(2500, 2);
                    }

                    return this.cache[node];
                })
                .ToList();
        }

        private double DistanceSquaredFromCenterLine(INode node)
        {
            // https://mathworld.wolfram.com/Point-LineDistance3-Dimensional.html

            var x0 = node.StarSystem.Coordinates;
            var x1 = this.start;
            var x2 = this.goal;

            var x1m0 = x1 - x0;
            var x2m1 = x2 - x1;

            var t = -1 * Vector3.Dot(x1m0, x2m1) / (float)Math.Pow(x2m1.Length(), 2);
            if (t < 0)
            {
                return Vector3.DistanceSquared(x0, x1);
            }
            else if (t < 1)
            {
                var x3 = x1 + Vector3.Multiply(t, x2m1);
                return Vector3.DistanceSquared(x0, x3);
            }
            else
            {
                return Vector3.DistanceSquared(x0, x2);
            }
        }
    }
}
