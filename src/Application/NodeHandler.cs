using EliteBuckyball.Application.Interfaces;
using EliteBuckyball.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace EliteBuckyball.Application
{
    public class NodeHandler : INodeHandler
    {

        private const double TIME_PER_JUMP = 52;

        private readonly IStarSystemRepository starSystemRepository;
        private readonly IEnumerable<IEdgeConstraint> edgeConstraints;
        private readonly Ship ship;
        private readonly IReadOnlyList<FuelRange> refuelLevels;
        private readonly StarSystem start;
        private readonly StarSystem goal;

        private readonly double bestJumpRange;
        private readonly double[] jumpRangeCache;

        public NodeHandler(
            IStarSystemRepository starSystemRepository,
            IEnumerable<IEdgeConstraint> edgeConstraints,
            Ship ship,
            List<FuelRange> refuelLevels,
            StarSystem start,
            StarSystem goal)
        {
            this.starSystemRepository = starSystemRepository;
            this.edgeConstraints = edgeConstraints;
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
                .Select(x => (INode)this.CreateNode(this.start, x, x, 0))
                .ToList();
        }

        private Node CreateNode(StarSystem system, FuelRange fuel, FuelRange? refuel, int jumps)
        {
            int min = (int)(2 * fuel.Min / this.ship.FSD.MaxFuelPerJump);
            int max = (int)(2 * fuel.Max / this.ship.FSD.MaxFuelPerJump);

            return new Node(
                (system.Id, min, max),
                system,
                fuel,
                refuel,
                jumps
            );
        }

        public double GetShortestDistanceToGoal(INode a)
        {
            var distance = Vector3.Distance(a.StarSystem.Coordinates, this.goal.Coordinates);
            //return TIME_PER_JUMP * Math.Ceiling(distance / (4 * this.bestJumpRange));
            return TIME_PER_JUMP * distance / (4 * this.bestJumpRange);
        }

        private IEnumerable<StarSystem> GetNeighbors(INode node, double distance)
        {
            var systems = this.starSystemRepository.GetNeighbors(node.StarSystem, distance)
                .Where(x => this.edgeConstraints.All(y => y.IsValid(node.StarSystem, x)));

            if (Vector3.DistanceSquared(node.StarSystem.Coordinates, this.goal.Coordinates) < distance * distance)
            {
                systems = systems.Concat(new List<StarSystem> { this.goal });
            }

            return systems;
        }

        public Task<List<IEdge>> GetEdges(INode node)
        {
            var baseNode = (Node)node;

            var systems = this.GetNeighbors(node, 500);

            var edges = systems
                .AsParallel()
                .AsUnordered()
                .SelectMany(system => new List<Edge?>()
                    .Concat(new List<Edge?>
                    {
                        this.CreateEdge(baseNode, system, null)
                    })
                    .Concat(this.refuelLevels.Select(x => this.CreateEdge(baseNode, system, x)))
                )
                .Where(x => x != null)
                .Cast<IEdge>()
                .ToList();

            return Task.FromResult(edges);
        }

        private Edge? CreateEdge(Node node, StarSystem system, FuelRange? refuel)
        {
            var min = this.CreateEdge(node, node.Fuel.Min, system, refuel?.Min, CreateEdgeType.Mininum);

            if (min == null)
            {
                return null;
            }

            var max = this.CreateEdge(node, node.Fuel.Max, system, refuel?.Max, CreateEdgeType.Maximum);

            if (max == null)
            {
                return null;
            }

            if (min.Value.Jumps == 1 && max.Value.Jumps == 1)
            {
                // Allowed
            }
            else if (min.Value.Jumps > 1 && max.Value.Jumps > 1)
            {
                // Allowed
            }
            else
            {
                // Not allowed
                return null;
            }

            var distance = Math.Max(min.Value.Distance, max.Value.Distance);
            var jumps = Math.Max(min.Value.Jumps, max.Value.Jumps);

            return new Edge
            {
                From = node,
                To = this.CreateNode(
                    system,
                    new FuelRange(
                        min.Value.Fuel,
                        max.Value.Fuel
                    ),
                    refuel,
                    jumps
                ),
                Distance = distance,
                Jumps = jumps
            };
        }

        private Edge? CreateEdge(Node node, double fuel, StarSystem system, double? refuel, CreateEdgeType type)
        {
            var from = node.StarSystem.Coordinates;
            var to = system.Coordinates;

            double time = 0;

            var distance = Vector3.Distance(from, to);

            double fstJumpFactor;
            var fstJumpRange = this.GetJumpRange(fuel);
            if (node.StarSystem.HasNeutron && node.StarSystem.DistanceToNeutron < 100)
            {
                fstJumpFactor = 4;
                time += this.GetTravelTime(node.StarSystem.DistanceToNeutron);
            }
            else
            {
                fstJumpFactor = 1;
            }

            var rstJumpFactor = 1;
            var rstJumpRange = this.GetJumpRange(this.ship.FuelCapacity);
            var rstDistance = Math.Max(distance - (fstJumpFactor * fstJumpRange), 0);

            var jumps = (int)(1 + Math.Ceiling(rstDistance / (rstJumpFactor * rstJumpRange)));

            time += TIME_PER_JUMP * jumps;

            if (jumps == 1)
            {
                fuel -= this.GetFuelCost(fuel, distance / fstJumpFactor);

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
                if (fuel < 1)
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
                time += 20 * jumps;

                if (type == CreateEdgeType.Mininum)
                {
                    fuel = refuel.Value - this.ship.FSD.MaxFuelPerJump;
                }
                else if (type == CreateEdgeType.Maximum)
                {
                    fuel = refuel.Value;
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }

            return new Edge
            {
                From = node,
                To = this.CreateNode(
                    system,
                    new FuelRange(fuel, fuel),
                    refuel.HasValue ? new FuelRange(refuel.Value, refuel.Value) : (FuelRange?)null,
                    jumps
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

        private double GetFuelCost(double fuel, double distance)
        {
            var totalMass = this.ship.DryMass + fuel;
            return this.GetBoostedFuelMultiplier(fuel) * Math.Pow(distance * totalMass / this.ship.FSD.OptimisedMass, this.ship.FSD.FuelPower);
        }

        private double GetBoostedFuelMultiplier(double fuel)
        {
            var baseRange = this.GetJumpRange(fuel);
            return this.ship.FSD.FuelMultiplier * Math.Pow(baseRange / (baseRange + this.ship.GuardianBonus), this.ship.FSD.FuelPower);
        }

        private double GetJumpRange(double fuel)
        {
            return this.jumpRangeCache[(int)(100 * fuel)];
        }

        private enum CreateEdgeType
        {
            Mininum,
            Maximum
        }

    }
}
