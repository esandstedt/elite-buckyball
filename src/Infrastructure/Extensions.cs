using EliteBuckyball.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace EliteBuckyball.Infrastructure
{
    public static class Extensions
    {
        public static StarSystem AsDomainObject(this Persistence.Entities.StarSystem system)
        {
            return new StarSystem
            {
                Id = system.Id,
                Name = system.Name,
                Coordinates = new Vector3(system.X, system.Y, system.Z),
                HasNeutron = system.DistanceToNeutron.HasValue,
                DistanceToNeutron = system.DistanceToNeutron ?? default,
                HasScoopable = system.DistanceToScoopable.HasValue,
                DistanceToScoopable = system.DistanceToScoopable ?? default,
                HasStation = system.DistanceToStation.HasValue,
                DistanceToStation = system.DistanceToStation ?? default,
                HasWhiteDwarf = system.DistanceToWhiteDwarf.HasValue,
                DistanceToWhiteDwarf = system.DistanceToWhiteDwarf ?? default,
                Date = system.Date ?? default
            };
        }
    }
}
