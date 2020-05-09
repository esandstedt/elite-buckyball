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
        private readonly Node goal;
        private readonly double jumpRange;

        public NodeHandler(IStarSystemRepository starSystemRepository, StarSystem goal, double jumpRange)
        {
            this.starSystemRepository = starSystemRepository;
            this.goal = new Node(goal);
            this.jumpRange = jumpRange;
        }

        public INode Create(StarSystem system)
        {
            return new Node(system);
        }

        public double Distance(INode a, INode b)
        {
            var dist = Math.Sqrt(
                Math.Pow(a.StarSystem.X - b.StarSystem.X, 2) +
                Math.Pow(a.StarSystem.Y - b.StarSystem.Y, 2) +
                Math.Pow(a.StarSystem.Z - b.StarSystem.Z, 2)
            );

            if (dist < 4 * this.jumpRange)
            {
                return 1;
            }
            else
            {
                return Math.Ceiling(dist / this.jumpRange) - 3;
            }
        }

        public double ShortestDistance(INode a, INode b) {

            var dist = Math.Sqrt(
                Math.Pow(a.StarSystem.X - b.StarSystem.X, 2) +
                Math.Pow(a.StarSystem.Y - b.StarSystem.Y, 2) +
                Math.Pow(a.StarSystem.Z - b.StarSystem.Z, 2)
            );

            return Math.Ceiling(dist / (4 * this.jumpRange));
        }

        public async Task<List<INode>> Neighbors(INode node)
        {
            var result = (await this.starSystemRepository.GetNeighborsAsync(node.StarSystem, 500))
                .Select(system => (INode) new Node(system))
                .ToList();

            if (Distance(node, this.goal) < 500)
            {
                result.Add(this.goal);
            }

            return result;
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
