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

        private const double TIME_PER_JUMP = 50;

        private readonly IStarSystemRepository starSystemRepository;
        private readonly IEnumerable<IEdgeConstraint> edgeConstraints;
        private readonly Ship ship;
        private readonly IReadOnlyList<FuelRange> refuelLevels;
        private readonly StarSystem start;
        private readonly StarSystem goal;
        private readonly bool useFsdBoost;
        private readonly double neighborRange;

        private readonly double bestJumpRange;
        private double[] jumpRangeCache;

        public NodeHandler(
            IStarSystemRepository starSystemRepository,
            IEnumerable<IEdgeConstraint> edgeConstraints,
            Ship ship,
            List<FuelRange> refuelLevels,
            StarSystem start,
            StarSystem goal,
            bool useFsdBoost,
            double neighborRange)
        {
            this.starSystemRepository = starSystemRepository;
            this.edgeConstraints = edgeConstraints;
            this.ship = ship;
            this.refuelLevels = refuelLevels;
            this.start = start;
            this.goal = goal;
            this.useFsdBoost = useFsdBoost;
            this.neighborRange = neighborRange;

            this.bestJumpRange = this.ship.GetJumpRange(ship.FSD.MaxFuelPerJump);
            this.jumpRangeCache = Enumerable.Range(0, (int)(100 * this.ship.FuelCapacity) + 1)
                .Select(x => this.ship.GetJumpRange(x / 100.0))
                .ToArray();
        }

        public IEnumerable<INode> GetInitialNodes()
        {
            return this.refuelLevels
                .Select(x => (INode)this.CreateNode(this.start, x, x, 0));
        }

        private Node CreateNode(StarSystem system, FuelRange fuel, FuelRange? refuel, int jumps)
        {
            int min = (int)(2 * fuel.Min / this.ship.FSD.MaxFuelPerJump);
            int max = (int)(2 * fuel.Max / this.ship.FSD.MaxFuelPerJump);

            return new Node(
                (system.Id, min, max),
                system,
                system.Equals(this.goal),
                fuel,
                refuel,
                jumps
            );
        }

        public double GetShortestDistanceToGoal(INode a)
        {
            var distance = Vector3.Distance(a.StarSystem.Coordinates, this.goal.Coordinates);
            return TIME_PER_JUMP * distance / (4 * this.bestJumpRange);
        }


        private (StarSystem, double, List<StarSystem>) cache;
        public int cacheHits = 0;
        public int cacheMisses = 0;

        private IEnumerable<StarSystem> GetNeighbors(StarSystem system, double distance)
        {
            if (system.Equals(this.cache.Item1) && Math.Abs(distance - this.cache.Item2) < 1e-6)
            {
                this.cacheHits += 1;
                return this.cache.Item3;
            }
            else
            {
                this.cacheMisses += 1;
            }

            var results = this.starSystemRepository.GetNeighbors(system, distance)
                .Where(x => this.edgeConstraints.All(y => y.ValidBefore(system, x)))
                .ToList();

            if (Vector3.DistanceSquared(system.Coordinates, this.goal.Coordinates) < distance * distance)
            {
                results.Add(this.goal);
            }

            /*
            var maxCount = 500;
            if (maxCount < results.Count())
            {
                results = results
                    .OrderBy(x => Guid.NewGuid())
                    .Take(maxCount)
                    .ToList();
            }
             */

            this.cache = (system, distance, results);

            return results;
        }

        public IEnumerable<IEdge> GetEdges(INode node)
        {
            var baseNode = (Node)node;

            return this.GetNeighbors(node.StarSystem, this.neighborRange)
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
                .Where(x => this.edgeConstraints.All(y => y.ValidAfter(x)))
                .Cast<IEdge>()
                .AsSequential();
        }

        private Edge? CreateEdge(Node node, StarSystem system, FuelRange? refuel)
        {
            var min = this.CreateEdge(node.StarSystem, system, node.Fuel.Min, refuel?.Min);

            if (min == null)
            {
                return null;
            }

            var max = this.CreateEdge(node.StarSystem, system, node.Fuel.Max, refuel?.Max);

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

        private SimpleEdge? CreateEdge(StarSystem from, StarSystem to, double fuel, double? refuel)
        {
            double time = 0;

            var distance = Vector3.Distance(from.Coordinates, to.Coordinates);

            double fstJumpFactor;
            var fstJumpRange = this.GetJumpRange(fuel);
            if (from.HasNeutron && from.DistanceToNeutron < 100)
            {
                fstJumpFactor = 4;
                time += this.GetTravelTime(from.DistanceToNeutron);
            }
            else
            {
                fstJumpFactor = this.useFsdBoost ? 2 : 1;
            }

            var rstJumpFactor = this.useFsdBoost ? 2 : 1;
            var rstJumpRange = this.GetJumpRange(this.ship.FuelCapacity);
            var rstDistance = Math.Max(distance - (fstJumpFactor * fstJumpRange), 0);

            var jumps = (int)(1 + Math.Ceiling(rstDistance / (rstJumpFactor * rstJumpRange)));

            time += TIME_PER_JUMP * jumps;

            if (jumps == 1)
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
                    if (!to.HasScoopable || 100 < to.DistanceToScoopable)
                    {
                        return null;
                    }

                    time += this.GetTravelTime(to.DistanceToScoopable);
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

                fuel = Math.Max(0, refuel.Value - this.ship.FSD.MaxFuelPerJump);
            }

            return new SimpleEdge
            {
                From = from,
                To = to,
                Distance = time,
                Fuel = fuel,
                Refuel = refuel,
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

        public double GetJumpRange(double fuel)
        {
            return this.jumpRangeCache[(int)(100 * fuel)];
        }

        private struct SimpleEdge
        {

            public StarSystem From { get; set; }

            public StarSystem To { get; set; }

            public double Distance { get; set; }

            public double Fuel { get; set; }

            public double? Refuel { get; set; }

            public int Jumps { get; set; }

        }

        private enum CreateEdgeType
        {
            Mininum,
            Maximum
        }

    }

}
