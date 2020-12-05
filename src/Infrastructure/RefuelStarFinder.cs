using EliteBuckyball.Application;
using EliteBuckyball.Application.Interfaces;
using EliteBuckyball.Domain.Entities;
using EliteBuckyball.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace EliteBuckyball.Infrastructure
{
    public class RefuelStarFinder : IRefuelStarFinder
    {
        private readonly ApplicationDbContext dbContext;
        private readonly IStarSystemRepository starSystemRepository;
        private readonly Ship ship;
        private readonly bool useFsdBoost;

        public RefuelStarFinder(
            ApplicationDbContext dbContext,
            IStarSystemRepository starSystemRepository,
            Ship ship,
            bool useFsdBoost)
        {
            this.dbContext = dbContext;
            this.starSystemRepository = starSystemRepository;
            this.ship = ship;
            this.useFsdBoost = useFsdBoost;
        }

        public IEnumerable<Node> Invoke(List<Node> nodes)
        {
            yield return nodes[0];

            for (var i=1; i<nodes.Count; i++)
            {
                var prev = nodes[i - 1];
                var current = nodes[i];

                if (current.Jumps == 2)
                {
                    foreach (var node in this.Apply(prev, current))
                    {
                        yield return node;
                    }
                }
                else
                {
                    yield return current;
                }
            }
        }

        public StarSystem GetCandidate(Node from, Node to)
        {
            var point = this.GetRefuelCoordinates(from.StarSystem, from.FuelAvg, to.StarSystem, to.RefuelAvg.Value);

            return this.starSystemRepository.GetNeighbors(point, 50)
                .OrderBy(x => Vector3.Distance(point, x.Coordinates))
                .FirstOrDefault(x =>      
                    this.CanMakeJump(from.StarSystem, x, from.FuelMin) &&
                    this.CanMakeJump(from.StarSystem, x, from.FuelMax) &&
                    this.CanMakeJump(x, to.StarSystem, to.RefuelMin.Value) &&
                    this.CanMakeJump(x, to.StarSystem, to.RefuelMax.Value)
                );
        }

        public StarSystem GetCandidate(StarSystem from, double fromFuel, StarSystem to, double toRefuel)
        {
            var point = this.GetRefuelCoordinates(from, fromFuel, to, toRefuel);

            return this.starSystemRepository.GetNeighbors(point, 50)
                .OrderBy(x => Vector3.Distance(point, x.Coordinates))
                .FirstOrDefault(x =>
                    this.CanMakeJump(from, x, fromFuel) &&
                    this.CanMakeJump(x, to, toRefuel)
                );
        }

        private IEnumerable<Node> Apply(Node from, Node to)
        {
            var candidate = this.GetCandidate(from, to);

            if (candidate == null)
            {
                candidate = new StarSystem
                {
                    Id = 0,
                    Name = "???",
                    Coordinates = this.GetRefuelCoordinates(from.StarSystem, from.FuelAvg, to.StarSystem, to.FuelAvg),
                    HasScoopable = true,
                };
            }

            yield return new Node(
                (0, 0),
                candidate,
                false,
                to.RefuelMin.Value,
                to.RefuelMax.Value,
                to.RefuelType,
                to.RefuelMin.Value,
                to.RefuelMax.Value,
                1
            );

            yield return new Node(
                to.Id,
                to.StarSystem,
                to.IsGoal,
                to.FuelMin,
                to.FuelMax,
                RefuelType.None,
                0,
                0,
                1
            );
        }

        private Vector3 GetRefuelCoordinates(StarSystem from, double fromFuel,  StarSystem to, double toRefuel)
        {
            var distance = Vector3.Distance(from.Coordinates, to.Coordinates);

            double fstJumpFactor;
            if (from.HasNeutron && from.DistanceToNeutron < 100)
            {
                fstJumpFactor = 4;
            }
            else if (this.useFsdBoost)
            {
                fstJumpFactor = 2;
            }
            else
            {
                fstJumpFactor = 1;
            }

            var fstJumpRange = ship.GetJumpRange(fromFuel);
            var fstDistance = fstJumpFactor * fstJumpRange;
            var fstPercent = fstDistance / distance;

            double sndJumpFactor;
            if (this.useFsdBoost)
            {
                sndJumpFactor = 2;
            }
            else
            {
                sndJumpFactor = 1;
            }

            var sndJumpRange = ship.GetJumpRange(toRefuel - this.ship.FSD.MaxFuelPerJump);
            var sndDistance = sndJumpFactor * sndJumpRange;
            var sndPercent = 1 - (sndDistance / distance);

            var percent = 0.25 * fstPercent + 0.75 * sndPercent;

            return Vector3.Add(
                from.Coordinates,
                Vector3.Multiply(
                    (float)percent,
                    Vector3.Subtract(
                        to.Coordinates,
                        from.Coordinates
                    )
                )
            );

        }

        private bool CanMakeJump(StarSystem from, StarSystem to, double fuel)
        {
            var distance = Vector3.Distance(from.Coordinates, to.Coordinates);

            var jumpRange = this.ship.GetJumpRange(fuel);
            double jumpFactor;
            if (from.HasNeutron && from.DistanceToNeutron < 100)
            {
                jumpFactor = 4;
            }
            else if (this.useFsdBoost)
            {
                jumpFactor = 2;
            }
            else
            {
                jumpFactor = 1;
            }

            return distance < (jumpRange * jumpFactor);
        }
    }
}
