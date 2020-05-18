using EliteBuckyball.Application.Interfaces;
using EliteBuckyball.Domain.Entities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EliteBuckyball.Application
{
    public class NodeHandler : INodeHandler
    {

        private const double TIME_PER_JUMP = 52; 

        private readonly IStarSystemRepository starSystemRepository;
        private readonly Ship ship;
        private readonly IReadOnlyList<FuelRange> refuelLevels;
        private readonly StarSystem start;
        private readonly StarSystem goal;

        private readonly double bestJumpRange;
        private readonly double[] jumpRangeCache;

        public NodeHandler(IStarSystemRepository starSystemRepository, Ship ship, List<FuelRange> refuelLevels, StarSystem start, StarSystem goal)
        {
            this.starSystemRepository = starSystemRepository;
            this.ship = ship;
            this.refuelLevels = refuelLevels;
            this.start = start;
            this.goal = goal;

            this.bestJumpRange = this.ship.GetJumpRange(ship.FSD.MaxFuelPerJump);
            this.jumpRangeCache = Enumerable.Range(0, (int)(100 * ship.FuelCapacity) + 1)
                .Select(x => this.ship.GetJumpRange(x / 100.0))
                .ToArray();
        }

        public List<INode> GetInitialNodes()
        {
            return this.refuelLevels
                .Select(x => (INode)this.CreateNode(this.start, x, x))
                .ToList();
        }

        public INode Create(StarSystem system)
        {
            var fuel = new FuelRange(
                this.ship.FuelCapacity,
                this.ship.FuelCapacity
            );

            return this.CreateNode(
                system,
                fuel,
                fuel
            );
        }

        private Node CreateNode(StarSystem system, FuelRange fuel, FuelRange refuel)
        {
            int min = (int)(2 * fuel.Min / this.ship.FSD.MaxFuelPerJump);
            int max = (int)(2 * fuel.Max / this.ship.FSD.MaxFuelPerJump);

            return new Node(
                string.Join('-', system.Name, min, max),
                system,
                fuel,
                refuel
            );
        }

        public double GetShortestDistance(INode a, StarSystem b)
        {
            var distance = ((Vector)a.StarSystem).Distance((Vector)b);
            return TIME_PER_JUMP * Math.Ceiling(distance / (4 * this.bestJumpRange));
        }

        public Task<List<IEdge>> GetEdges(INode node)
        {
            var baseNode = (Node)node;

            var neighbors = this.starSystemRepository.GetNeighbors(node.StarSystem, 500).ToList();

            var results = neighbors
                .AsParallel()
                .AsUnordered()
                .SelectMany(system => new List<EdgeDefinition>()
                    .Concat(this.refuelLevels.Select(x => new EdgeDefinition(baseNode, system, x)))
                    .Concat(new List<EdgeDefinition>
                    {
                        new EdgeDefinition(baseNode, system, null),
                        new EdgeDefinition(baseNode, this.goal, null),
                        new EdgeDefinition(
                            baseNode,
                            this.goal,
                            new FuelRange(
                                this.ship.FuelCapacity,
                                this.ship.FuelCapacity
                            )
                        )
                    })
                )
                .Select(x => this.CreateEdge(x.Node, x.StarSystem, x.Refuel))
                .Where(x => x != null)
                .Cast<IEdge>()
                .ToList();

            return Task.FromResult(results);
        }

        private Edge CreateEdge(Node node, StarSystem system, FuelRange refuel)
        {
            var min = this.CreateEdge(node, node.Fuel.Min, system, refuel?.Min);

            if (min == null)
            {
                return null;
            }

            var max = this.CreateEdge(node, node.Fuel.Max, system, refuel?.Max);

            if (max == null)
            {
                return null;
            }

            if (1e-6 < Math.Abs(min.Jumps - max.Jumps))
            {
                return null;
            }

            return new Edge
            {
                From = node,
                To = this.CreateNode(
                    system,
                    new FuelRange(
                        min.Fuel,
                        max.Fuel
                    ),
                    refuel
                ),
                Distance = min.Distance,
                Jumps = min.Jumps
            };
        }

        private Edge CreateEdge(Node node, double fuel, StarSystem system, double? refuel)
        {
            var from = (Vector)node.StarSystem;
            var to = (Vector)system;

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

            time += TIME_PER_JUMP * jumps;

            if (jumps < 1.5) // only one jump (floating point comparison)
            {
                fuel -= this.ship.GetFuelCost(fuel, distance / fstJumpFactor);

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

                var fuelToScoop = (refuel.Value - fuel) +
                    (jumps - 2) * this.ship.FSD.MaxFuelPerJump;

                time += fuelToScoop / this.ship.FuelScoopRate;
                // time += 20 * jumps;

                fuel = refuel.Value - this.ship.FSD.MaxFuelPerJump;
            }

            return new Edge
            {
                From = node,
                To = this.CreateNode(
                    system,
                    new FuelRange(fuel, fuel),
                    refuel.HasValue ? new FuelRange(refuel.Value, refuel.Value) : null
                ),
                Distance = time,
                Fuel = fuel,
                Jumps = jumps
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
            return this.jumpRangeCache[(int)(100 * fuel)];
        }

        private struct EdgeDefinition
        {
            public Node Node;
            public StarSystem StarSystem;
            public FuelRange Refuel;

            public EdgeDefinition(Node node, StarSystem system, FuelRange refuel)
            {
                this.Node = node;
                this.StarSystem = system;
                this.Refuel = refuel;
            }
        }

        public class Edge : IEdge
        {

            public INode From { get; set; }

            public INode To { get; set; }

            public double Distance { get; set; }

            public double Fuel { get; set; }

            public double Jumps { get; set; }

        }

        public class Node : INode
        {

            public string Id { get; }

            public StarSystem StarSystem { get; }

            public FuelRange Fuel { get; }

            public FuelRange Refuel { get; }

            public Node(string id, StarSystem system, FuelRange fuel, FuelRange refuel)
            {
                this.Id = id;
                this.StarSystem = system;
                this.Fuel = fuel;
                this.Refuel = refuel;
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

    public class FuelRange
    {

        public double Min { get; set; }

        public double Max { get; set; }

        public FuelRange(double min, double max)
        {
            this.Min = min;
            this.Max = max;
        }

    }
}
