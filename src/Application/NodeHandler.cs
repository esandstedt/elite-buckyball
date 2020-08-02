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

        private const double TIME_WITCHSPACE = 14;
        private const double TIME_FSD_CHARGE = 20;
        private const double TIME_FSD_COOLDOWN = 10;
        private const double TIME_NEUTRON_BOOST = 13;
        private const double TIME_SYNTHESIS_BOOST = 20;
        private const double TIME_TRAVEL_MIN = 5;
        private const double TIME_PARALLEL_MARGIN = 5;

        private const double JUMPRANGE_CACHE_RESOLUTION = 0.001;

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

        public int cacheHits = 0;
        public int cacheMisses = 0;
        private Dictionary<long, List<StarSystem>> neighborsCache;

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

            var topFuelLevel = this.refuelLevels.Max(x => x.Max);
            this.jumpRangeCache = Enumerable.Range(0, (int)(topFuelLevel / JUMPRANGE_CACHE_RESOLUTION) + 1)
                .Select(x => this.ship.GetJumpRange(JUMPRANGE_CACHE_RESOLUTION * x))
                .ToArray();

            this.neighborsCache = new Dictionary<long, List<StarSystem>>(10000000);
        }

        public IEnumerable<INode> GetInitialNodes()
        {
            return this.refuelLevels
                .Select(x => (INode)this.CreateNode(this.start, x, x, 0));
        }

        private Node CreateNode(StarSystem system, FuelRange fuel, FuelRange? refuel, int jumps)
        {
            int min = (int)(8 * fuel.Min / this.ship.FSD.MaxFuelPerJump);
            int max = (int)(8 * fuel.Max / this.ship.FSD.MaxFuelPerJump);

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
            var timePerJump = TIME_WITCHSPACE + TIME_TRAVEL_MIN + TIME_NEUTRON_BOOST + TIME_FSD_CHARGE;
            return timePerJump * distance / (4 * this.bestJumpRange);
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
            List<StarSystem> value;
            if (this.neighborsCache.TryGetValue(system.Id, out value))
            {
                this.cacheHits += 1;
                return value;
            }

            var result = this.GetNeighbors(system)
                .Where(x => this.edgeConstraints.All(y => y.ValidBefore(system, x)))
                .ToList();

            this.cacheMisses += 1;
            this.neighborsCache[system.Id] = result;

            return result;
        }

        private IEnumerable<StarSystem> GetNeighbors(StarSystem system)
        {
            var neighbors = this.starSystemRepository.GetNeighbors(system, this.neighborRange);
            foreach (var neighbor in neighbors)
            {
                yield return neighbor;
            }

            if (Vector3.Distance(system.Coordinates, this.goal.Coordinates) < this.neighborRange)
            {
                yield return this.goal;
            }
        }

        private IEnumerable<Edge?> CreateEdges(Node node, StarSystem system)
        {
            yield return this.CreateEdge(node, system, null, false);

            foreach (var level in this.refuelLevels)
            {
                yield return this.CreateEdge(node, system, level, false);

                if (this.useFsdBoost)
                {
                    yield return this.CreateEdge(node, system, level, true);
                }
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
            int jumps = 1;
            double time = 0;

            var distance = Vector3.Distance(from.Coordinates, to.Coordinates);

            var fstJumpRange = this.GetJumpRange(Math.Min(refuel ?? fuel, this.ship.FuelCapacity));
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

            if (distance < fstDistance)
            {
                fuel -= this.ship.GetFuelCost(fuel, distance / fstJumpFactor);
            }
            else
            {
                fuel -= this.ship.FSD.MaxFuelPerJump;
            }

            if (fuel < 1)
            {
                return null;
            }

            var rstDistance = distance - fstDistance;
            if (rstDistance < 0)
            {
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

                    time += this.GetJumpTime(from, to, fstBoostType, refuel.Value - fuel);

                    fuel = refuel.Value;
                }
                else
                {
                    time += this.GetJumpTime(from, to, fstBoostType, null);
                }
            }
            else
            {
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

                time += this.GetJumpTime(from, null, fstBoostType, null);

                var rstJumpRange = this.GetJumpRange(Math.Min(refuel.Value, this.ship.FuelCapacity));
                var rstJumpFactor = useFsdBoost ? 2 : 1;
                var rstBoostType = useFsdBoost ? BoostType.Synthesis : BoostType.None;
                var rstJumps = (int)Math.Ceiling(rstDistance / (rstJumpFactor * rstJumpRange));

                time += (rstJumps - 1) * this.GetJumpTime(null, null, rstBoostType, null);
                time += this.GetJumpTime(null, null, rstBoostType, rstJumps * this.ship.FSD.MaxFuelPerJump + refuel.Value - fuel);

                jumps += rstJumps;

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

        private double GetJumpTime(StarSystem from, StarSystem to, BoostType boost, double? refuel)
        {
            var timeFst = TIME_WITCHSPACE;

            if (boost == BoostType.Neutron)
            {
                timeFst += this.GetTravelTime(from?.DistanceToNeutron ?? 0);
                timeFst += TIME_NEUTRON_BOOST;
            }

            var timeRst = 0.0;

            if (boost == BoostType.None)
            {
                timeRst = TIME_FSD_COOLDOWN + TIME_FSD_CHARGE;
            }
            else if (boost == BoostType.Synthesis)
            {
                timeRst = TIME_SYNTHESIS_BOOST + TIME_FSD_CHARGE;
            }
            else if (boost == BoostType.Neutron)
            {
                timeRst = TIME_FSD_CHARGE;
            }

            if (refuel.HasValue)
            {
                var timeRefuel = this.GetTravelTime(to?.DistanceToScoopable ?? 0) +
                    refuel.Value / this.ship.FuelScoopRate;

                var timeForParallel = timeRst + TIME_PARALLEL_MARGIN;

                if (timeRefuel < timeForParallel)
                {
                    timeRefuel += TIME_FSD_CHARGE;
                }

                timeRst = Math.Max(timeRst, timeRefuel);
            }

            return timeFst + timeRst;
        }

        private enum BoostType
        {
            None,
            Synthesis,
            Neutron
        }

        private double GetTravelTime(double distance)
        {
            return Math.Max(
                TIME_TRAVEL_MIN,
                12 * Math.Log(Math.Max(1, distance))
            );
        }

        public double GetJumpRange(double fuel)
        {
            return this.jumpRangeCache[(int)(fuel / JUMPRANGE_CACHE_RESOLUTION)];
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
