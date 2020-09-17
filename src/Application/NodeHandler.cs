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
            IEnumerable<IEdgeConstraint> edgeConstraints,
            Ship ship,
            List<RefuelRange> refuelLevels,
            StarSystem start,
            StarSystem goal,
            Options options)
        {
            this.starSystemRepository = starSystemRepository;
            this.edgeConstraints = edgeConstraints;
            this.ship = ship;
            this.refuelLevels = refuelLevels;
            this.start = start;
            this.goal = goal;
            this.options = options;

            this.minimumFuelLevel = ship.FSD.MaxFuelPerJump / 16;

            this.jumpTime = new JumpTime(ship);

            this.bestJumpRange = this.ship.GetJumpRange(ship.FSD.MaxFuelPerJump);

            var topFuelLevel = this.refuelLevels.Max(x => x.Fuel.Max);
            this.jumpRangeCache = Enumerable.Range(0, (int)(topFuelLevel / JUMPRANGE_CACHE_RESOLUTION) + 1)
                .Select(x => this.ship.GetJumpRange(JUMPRANGE_CACHE_RESOLUTION * x))
                .ToArray();

            this.neighborsCache = new Dictionary<long, List<StarSystem>>(10000000);
        }

        public IEnumerable<INode> GetInitialNodes()
        {
            return this.refuelLevels
                .Where(x => x.Type == RefuelRange.TYPE_INITIAL)
                .Select(x => (INode)this.CreateNode(this.start, x.Fuel, null, 0));
        }

        private Node CreateNode(StarSystem system, FuelRange fuel, RefuelRange? refuel, int jumps)
        {
            return new Node(
                (system.Id, this.GetNodeFuelId(fuel.Min), this.GetNodeFuelId(fuel.Max)),
                system,
                system.Equals(this.goal),
                fuel,
                refuel,
                jumps
            );
        }

        private byte GetNodeFuelId(double fuel)
        {
            // Higher resolution when the tank is empty.

            var id = (byte)(16 * fuel / this.ship.FSD.MaxFuelPerJump);

            if (fuel < 1 * this.ship.FSD.MaxFuelPerJump)
            {
                return id;
            }
            else if (fuel < 2 * this.ship.FSD.MaxFuelPerJump)
            {
                return (byte)(id - id % 2);
            }
            else 
            {
                return (byte)(id - id % 4);
            }
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
                .Cast<IEdge>()
                .AsSequential();
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

        private IEnumerable<Edge?> CreateEdges(Node node, StarSystem system)
        {
            yield return this.CreateEdge(node, system, null, this.options.UseFsdBoost);

            if (node.StarSystem != this.start)
            {
                foreach (var level in this.refuelLevels.Where(x => x.Type != RefuelRange.TYPE_INITIAL))
                {
                    yield return this.CreateEdge(node, system, level, this.options.UseFsdBoost);
                }
            }
        }

        private Edge? CreateEdge(Node node, StarSystem system, RefuelRange? refuel, bool useFsdBoost)
        {
            var min = this.CreateEdge(
                node.StarSystem,
                system,
                node.Fuel.Min,
                refuel?.Type,
                refuel?.Fuel.Min,
                useFsdBoost
            );

            if (min == null)
            {
                return null;
            }

            var max = this.CreateEdge(
                node.StarSystem,
                system,
                node.Fuel.Max,
                refuel?.Type,
                refuel?.Fuel.Max,
                useFsdBoost
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
                    new FuelRange(
                        min.Value.Fuel,
                        max.Value.Fuel
                    ),
                    refuel,
                    jumps
                ),
                distance,
                jumps
            );
        }

        private SimpleEdge? CreateEdge(StarSystem from, StarSystem to, double fuel, string refuelType, double? refuelLevel, bool useFsdBoost)
        {
            if (this.CanCompleteInSingleJump(from, to, refuelLevel ?? fuel, useFsdBoost))
            {
                return this.GetSingleJump(from, to, fuel, refuelType, refuelLevel, useFsdBoost);
            }
            else
            {
                return this.GetMultiJump(from, to, fuel, refuelType, refuelLevel, useFsdBoost);
            }
        }

        private bool CanCompleteInSingleJump(StarSystem from, StarSystem to, double fuel, bool useFsdBoost)
        {
            var distance = Vector3.Distance(from.Coordinates, to.Coordinates);

            var jumpRange = this.GetJumpRange(fuel);

            double jumpFactor;
            if (from.HasNeutron && from.DistanceToNeutron < 100)
            {
                jumpFactor = 4;
            }
            else if (useFsdBoost) 
            {
                jumpFactor = 2;
            }
            else
            {
                jumpFactor = 1;
            }

            return distance < jumpFactor * jumpRange;
        }

        private SimpleEdge? GetSingleJump(StarSystem from, StarSystem to, double fuel, string refuelType, double? refuelLevel, bool useFsdBoost)
        {
            double jumpFactor;
            BoostType boostType;
            if (from.HasNeutron && from.DistanceToNeutron < 100)
            {
                jumpFactor = 4;
                boostType = BoostType.Neutron;
            }
            else if (useFsdBoost) 
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

            if (refuelType != null)
            {
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
                time = this.jumpTime.Get(from, to, boostType, null, null).Value;
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
                From = from,
                To = to,
                Distance = time.Value,
                Fuel = fuel,
                Jumps = 1
            };
        }

        private SimpleEdge? GetMultiJump(StarSystem from, StarSystem to, double fuel, string refuelType, double? refuelLevel, bool useFsdBoost)
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
            else if (useFsdBoost) 
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
            if (refuelType == null)
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
            var rstJumpFactor = useFsdBoost ? 2 : 1;
            var rstBoostType = useFsdBoost ? BoostType.Synthesis : BoostType.None;
            var rstJumps = (int)Math.Ceiling(rstDistance / (rstJumpFactor * rstJumpRange));

            var timeFst = this.jumpTime.Get(from, null, fstBoostType, null, null);
            var timeRst = this.jumpTime.Get(null, null, rstBoostType, null, null);
            var timeRstRefuel = this.jumpTime.Get(null, null, rstBoostType, refuelType, rstJumps * this.ship.FSD.MaxFuelPerJump + refuelLevel.Value - fuel);

            if (!timeFst.HasValue || !timeRst.HasValue || !timeRstRefuel.HasValue)
            {
                return null;
            }

            var time = timeFst.Value + (rstJumps - 1) * timeRst.Value + timeRstRefuel.Value;

            fuel = Math.Max(0, refuelLevel.Value - this.ship.FSD.MaxFuelPerJump);

            return new SimpleEdge
            {
                From = from,
                To = to,
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

            public StarSystem From { get; set; }

            public StarSystem To { get; set; }

            public double Fuel { get; set; }

            public double Distance { get; set; }


            public int Jumps { get; set; }

        }

        private enum CreateEdgeType
        {
            Mininum,
            Maximum
        }

    }

}
