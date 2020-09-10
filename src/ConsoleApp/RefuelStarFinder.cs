﻿using EliteBuckyball.Application;
using EliteBuckyball.Application.Interfaces;
using EliteBuckyball.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace EliteBuckyball.ConsoleApp.GenerateRoute
{
    public class RefuelStarFinder
    {
        private readonly IStarSystemRepository starSystemRepository;
        private readonly Ship ship;
        private readonly bool useFsdBoost;

        public RefuelStarFinder(
            IStarSystemRepository starSystemRepository,
            Ship ship,
            bool useFsdBoost)
        {
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

        private IEnumerable<Node> Apply(Node from, Node to)
        {
            var point = this.GetRefuelCoordinates(from, to);

            var candidate = this.starSystemRepository.GetNeighbors(point, 50)
                .OrderBy(x => Vector3.Distance(point, x.Coordinates))
                .FirstOrDefault(x => this.IsValidCandidate(from, to, x));

            if (candidate == null)
            {
                candidate = new StarSystem
                {
                    Id = 0,
                    Name = "???",
                    Coordinates = point,
                    HasScoopable = true,
                };
            }

            yield return new Node(
                Guid.NewGuid(),
                candidate,
                false,
                to.Refuel.Value.Fuel,
                to.Refuel.Value,
                1
            );

            yield return new Node(
                to.Id,
                to.StarSystem,
                to.IsGoal,
                to.Fuel,
                null,
                1
            );
        }

        private Vector3 GetRefuelCoordinates(Node from, Node to)
        {
            var distance = Vector3.Distance(from.StarSystem.Coordinates, to.StarSystem.Coordinates);

            double fstJumpFactor;
            if (from.StarSystem.HasNeutron && from.StarSystem.DistanceToNeutron < 100)
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

            var fstJumpRange = ship.GetJumpRange(from.Fuel.Avg);
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

            var sndJumpRange = ship.GetJumpRange(
                ((to.Fuel.Min + to.Fuel.Max) / 2) +
                this.ship.FSD.MaxFuelPerJump
            );
            var sndDistance = sndJumpFactor * sndJumpRange;
            var sndPercent = 1 - (sndDistance / distance);

            var percent = 0.25 * fstPercent + 0.75 * sndPercent;

            return Vector3.Add(
                from.StarSystem.Coordinates,
                Vector3.Multiply(
                    (float)percent,
                    Vector3.Subtract(
                        to.StarSystem.Coordinates,
                        from.StarSystem.Coordinates
                    )
                )
            );

        }

        private bool IsValidCandidate(Node from, Node to, StarSystem candidate)
        {
            return this.CanMakeJump(from.StarSystem, candidate, from.Fuel.Min) &&
                this.CanMakeJump(from.StarSystem, candidate, from.Fuel.Max) &&
                this.CanMakeJump(candidate, to.StarSystem, to.Refuel.Value.Fuel.Min) &&
                this.CanMakeJump(candidate, to.StarSystem, to.Refuel.Value.Fuel.Max);
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
