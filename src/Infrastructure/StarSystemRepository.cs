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

        public StarSystemRepository(ApplicationDbContext dbContext)
        {
            this.dbContext = dbContext;
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
            var sectorX = (int)Math.Floor(system.X / 1000);
            var sectorY = (int)Math.Floor(system.Y / 1000);
            var sectorZ = (int)Math.Floor(system.Z / 1000);

            var sectorXList = new List<int> { sectorX - 1, sectorX, sectorX + 1 };
            var sectorYList = new List<int> { sectorY - 1, sectorY, sectorY + 1 };
            var sectorZList = new List<int> { sectorZ - 1, sectorZ, sectorZ + 1 };

            IList<StarSystem> result = this.dbContext.StarSystems
                .Where(x =>
                    x.DistanceToNeutron.HasValue &&
                    x.DistanceToNeutron.Value < 500 &&
                    sectorXList.Contains(x.SectorX) &&
                    sectorYList.Contains(x.SectorY) &&
                    sectorZList.Contains(x.SectorZ)
                )
                .Select(Convert)
                .ToList()
                .Where(x => Distance(system, x) < distance)
                .ToList();

            return Task.FromResult(result);
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
