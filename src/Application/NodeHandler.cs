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
        private readonly IReadOnlyList<FuelRange> refuelLevels;
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
            List<FuelRange> refuelLevels,
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

            //this.minimumFuelLevel = ship.FSD.MaxFuelPerJump / 4;
            this.minimumFuelLevel = 0;

            this.jumpTime = new JumpTime(ship);

            this.bestJumpRange = this.ship.GetJumpRange(ship.FSD.MaxFuelPerJump);

            var topFuelLevel = this.refuelLevels.Max(x => x.Max);
            this.jumpRangeCache = Enumerable.Range(0, (int)(topFuelLevel / JUMPRANGE_CACHE_RESOLUTION) + 1)
                .Select(x => this.ship.GetJumpRange(JUMPRANGE_CACHE_RESOLUTION * x))
                .ToArray();

            this.neighborsCache = new Dictionary<long, List<StarSystem>>(10000000);
        }

        public IEnumerable<INode> GetInitialNodes()
        {
            return this.refuelLevels
                .Select(x => (INode)this.CreateNode(this.start, x, null, 0));
        }

        private Node CreateNode(StarSystem system, FuelRange fuel, FuelRange? refuel, int jumps)
        {
            int min = (int)(4 * fuel.Min / this.ship.FSD.MaxFuelPerJump);
            int max = (int)(4 * fuel.Max / this.ship.FSD.MaxFuelPerJump);

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
                .Where(x => !(system.Name.Equals("Bleia Eohn HS-K d8-5") && x.Name.Equals("Bleia Eohn ZW-B d13-5")))
                .Where(x => !x.Name.Equals("Blae Drye GL-Y e0"))
                .Where(x => !x.Name.Equals("Blae Drye RC-V d2-7"))
                .ToList();
        }

        private IEnumerable<Edge?> CreateEdges(Node node, StarSystem system)
        {
            yield return this.CreateEdge(node, system, null, false);

            foreach (var level in this.refuelLevels)
            {
                yield return this.CreateEdge(node, system, level, this.options.UseFsdBoost);
            }
        }

        private Edge? CreateEdge(Node node, StarSystem system, FuelRange? refuel, bool useFsdBoost)
        {
            var min = this.CreateEdge(
                node.StarSystem,
                system,
                node.Fuel.Min,
                refuel?.Min,
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
                refuel?.Max,
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

        private SimpleEdge? CreateEdge(StarSystem from, StarSystem to, double fuel, double? refuel, bool useFsdBoost)
        {
            if (this.CanCompleteInSingleJump(from, to, fuel, refuel, useFsdBoost))
            {
                return this.GetSingleJump(from, to, fuel, refuel, useFsdBoost);
            }
            else
            {
                return this.GetMultiJump(from, to, fuel, refuel, useFsdBoost);
            }
        }

        private bool CanCompleteInSingleJump(StarSystem from, StarSystem to, double fuel, double? refuel, bool useFsdBoost)
        {
            var distance = Vector3.Distance(from.Coordinates, to.Coordinates);

            var jumpRange = this.GetJumpRange(refuel ?? fuel);

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

        private SimpleEdge? GetSingleJump(StarSystem from, StarSystem to, double fuel, double? refuel, bool useFsdBoost)
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

            double time = 0;

            if (refuel.HasValue)
            {
                // must be above current fuel
                if (refuel.Value < fuel)
                {
                    return null;
                }

                // must have scoopable 
                if (!from.HasScoopable || 100 < from.DistanceToScoopable)
                {
                    return null;
                }

                time += this.jumpTime.Get(from, to, boostType, refuel.Value - fuel);

                fuel = refuel.Value;
            }
            else
            {
                time += this.jumpTime.Get(from, to, boostType, null);
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
                Distance = time,
                Fuel = fuel,
                Jumps = 1
            };
        }

        private SimpleEdge? GetMultiJump(StarSystem from, StarSystem to, double fuel, double? refuel, bool useFsdBoost)
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
            if (!refuel.HasValue)
            {
                return null;
            }

            // must be above current fuel
            if (refuel.Value < fuel)
            {
                return null;
            }

            // must have enough fuel to make first jump
            if (fuel < this.ship.FSD.MaxFuelPerJump + this.minimumFuelLevel)
            {
                return null;
            }

            var rstDistance = distance - fstDistance;
            var rstJumpRange = this.options.MultiJumpRangeFactor * this.GetJumpRange(refuel.Value);
            var rstJumpFactor = useFsdBoost ? 2 : 1;
            var rstBoostType = useFsdBoost ? BoostType.Synthesis : BoostType.None;
            var rstJumps = (int)Math.Ceiling(rstDistance / (rstJumpFactor * rstJumpRange));

            var time = this.jumpTime.Get(from, null, fstBoostType, null) +
                (rstJumps - 1) * this.jumpTime.Get(null, null, rstBoostType, null) +
                this.jumpTime.Get(null, null, rstBoostType, rstJumps * this.ship.FSD.MaxFuelPerJump + refuel.Value - fuel);

            fuel = Math.Max(0, refuel.Value - this.ship.FSD.MaxFuelPerJump);

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
