using EliteBuckyball.Application.Interfaces;
using EliteBuckyball.Domain.Entities;
using EliteBuckyball.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EliteBuckyball.Infrastructure
{
    public class StarSystemRepository : IStarSystemRepository
    {

        private readonly ApplicationDbContext dbContext;
        private readonly Dictionary<(int, int, int), Sector> sectors;

        public StarSystemRepository(ApplicationDbContext dbContext)
        {
            this.dbContext = dbContext;
            this.sectors = new Dictionary<(int, int, int), Sector>();
        }

        public async Task<StarSystem> GetAsync(string name)
        {
            var result = await this.dbContext.StarSystems.SingleOrDefaultAsync(x => x.Name == name);
            if (result != null)
            {
                return Convert(result);
            }

            return null;
        }

        public Task<IList<StarSystem>> GetNeighborsAsync(StarSystem system, double distance)
        {
            var minSectorX = (int)Math.Floor((system.X - distance) / 1000);
            var maxSectorX = (int)Math.Floor((system.X + distance) / 1000);
            var minSectorY = (int)Math.Floor((system.Y - distance) / 1000);
            var maxSectorY = (int)Math.Floor((system.Y + distance) / 1000);
            var minSectorZ = (int)Math.Floor((system.Z - distance) / 1000);
            var maxSectorZ = (int)Math.Floor((system.Z + distance) / 1000);

            var sectors = new List<Sector>();

            for (var x = minSectorX; x <= maxSectorX; x++)
            {
                for (var y = minSectorY; y <= maxSectorY; y++)
                {
                    for (var z = minSectorZ; z <= maxSectorZ; z++)
                    {
                        var key = (x, y, z);
                        if (!this.sectors.ContainsKey(key))
                        {
                            this.sectors[key] = new Sector(this.dbContext, x, y, z);
                        }

                        sectors.Add(this.sectors[key]);
                    }
                }
            }

            IList<StarSystem> result = sectors
                .SelectMany(s => s.GetNeighbors(system, distance))
                .ToList();

            return Task.FromResult(result);
        }

        private static StarSystem Convert(Persistence.Entities.StarSystem system)
        {
            return new StarSystem
            {
                Id = system.Id,
                Name = system.Name,
                X = system.X,
                Y = system.Y,
                Z = system.Z,
                HasNeutron = system.DistanceToNeutron.HasValue,
                DistanceToNeutron = system.DistanceToNeutron ?? default,
                HasScoopable = system.DistanceToScoopable.HasValue,
                DistanceToScoopable = system.DistanceToScoopable ?? default,
                Date = system.Date ?? default
            };
        }
    }

    public class Sector
    {

        private List<StarSystem> list;

        public Sector(ApplicationDbContext dbContext, int x, int y, int z)
        {
            this.list = dbContext.StarSystems
                .Where(s =>
                    s.DistanceToNeutron.HasValue &&
                    s.DistanceToNeutron.Value == 0 &&
                    s.SectorX == x &&
                    s.SectorY == y &&
                    s.SectorZ == z
                )
                .Select(Convert)
                .ToList();
        }

        public List<StarSystem> GetNeighbors(StarSystem system, double distance)
        {
            return this.list
                .Where(x => Distance(system, x) < distance)
                .ToList();
        }

        private static double Distance(StarSystem a, StarSystem b)
        {
            return Math.Sqrt(
                Math.Pow(a.X - b.X, 2) +
                Math.Pow(a.Y - b.Y, 2) +
                Math.Pow(a.Z - b.Z, 2)
            );
        }

        private static StarSystem Convert(Persistence.Entities.StarSystem system)
        {
            return new StarSystem
            {
                Id = system.Id,
                Name = system.Name,
                X = system.X,
                Y = system.Y,
                Z = system.Z,
                HasNeutron = system.DistanceToNeutron.HasValue,
                DistanceToNeutron = system.DistanceToNeutron ?? default,
                HasScoopable = system.DistanceToScoopable.HasValue,
                DistanceToScoopable = system.DistanceToScoopable ?? default,
                Date = system.Date ?? default
            };
        }
    }
}
