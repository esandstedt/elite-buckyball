using EliteBuckyball.Application.Interfaces;
using EliteBuckyball.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EliteBuckyball.Application
{
    public class Pathfinder
    {

        private readonly INodeHandler nodeHandler;
        private readonly INode start;
        private readonly INode goal;

        private Dictionary<INode, double> g;
        private Dictionary<INode, double> f;
        private Dictionary<INode, INode> cameFrom;
        private PriorityQueue<INode> open;

        public Pathfinder(
            INodeHandler nodeHandler,
            StarSystem start,
            StarSystem goal)
        {
            this.nodeHandler = nodeHandler;
            this.start = nodeHandler.Create(start);
            this.goal = nodeHandler.Create(goal);

            this.g = new Dictionary<INode, double>();
            this.f = new Dictionary<INode, double>();
            this.cameFrom = new Dictionary<INode, INode>();
            this.open = new PriorityQueue<INode>();
        }

        public async Task<List<string>> InvokeAsync()
        {
            this.Enqueue(this.start, 0);

            var i = 0;
            while (this.open.Any())
            {
                i += 1;

                var (current, f) = this.open.Dequeue();

                if (this.f[current] < f)
                {
                    continue;
                }

                Console.WriteLine("{0,8} {1,8} {2,8} {3,6} {4,6} {5,6}   {6}",
                    i,
                    this.open.Count,
                    this.cameFrom.Count,
                    (int)this.f[current],
                    (int)this.g[current],
                    (int)nodeHandler.Distance(current, this.goal),
                    current
                );

                if (current.Equals(goal))
                {
                    return this.GenerateRoute();
                }

                var neighbors = await this.nodeHandler.Neighbors(current);
                foreach (var neighbor in neighbors)
                {
                    this.HandleNeighbor(current, neighbor);
                }
            }

            return new List<string>();
        }

        private void HandleNeighbor(INode current, INode neighbor)
        {
            var g = this.g[current] + this.nodeHandler.Distance(current, neighbor);

            if (g < this.g.GetValueOrDefault(neighbor, double.MaxValue))
            {
                this.cameFrom[neighbor] = current;
                this.Enqueue(neighbor, g);
            }
        }

        private void Enqueue(INode node, double g)
        {
            this.g[node] = g;
            var f = g + this.nodeHandler.ShortestDistance(node, this.goal);
            this.f[node] = f;
            this.open.Enqueue(node, f);
        }

        private List<string> GenerateRoute()
        {
            var result = new List<string>();

            var current = this.goal;
            while (this.cameFrom.ContainsKey(current))
            {
                result.Insert(0, current.StarSystem.Name);
                current = this.cameFrom[current];
            }

            result.Insert(0, current.StarSystem.Name);

            return result;
        }

    }
}
