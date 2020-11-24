using EliteBuckyball.Application.Interfaces;
using EliteBuckyball.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace EliteBuckyball.Application
{
    public class NodeHandler : INodeHandler
    {

        public class Options
        {
            public bool UseFsdBoost { get; set; }
            public double MultiJumpRangeFactor { get; set; }
            public double NeighborRangeMin { get; set; }
            public double NeighborRangeMax { get; set; }
            public double NeighborRangeMultiplier { get; set; }
            public int NeighborCountMin { get; set; }
            public int NeighborCountMax { get; set; }
        }

        private const double JUMPRANGE_CACHE_RESOLUTION = 0.001;

        private readonly IStarSystemRepository starSystemRepository;
        private readonly RefuelStarFinder refuelStarFinder;
        private readonly IEnumerable<IEdgeConstraint> edgeConstraints;
        private readonly Ship ship;
        private readonly IReadOnlyList<RefuelRange> refuelLevels;
        private readonly StarSystem start;
        private readonly StarSystem goal;
        private readonly Options options;
        private readonly double minimumFuelLevel;

        private readonly JumpTime jumpTime;

        private readonly double bestJumpRange;
        private readonly double[] jumpRangeCache;

        public int cacheHits = 0;
        public int cacheMisses = 0;
        private readonly Dictionary<long, List<StarSystem>> neighborsCache;

        public NodeHandler(
            IStarSystemRepository starSystemRepository,
            RefuelStarFinder refuelStarFinder,
            IEnumerable<IEdgeConstraint> edgeConstraints,
            Ship ship,
            List<RefuelRange> refuelLevels,
            StarSystem start,
            StarSystem goal,
            Options options)
        {
            this.starSystemRepository = starSystemRepository;
            this.refuelStarFinder = refuelStarFinder;
            this.edgeConstraints = edgeConstraints;
            this.ship = ship;
            this.refuelLevels = refuelLevels;
            this.start = start;
            this.goal = goal;
            this.options = options;

            this.minimumFuelLevel = ship.FSD.MaxFuelPerJump / 16;

            this.jumpTime = new JumpTime(ship);

            this.bestJumpRange = this.ship.GetJumpRange(ship.FSD.MaxFuelPerJump);

            var topFuelLevel = this.refuelLevels.Max(x => x.FuelMax);
            this.jumpRangeCache = Enumerable.Range(0, (int)(topFuelLevel / JUMPRANGE_CACHE_RESOLUTION) + 1)
                .Select(x => this.ship.GetJumpRange(JUMPRANGE_CACHE_RESOLUTION * x))
                .ToArray();

            this.neighborsCache = new Dictionary<long, List<StarSystem>>();
        }

        public IEnumerable<INode> GetInitialNodes()
        {
            return this.refuelLevels
                .Where(x => x.Type == RefuelType.Initial)
                .Select(x => this.CreateNode(
                    this.start,
                    x.FuelMin.Value,
                    x.FuelMax.Value,
                    RefuelType.None,
                    null,
                    null,
                    0
                ))
                .Cast<INode>();
        }

        private Node CreateNode(
            StarSystem system,
            double fuelMin,
            double fuelMax,
            RefuelType refuelType,
            double? refuelMin,
            double? refuelMax,
            int jumps)
        {
            var fuelAvg = (fuelMin + fuelMax) / 2.0;

            return new Node(
                (system.Id, this.GetNodeFuelId(fuelAvg)),
                system,
                system.Equals(this.goal),
                fuelMin,
                fuelMax,
                refuelType,
                refuelMin,
                refuelMax,
                jumps
            );
        }

        private ushort GetNodeFuelId(double fuel)
        {
            var resolution = 16;

            // Higher resolution when the tank is empty.

            var id = resolution * fuel / this.ship.FSD.MaxFuelPerJump;

            if (fuel < 2 * this.ship.FSD.MaxFuelPerJump)
            {
                return (ushort)id;
            }
            else if (fuel < 4 * this.ship.FSD.MaxFuelPerJump)
            {
                return (ushort)(id - id % (resolution / 2));
            }
            else 
            {
                return (ushort)(id - id % (resolution / 4));
            };
        }

        public double GetShortestDistanceToGoal(INode a)
        {
            var distance = Vector3.Distance(a.StarSystem.Coordinates, this.goal.Coordinates);
            return JumpTime.NeutronWithoutRefuel * distance / (4 * this.bestJumpRange);
        }

        public IEnumerable<IEdge> GetEdges(INode node)
        {
            var baseNode = (Node)node;

            return this.GetNeighborsCached(node.StarSystem)
                .AsParallel()
                .AsUnordered()
                //.Where(x => this.edgeConstraints.All(y => y.ValidBefore(node.StarSystem, x)))
                .SelectMany(x => this.CreateEdges(baseNode, x))
                .Where(x => x != null)
                .Where(x => this.edgeConstraints.All(y => y.ValidAfter(x)))
                .AsSequential()
                .Select(x => this.ApplyRefuelStarFinder(x))
                .Where(x => x != null)
                .Cast<IEdge>();
        }

        private Edge ApplyRefuelStarFinder(Edge edge) {
            if (edge.Jumps == 2)
            {
                var from = edge.From;
                var to = edge.To;

                var candidate = this.refuelStarFinder.GetCandidate(from, to);

                if (candidate == null)
                {
                    return null;
                }

                var distance = Vector3.Distance(candidate.Coordinates, to.StarSystem.Coordinates);
                var jumpFactor = this.options.UseFsdBoost ? 2 : 1;

                var fuelMin = to.RefuelMin.Value - this.ship.GetFuelCost(to.RefuelMin.Value, distance / jumpFactor);
                var fuelMax = to.RefuelMax.Value - this.ship.GetFuelCost(to.RefuelMax.Value, distance / jumpFactor);

                return new Edge(
                    from, 
                    this.CreateNode(
                        to.StarSystem,
                        fuelMin,
                        fuelMax,
                        to.RefuelType,
                        to.RefuelMin,
                        to.RefuelMax,
                        to.Jumps
                    ), 
                    edge.Distance, 
                    edge.Jumps
                );
            }

            return edge;
        }

        private List<StarSystem> GetNeighborsCached(StarSystem system)
        {
            if (this.neighborsCache.TryGetValue(system.Id, out List<StarSystem> value))
            {
                this.cacheHits += 1;
                return value;
            }

            double range = this.options.NeighborRangeMin;
            List<StarSystem> result;
            do
            {
                result = this.GetNeighbors(system, range);
                range *= this.options.NeighborRangeMultiplier;
            } while (range < this.options.NeighborRangeMax && result.Count < this.options.NeighborCountMin);

            if (this.options.NeighborCountMax < result.Count)
            {
                result = result
                    .OrderBy(x => Guid.NewGuid())
                    .Take(this.options.NeighborCountMax)
                    .ToList();
            }

            this.cacheMisses += 1;
            this.neighborsCache[system.Id] = result;

            return result;
        }

        private List<StarSystem> GetNeighbors(StarSystem system, double range)
        {
            var neighbors = this.starSystemRepository.GetNeighbors(system, range);

            if (Vector3.Distance(system.Coordinates, this.goal.Coordinates) < range)
            {
                neighbors = neighbors
                    .Concat(new List<StarSystem>
                    { 
                        this.goal 
                    });
            }

            return neighbors
                .Where(x => this.edgeConstraints.All(y => y.ValidBefore(system, x)))
                .ToList();
        }

        private IEnumerable<Edge> CreateEdges(Node node, StarSystem system)
        {
            yield return this.CreateEdge(
                node,
                system,
                RefuelType.None,
                null,
                null
            );

            foreach (var level in this.refuelLevels.Where(x => x.Type != RefuelType.Initial))
            {
                yield return this.CreateEdge(
                    node,
                    system,
                    level.Type,
                    level.FuelMin,
                    level.FuelMax
                );
            }
        }

        private Edge CreateEdge(Node node, StarSystem system, RefuelType refuelType, double? refuelMin, double? refuelMax)
        {
            var min = this.CreateEdge(
                node.StarSystem,
                system,
                node.FuelMin,
                refuelType,
                refuelMin
            );

            if (min == null)
            {
                return null;
            }

            var max = this.CreateEdge(
                node.StarSystem,
                system,
                node.FuelMax,
                refuelType,
                refuelMax
            );

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

            return new Edge(
                node,
                this.CreateNode(
                    system,
                    min.Value.Fuel,
                    max.Value.Fuel,
                    refuelType,
                    refuelMin,
                    refuelMax,
                    jumps
                ),
                distance,
                jumps
            );
        }

        private SimpleEdge? CreateEdge(StarSystem from, StarSystem to, double fuel, RefuelType refuelType, double? refuelLevel)
        {
            if (this.CanCompleteInSingleJump(from, to, refuelLevel ?? fuel))
            {
                return this.GetSingleJump(from, to, fuel, refuelType, refuelLevel);
            }
            else
            {
                return this.GetMultiJump(from, to, fuel, refuelType, refuelLevel);
            }
        }

        private bool CanCompleteInSingleJump(StarSystem from, StarSystem to, double fuel)
        {
            var distance = Vector3.Distance(from.Coordinates, to.Coordinates);

            var jumpRange = this.GetJumpRange(fuel);

            double jumpFactor;
            if (from.HasNeutron && from.DistanceToNeutron < 100)
            {
                jumpFactor = 4;
            }
            else if (this.options.UseFsdBoost) 
            {
                jumpFactor = 2;
            }
            else
            {
                jumpFactor = 1;
            }

            return distance < jumpFactor * jumpRange;
        }

        private SimpleEdge? GetSingleJump(StarSystem from, StarSystem to, double fuel, RefuelType refuelType, double? refuelLevel)
        {
            double jumpFactor;
            BoostType boostType;
            if (from.HasNeutron && from.DistanceToNeutron < 100)
            {
                jumpFactor = 4;
                boostType = BoostType.Neutron;
            }
            else if (this.options.UseFsdBoost) 
            {
                jumpFactor = 2;
                boostType = BoostType.Synthesis;
            }
            else
            {
                jumpFactor = 1;
                boostType = BoostType.None;
            }

            double? time;

            if (refuelType != RefuelType.None)
            {
                // cannot be start system
                if (from == this.start)
                {
                    return null;
                }

                // must be above current fuel
                if (refuelLevel.Value < fuel)
                {
                    return null;
                }

                // must have scoopable 
                if (!from.HasScoopable || 100 < from.DistanceToScoopable)
                {
                    return null;
                }

                time = this.jumpTime.Get(from, to, boostType, refuelType, refuelLevel.Value - fuel);

                fuel = refuelLevel.Value;
            }
            else
            {
                time = this.jumpTime.Get(from, to, boostType, RefuelType.None, null);
            }

            if (!time.HasValue)
            {
                return null;
            }

            var distance = Vector3.Distance(from.Coordinates, to.Coordinates);

            fuel -= this.ship.GetFuelCost(fuel, distance / jumpFactor);

            if (fuel < this.minimumFuelLevel)
            {
                return null;
            }

            return new SimpleEdge
            {
                Distance = time.Value,
                Fuel = fuel,
                Jumps = 1
            };
        }

        private SimpleEdge? GetMultiJump(StarSystem from, StarSystem to, double fuel, RefuelType refuelType, double? refuelLevel)
        {

            var distance = Vector3.Distance(from.Coordinates, to.Coordinates);

            var fstJumpRange = this.options.MultiJumpRangeFactor * this.GetJumpRange(fuel);
            double fstJumpFactor;
            BoostType fstBoostType;
            if (from.HasNeutron && from.DistanceToNeutron < 100)
            {
                fstJumpFactor = 4;
                fstBoostType = BoostType.Neutron;
            }
            else if (this.options.UseFsdBoost) 
            {
                fstJumpFactor = 2;
                fstBoostType = BoostType.Synthesis;
            }
            else
            {
                fstJumpFactor = 1;
                fstBoostType = BoostType.None;
            }
            var fstDistance = fstJumpFactor * fstJumpRange;

            // was a multi-jump with refuel but single without
            if (distance < fstDistance)
            {
                return null;
            }

            // must refuel 
            if (refuelType == RefuelType.None)
            {
                return null;
            }

            // must be above current fuel
            if (refuelLevel.Value < fuel)
            {
                return null;
            }

            // must have enough fuel to make first jump
            if (fuel < this.ship.FSD.MaxFuelPerJump + this.minimumFuelLevel)
            {
                return null;
            }

            var rstDistance = distance - fstDistance;
            var rstJumpRange = this.options.MultiJumpRangeFactor * this.GetJumpRange(refuelLevel.Value);
            var rstJumpFactor = this.options.UseFsdBoost ? 2 : 1;
            var rstBoostType = this.options.UseFsdBoost ? BoostType.Synthesis : BoostType.None;
            var rstJumps = (int)Math.Ceiling(rstDistance / (rstJumpFactor * rstJumpRange));

            var timeFst = this.jumpTime.Get(from, null, fstBoostType, RefuelType.None, null);
            var timeRst = this.jumpTime.Get(null, null, rstBoostType, RefuelType.None, null);
            var timeRstRefuel = this.jumpTime.Get(null, null, rstBoostType, refuelType, rstJumps * this.ship.FSD.MaxFuelPerJump + refuelLevel.Value - fuel);

            if (!timeFst.HasValue || !timeRst.HasValue || !timeRstRefuel.HasValue)
            {
                return null;
            }

            var time = timeFst.Value + (rstJumps - 1) * timeRst.Value + timeRstRefuel.Value;

            fuel = Math.Max(0, refuelLevel.Value - this.ship.FSD.MaxFuelPerJump);

            return new SimpleEdge
            {
                Distance = time,
                Fuel = fuel,
                Jumps = 1 + rstJumps
            };
        }

        public double GetJumpRange(double fuel)
        {
            return this.jumpRangeCache[(int)(fuel / JUMPRANGE_CACHE_RESOLUTION)];
        }

        private struct SimpleEdge
        {
            public double Fuel { get; set; }
            public double Distance { get; set; }
            public int Jumps { get; set; }
        }

    }

}
