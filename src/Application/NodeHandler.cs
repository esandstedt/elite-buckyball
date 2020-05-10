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
        private readonly List<double> refuelLevels;
        private readonly StarSystem start;
        private readonly StarSystem goal;

        private readonly Dictionary<int, double> jumpRangeCache;
        private readonly double bestJumpRange;

        public NodeHandler(IStarSystemRepository starSystemRepository, Ship ship, List<double> refuelLevels, StarSystem start, StarSystem goal)
        {
            this.starSystemRepository = starSystemRepository;
            this.ship = ship;
            this.refuelLevels = refuelLevels;
            this.start = start;
            this.goal = goal;

            this.jumpRangeCache = new Dictionary<int, double>();
            this.bestJumpRange = this.GetJumpRange(ship.FSD.MaxFuelPerJump);
        }

        public List<INode> GetInitialNodes()
        {
            return this.refuelLevels
                .Select(x => (INode)this.CreateNode(this.start, x))
                .ToList();
        }

        public INode Create(StarSystem system)
        {
            return this.CreateNode(system, this.ship.FuelCapacity);
        }

        private Node CreateNode(StarSystem system, double fuel)
        {
            return new Node(
                string.Format("{0}-{1}", system.Name, (int)(8 * fuel / this.ship.FuelCapacity)),
                system,
                fuel
            );
        }

        public double GetShortestDistance(INode a, StarSystem b)
        {
            var distance = this.GetDistance(a.StarSystem, b);

            return 50 * Math.Ceiling(distance / (4 * this.bestJumpRange));
        }

        public async Task<List<IEdge>> GetEdges(INode node)
        {
            var baseNode = (Node)node;

            var result = (await this.starSystemRepository.GetNeighborsAsync(node.StarSystem, 500))
                .SelectMany(system => new List<IEdge>()
                    .Concat(this.refuelLevels.Select(x => this.CreateEdge(baseNode, system, x)))
                    .Concat(new List<IEdge>
                    {
                        this.CreateEdge(baseNode, system, null)
                    })
                );

            if (GetDistance(node.StarSystem, this.goal) < 500)
            {
                result = result.Concat(new List<IEdge>
                {
                    this.CreateEdge(baseNode, this.goal, null)
                });
            }

            return result
                .Where(x => x != null)
                .ToList();
        }

        private IEdge CreateEdge(Node node, StarSystem system, double? refuel)
        {
            var from = (Vector)node.StarSystem;
            var to = (Vector)system;

            double fuel = node.Fuel;
            double time = 0;

            var distance = from.Distance(to);

            double fstJumpFactor;
            var fstJumpRange = this.GetJumpRange(fuel);
            if (node.StarSystem.HasNeutron && node.StarSystem.DistanceToNeutron < 100)
            {
                fstJumpFactor = 4;
                time += this.GetTravelTime(node.StarSystem.DistanceToNeutron);
            }
            else
            {
                fstJumpFactor = 2;
            }

            var rstJumpFactor = 2;
            var rstJumpRange = this.GetJumpRange(this.ship.FuelCapacity);
            var rstDistance = Math.Max(distance - (fstJumpFactor * fstJumpRange), 0);

            double jumps = 1 + Math.Ceiling(rstDistance / (rstJumpFactor * rstJumpRange));

            time += 50 * jumps;

            if (jumps < 1.5) // only one jump (floating point comparison)
            {
                fuel -= this.ship.GetFuelCost(fuel, distance / 4);

                // too low fuel
                if (fuel < 1)
                {
                    return null;
                }

                if (refuel.HasValue)
                {
                    // must be above current fuel
                    if (refuel.Value < fuel)
                    {
                        return null;
                    }

                    // must have scoopable 
                    if (!system.HasScoopable || 100 < system.DistanceToScoopable)
                    {
                        return null;
                    }

                    time += this.GetTravelTime(system.DistanceToScoopable);
                    time += (refuel.Value - fuel) / this.ship.FuelScoopRate;
                    time += 20;

                    fuel = refuel.Value;
                }
            }
            else
            {
                fuel -= this.ship.FSD.MaxFuelPerJump;

                // too low fuel
                if (fuel <  1)
                {
                    return null;
                }

                // must refuel 
                if (!refuel.HasValue)
                {
                    return null;
                }

                // must be above current fuel
                if (refuel.Value < fuel)
                {
                    return null;
                }

                var maxFuelPerJump = this.ship.FSD.MaxFuelPerJump;

                var fuelToScoop = (refuel.Value - fuel) +
                    (jumps - 2) * maxFuelPerJump;

                time += fuelToScoop / this.ship.FuelScoopRate;
                time += 20 * jumps;

                fuel = refuel.Value - maxFuelPerJump;
            }

            return new Edge
            {
                From = node,
                To = this.CreateNode(system, fuel),
                Distance = time
            };
        }

        private double GetTravelTime(double distance)
        {
            if (distance < 1)
            {
                return 0;
            }
            else
            {
                return 12 * Math.Log(distance);
            }
        }


        private double GetJumpRange(double fuel)
        {
            var key = (int)(100 * fuel);

            if (!this.jumpRangeCache.ContainsKey(key))
            {
                this.jumpRangeCache[key] = this.ship.GetJumpRange(fuel);
            }

            return this.jumpRangeCache[key];
        }

        private double GetDistance(StarSystem a, StarSystem b)
        {
            return ((Vector)a).Distance((Vector)b);
        }

        private class Edge : IEdge
        {

            public INode From { get; set; }

            public INode To { get; set; }

            public double Distance { get; set; }

        }

        public class Node : INode
        {

            public string Id { get; }

            public StarSystem StarSystem { get; }

            public double Fuel { get; }

            public Node(string id, StarSystem system, double fuel)
            {
                this.Id = id;
                this.StarSystem = system;
                this.Fuel = fuel;
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
