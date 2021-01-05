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
            public bool UseRefuelStarFinder { get; set; }
            public double NeighborRangeMin { get; set; }
            public double NeighborRangeMax { get; set; }
            public double NeighborRangeMultiplier { get; set; }
            public int NeighborCountMin { get; set; }
            public int NeighborCountMax { get; set; }
        }

        private readonly IStarSystemRepository starSystemRepository;
        private readonly IRefuelStarFinder refuelStarFinder;
        private readonly IEnumerable<IEdgeConstraint> edgeConstraints;
        private readonly ShipHandler shipHandler;
        private Ship ship => this.shipHandler.Ship;
        private readonly IReadOnlyList<JumpParameters> jumpParameters;
        private readonly StarSystem start;
        private readonly StarSystem goal;
        private readonly Options options;
        private readonly double minimumFuelLevel;

        private readonly JumpTime jumpTime;

        public int cacheHits = 0;
        public int cacheMisses = 0;
        private readonly Dictionary<long, List<StarSystem>> neighborsCache;

        public NodeHandler(
            IStarSystemRepository starSystemRepository,
            IRefuelStarFinder refuelStarFinder,
            IEnumerable<IEdgeConstraint> edgeConstraints,
            ShipHandler shipHandler,
            List<JumpParameters> jumpParameters,
            StarSystem start,
            StarSystem goal,
            Options options)
        {
            this.starSystemRepository = starSystemRepository;
            this.refuelStarFinder = refuelStarFinder;
            this.edgeConstraints = edgeConstraints;
            this.shipHandler = shipHandler;
            this.jumpParameters = jumpParameters;
            this.start = start;
            this.goal = goal;
            this.options = options;

            this.minimumFuelLevel = this.ship.FSD.MaxFuelPerJump / 16;

            this.jumpTime = new JumpTime(this.ship);

            this.neighborsCache = new Dictionary<long, List<StarSystem>>();
        }

        public IEnumerable<INode> GetInitialNodes()
        {
            return this.jumpParameters
                .Where(x => x.RefuelType == RefuelType.Initial)
                .Select(x => this.CreateNode(
                    this.start,
                    x.RefuelMin.Value,
                    x.RefuelMax.Value,
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
            return JumpTime.NeutronWithoutRefuel * distance / (4 * this.shipHandler.BestJumpRange);
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
                .Cast<IEdge>();
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

        private List<StarSystem> GetNeighbors(StarSystem system, double distance)
        {
            var neighbors = this.starSystemRepository.GetNeighbors(system.Coordinates, distance);

            if (Vector3.Distance(system.Coordinates, this.goal.Coordinates) < distance)
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
                new JumpParameters(RefuelType.None)
            );

            foreach (var parameters in this.jumpParameters.Where(x => x.RefuelType != RefuelType.Initial))
            {
                yield return this.CreateEdge(
                    node,
                    system,
                    parameters
                );
            }
        }

        private Edge CreateEdge(Node from, StarSystem system, JumpParameters parameters)
        {
            var min = this.CreateEdge(
                from.StarSystem,
                system,
                from.FuelMin,
                new SimpleJumpParameters
                {
                    RefuelType = parameters.RefuelType,
                    Refuel = parameters.RefuelMin,
                    JumpsMin = parameters.JumpsMin,
                    JumpsMax = parameters.JumpsMax,
                    MultiJumpRangeFactor = parameters.MultiJumpRangeFactor
                }
            );

            if (min == null)
            {
                return null;
            }

            var max = this.CreateEdge(
                from.StarSystem,
                system,
                from.FuelMax,
                new SimpleJumpParameters
                {
                    RefuelType = parameters.RefuelType,
                    Refuel = parameters.RefuelMax,
                    JumpsMin = parameters.JumpsMin,
                    JumpsMax = parameters.JumpsMax,
                    MultiJumpRangeFactor = parameters.MultiJumpRangeFactor
                }
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

            var to = this.CreateNode(
                system,
                min.Value.Fuel,
                max.Value.Fuel,
                parameters.RefuelType,
                parameters.RefuelMin,
                parameters.RefuelMax,
                jumps
            );

            if (jumps == 2 && this.options.UseRefuelStarFinder)
            {
                var candidate = this.refuelStarFinder.GetCandidate(from, to);
                if (candidate == null)
                {
                    return null;
                }
            }

            return new Edge(
                from,
                to,
                distance,
                jumps
            );
        }

        private SimpleEdge? CreateEdge(
            StarSystem from,
            StarSystem to,
            double fuel,
            SimpleJumpParameters parameters)
        {
            if (this.CanCompleteInSingleJump(from, to, parameters.Refuel ?? fuel))
            {
                return this.GetSingleJump(from, to, fuel, parameters);
            }
            else
            {
                return this.GetMultiJump(from, to, fuel, parameters);
            }
        }

        private bool CanCompleteInSingleJump(StarSystem from, StarSystem to, double fuel)
        {
            var distance = Vector3.Distance(from.Coordinates, to.Coordinates);

            var jumpRange = this.shipHandler.GetJumpRange(fuel);

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

        private SimpleEdge? GetSingleJump(
            StarSystem from,
            StarSystem to,
            double fuel,
            SimpleJumpParameters parameters)
        {
            if (1 < parameters.JumpsMin)
            {
                return null;
            }

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

            if (parameters.RefuelType != RefuelType.None)
            {
                // cannot be start system
                if (from == this.start)
                {
                    return null;
                }

                // must be above current fuel
                if (parameters.Refuel.Value < fuel)
                {
                    return null;
                }

                // must have scoopable 
                if (!from.HasScoopable || 100 < from.DistanceToScoopable)
                {
                    return null;
                }

                time = this.jumpTime.Get(from, boostType, parameters.RefuelType, parameters.Refuel.Value - fuel);

                fuel = parameters.Refuel.Value;
            }
            else
            {
                time = this.jumpTime.Get(from, boostType, RefuelType.None, null);
            }

            if (!time.HasValue)
            {
                return null;
            }

            var distance = Vector3.Distance(from.Coordinates, to.Coordinates);

            fuel -= this.shipHandler.GetFuelCost(fuel, distance / jumpFactor);

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

        private SimpleEdge? GetMultiJump(
            StarSystem from,
            StarSystem to,
            double fuel,
            SimpleJumpParameters parameters)
        {
            // must refuel 
            if (parameters.RefuelType == RefuelType.None)
            {
                return null;
            }

            var distance = Vector3.Distance(from.Coordinates, to.Coordinates);

            var fstJumpRange = this.shipHandler.GetJumpRange(fuel);
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
            var fstDistance = parameters.MultiJumpRangeFactor * fstJumpFactor * fstJumpRange;

            // was a multi-jump with refuel but single without
            if (distance < fstDistance)
            {
                return null;
            }

            var rstDistance = distance - fstDistance;
            var rstJumpRange = this.shipHandler.GetJumpRange(parameters.Refuel.Value);
            var rstJumpFactor = this.options.UseFsdBoost ? 2 : 1;
            var rstBoostType = this.options.UseFsdBoost ? BoostType.Synthesis : BoostType.None;
            var rstJumps = (int)Math.Ceiling(rstDistance / (parameters.MultiJumpRangeFactor * rstJumpFactor * rstJumpRange));

            var jumps = rstJumps + 1;

            if (jumps < parameters.JumpsMin || parameters.JumpsMax < jumps)
            {
                return null;
            }

            double? timeFst;
            double? timeRst;
            double? timeRstRefuel;
            double fuelCostRst;

            if (jumps == 2 && this.options.UseRefuelStarFinder)
            {
                var refuel = this.refuelStarFinder.GetCandidate(from, fuel, to, parameters.Refuel.Value);

                if (refuel == null)
                {
                    return null;
                }

                timeFst = this.jumpTime.Get(from, fstBoostType, RefuelType.None, null);
                var distFst = Vector3.Distance(from.Coordinates, refuel.Coordinates);
                var fuelCostFst = this.shipHandler.GetFuelCost(fuel, distFst / fstJumpFactor);

                // must have enough fuel to make first jump
                if (fuel < fuelCostFst + this.minimumFuelLevel)
                {
                    return null;
                }

                // must refuel to a higher fuel level
                if (parameters.Refuel.Value < (fuel - fuelCostFst))
                {
                    return null;
                }

                timeRst = 0;

                timeRstRefuel = this.jumpTime.Get(refuel, rstBoostType, parameters.RefuelType, parameters.Refuel.Value - (fuel + fuelCostFst));
                var distRst = Vector3.Distance(refuel.Coordinates, to.Coordinates);
                fuelCostRst = this.shipHandler.GetFuelCost(parameters.Refuel.Value, distRst / rstJumpFactor);
            }
            else
            {
                // must have enough fuel to make first jump
                if (fuel < this.ship.FSD.MaxFuelPerJump + this.minimumFuelLevel)
                {
                    return null;
                }

                // must refuel to a higher fuel level
                if (parameters.Refuel.Value < fuel)
                {
                    return null;
                }

                timeFst = this.jumpTime.Get(from, fstBoostType, RefuelType.None, null);
                timeRst = this.jumpTime.Get(null, rstBoostType, RefuelType.None, null);
                timeRstRefuel = this.jumpTime.Get(null, rstBoostType, parameters.RefuelType, rstJumps * this.ship.FSD.MaxFuelPerJump + parameters.Refuel.Value - fuel);
                fuelCostRst = this.ship.FSD.MaxFuelPerJump;
            }

            if (!timeFst.HasValue || !timeRst.HasValue || !timeRstRefuel.HasValue)
            {
                return null;
            }

            var time = timeFst.Value + (rstJumps - 1) * timeRst.Value + timeRstRefuel.Value;

            fuel = Math.Max(0, parameters.Refuel.Value - fuelCostRst);

            return new SimpleEdge
            {
                Distance = time,
                Fuel = fuel,
                Jumps = jumps
            };
        }

        private struct SimpleEdge
        {
            public double Fuel { get; set; }
            public double Distance { get; set; }
            public int Jumps { get; set; }
        }

        private struct SimpleJumpParameters
        {
            public RefuelType RefuelType { get; set; }
            public double? Refuel { get; set; }
            public int JumpsMin { get; set; } 
            public int JumpsMax { get; set; } 
            public double MultiJumpRangeFactor { get; set; }
        }

    }

}
