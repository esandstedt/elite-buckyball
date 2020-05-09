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

        public NodeHandler(IStarSystemRepository starSystemRepository)
        {
            this.starSystemRepository = starSystemRepository;
        }

        public INode Create(StarSystem system)
        {
            return new Node(system);
        }

        public double Distance(INode a, INode b)
        {
            return Math.Sqrt(
                Math.Pow(a.StarSystem.X - b.StarSystem.X, 2) + 
                Math.Pow(a.StarSystem.Y - b.StarSystem.Y, 2) + 
                Math.Pow(a.StarSystem.Z - b.StarSystem.Z, 2)
            ); 
        }

        public double ShortestDistance(INode a, INode b) => Distance(a, b);

        public async Task<List<INode>> Neighbors(INode node)
        {
            return (await this.starSystemRepository.GetNeighborsAsync(node.StarSystem, 500))
                .Select(system => (INode) new Node(system))
                .ToList();
        }

        private class Node : INode
        {

            public string Id => StarSystem.Name;

            public StarSystem StarSystem { get; }

            public Node(StarSystem system)
            {
                StarSystem = system;
            }

        }
    }
}
