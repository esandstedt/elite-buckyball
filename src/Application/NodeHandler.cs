using EliteBuckyball.Application.Interfaces;
using EliteBuckyball.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EliteBuckyball.Application
{
    public class NodeHandler : INodeHandler
    {

        private readonly IStarSystemRepository starSystemRepository;
        private readonly Ship ship;
        private readonly Node goal;
        private readonly double jumpRange;

        public NodeHandler(IStarSystemRepository starSystemRepository, Ship ship, StarSystem goal)
        {
            this.starSystemRepository = starSystemRepository;
            this.ship = ship;
            this.goal = new Node(goal);

            this.jumpRange = ship.GetJumpRange();
        }

        public INode Create(StarSystem system)
        {
            return new Node(system);
        }

        public double GetDistance(INode a, INode b)
        {
            var dist = ((Vector)a.StarSystem).Distance((Vector)b.StarSystem);

            if (dist < 4 * this.jumpRange)
            {
                return 1;
            }
            else
            {
                return Math.Ceiling(dist / this.jumpRange) - 3;
            }
        }

        public double GetShortestDistance(INode a, INode b)
        {
            var dist = ((Vector)a.StarSystem).Distance((Vector)b.StarSystem);

            return Math.Ceiling(dist / (4 * this.jumpRange));
        }

        public async Task<List<IEdge>> GetEdges(INode node)
        {
            var result = (await this.starSystemRepository.GetNeighborsAsync(node.StarSystem, 500))
                .Select(system => this.CreateEdge(node, new Node(system)))
                .ToList();

            if (GetDistance(node, this.goal) < 500)
            {
                result.Add(this.CreateEdge(node, this.goal));
            }

            return result;
        }

        private IEdge CreateEdge(INode from, INode to)
        {
            return new Edge
            {
                From = from,
                To = to,
                Distance = this.GetDistance(from, to)
            };
        }

        private class Edge : IEdge
        {

            public INode From { get; set; }

            public INode To { get; set; }

            public double Distance { get; set; }

        }

        private class Node : INode
        {

            public string Id => StarSystem.Name;

            public StarSystem StarSystem { get; }

            public Node(StarSystem system)
            {
                StarSystem = system;
            }

            public override int GetHashCode()
            {
                return this.Id.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as Node);
            }

            public bool Equals(Node that)
            {
                return that != null && this.Id == that.Id;
            }

            public override string ToString()
            {
                return this.StarSystem.Name;
            }

        }
    }
}
