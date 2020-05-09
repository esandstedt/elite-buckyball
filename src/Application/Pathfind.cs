using EliteBuckyball.Application.Interfaces;
using EliteBuckyball.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EliteBuckyball.Application
{
    public class Pathfind
    {

        private readonly INodeHandler nodeHandler;
        private readonly INode start;
        private readonly INode goal;

        private Dictionary<string, double> g;
        private Dictionary<string, double> f;
        private Dictionary<string, string> cameFrom;
        private PriorityQueue<INode> open;

        public Pathfind(
            INodeHandler nodeHandler,
            StarSystem start,
            StarSystem goal)
        {
            this.nodeHandler = nodeHandler;
            this.start = nodeHandler.Create(start);
            this.goal = nodeHandler.Create(goal);

            this.g = new Dictionary<string, double>();
            this.f = new Dictionary<string, double>();
            this.cameFrom = new Dictionary<string, string>();
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

                Console.WriteLine("{0,8} {1,8} {2,8} {3,6} {4,6} {5,6}   {6}",
                    i,
                    this.open.Count,
                    this.cameFrom.Count,
                    (int)this.f[current.Id],
                    (int)this.g[current.Id],
                    (int)nodeHandler.Distance(current, this.goal),
                    current.Id
                );

                if (current.StarSystem.Id == goal.StarSystem.Id)
                {
                    return this.GenerateRoute();
                }

                var neighbors = await this.nodeHandler.Neighbors(current);
                foreach (var neighbor in neighbors)
                {
                    var g = this.g[current.Id] + this.nodeHandler.Distance(current, neighbor);

                    if (g < this.g.GetValueOrDefault(neighbor.Id, double.MaxValue))
                    {
                        this.cameFrom[neighbor.StarSystem.Name] = current.StarSystem.Name;
                        this.Enqueue(neighbor, g);
                    }
                }
            }

            return new List<string>();
        }

        private void Enqueue(INode node, double g)
        {
            this.g[node.Id] = g;
            var f = g + this.nodeHandler.ShortestDistance(node, this.goal);
            this.f[node.Id] = f;
            this.open.Enqueue(node, f);
        }

        private List<string> GenerateRoute()
        {
            var result = new List<string>();

            var current = this.goal.StarSystem.Name;
            while (this.cameFrom.ContainsKey(current))
            {
                result.Insert(0, current);
                current = this.cameFrom[current];
            }

            result.Insert(0, current);

            return result;
        }

    }
}
