using EliteBuckyball.Application.Interfaces;
using EliteBuckyball.Domain.Entities;
using EliteBuckyball.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EliteBuckyball.Infrastructure.Repository
{
    public class StarSystemRepository : IStarSystemRepository
    {

        public const int SECTOR_SIZE = 500;

        public class Options
        {
            public string Mode { get; set; }
        }

        private readonly ApplicationDbContext dbContext;
        private readonly string mode;
        private readonly Dictionary<(int, int, int), Sector> sectors;

        private readonly object lockObject = new object();

        public StarSystemRepository(
            ApplicationDbContext dbContext,
            Options options)
        {
            this.dbContext = dbContext;
            this.mode = options.Mode;
            this.sectors = new Dictionary<(int, int, int), Sector>();
        }

        public StarSystem Get(long id)
        {
            var result = this.dbContext.StarSystems.Find(id);
            if (result != null)
            {
                return result.AsDomainObject();
            }

            return null;
        }

        public StarSystem GetByName(string name)
        {
            var result = this.dbContext.StarSystems.SingleOrDefault(x => x.Name == name);
            if (result != null)
            {
                return result.AsDomainObject();
            }

            return null;
        }

        public bool Exists(long id)
        {
            return this.dbContext.StarSystems.Any(x => x.Id == id);
        }

        public ISet<long> Exists(List<long> ids)
        {
            return new HashSet<long>(
                this.dbContext.StarSystems
                    .Where(x => ids.Contains(x.Id))
                    .Select(x => x.Id)
            );
        }

        public IEnumerable<StarSystem> GetNeighbors(StarSystem system, double distance)
        {
            return this.GetNeighbors(system.Coordinates, distance);
        }

        public IEnumerable<StarSystem> GetNeighbors(Vector3 coordinate, double distance)
        {
            var minSectorX = (int)Math.Floor((coordinate.X - distance) / SECTOR_SIZE);
            var maxSectorX = (int)Math.Floor((coordinate.X + distance) / SECTOR_SIZE);
            var minSectorY = (int)Math.Floor((coordinate.Y - distance) / SECTOR_SIZE);
            var maxSectorY = (int)Math.Floor((coordinate.Y + distance) / SECTOR_SIZE);
            var minSectorZ = (int)Math.Floor((coordinate.Z - distance) / SECTOR_SIZE);
            var maxSectorZ = (int)Math.Floor((coordinate.Z + distance) / SECTOR_SIZE);

            var sectors = new List<Sector>();
            var keys = new List<(int x, int y, int z)>();

            for (var x = minSectorX; x <= maxSectorX; x++)
            {
                for (var y = minSectorY; y <= maxSectorY; y++)
                {
                    for (var z = minSectorZ; z <= maxSectorZ; z++)
                    {
                        var key = (x, y, z);
                        if (!this.sectors.ContainsKey(key))
                        {
                            keys.Add(key);
                        }
                        else
                        {
                            sectors.Add(this.sectors[key]);
                        }
                    }
                }
            }

            if (keys.Any())
            {
                lock (this.lockObject)
                {
                    var coords = keys
                        .Where(x => !this.sectors.ContainsKey(x))
                        .Select(key => new Coordinate(key.x, key.y, key.z))
                        .ToList();

                    if (coords.Any())
                    {
                        foreach (var sector in Sector.CreateMany(this.dbContext, this.mode, coords))
                        {
                            var key = (sector.X, sector.Y, sector.Z);
                            this.sectors[key] = sector;

                            sectors.Add(sector);
                        }

                        /*
                        var staleSectors = this.sectors.Values
                            .Where(x => x.LastInvoked < DateTime.Now.Subtract(TimeSpan.FromHours(1)))
                            .ToList();

                        if (staleSectors.Any())
                        {
                            Console.WriteLine(
                                "{0} Stale sectors: {1} {2}", 
                                DateTime.Now.ToString(@"HH\:mm\:ss"),
                                this.mode.ToString(),
                                staleSectors.Count
                            );

                            foreach (var sector in staleSectors)
                            {
                                this.sectors.Remove((sector.X, sector.Y, sector.Z));
                            }
                        }
                         */
                    }
                }
            }

            return sectors.SelectMany(s => s.GetNeighbors(coordinate, distance));
        }

        public void Create(StarSystem system)
        {
            this.CreateMany(new List<StarSystem> { system });
        }

        public void CreateMany(IEnumerable<StarSystem> systems)
        {
            foreach (var system in systems)
            {
                var entity = new Persistence.Entities.StarSystem
                {
                    Id = system.Id,
                    Name = system.Name,
                    X = system.Coordinates.X,
                    Y = system.Coordinates.Y,
                    Z = system.Coordinates.Z,
                    SectorX = (int)Math.Floor(system.Coordinates.X / SECTOR_SIZE),
                    SectorY = (int)Math.Floor(system.Coordinates.Y / SECTOR_SIZE),
                    SectorZ = (int)Math.Floor(system.Coordinates.Z / SECTOR_SIZE),
                    Date = system.Date.Date,
                    DistanceToNeutron = system.HasNeutron ? (int?)system.DistanceToNeutron : null,
                    DistanceToScoopable = system.HasScoopable ? (int?)system.DistanceToScoopable : null,
                };

                dbContext.Add(entity);
            }

            dbContext.SaveChanges();
        }

        public void Update(StarSystem system)
        {
            var entity = dbContext.StarSystems.Find(system.Id);

            if (entity == null)
            {
                throw new InvalidOperationException();
            }

            Update(entity, system);

            dbContext.SaveChanges();

            dbContext.Entry(entity).State = EntityState.Detached;
        }

        public void Update(List<StarSystem> systems)
        {
            var systemIds = systems.Select(x => x.Id).ToList();

            var entityMap = dbContext.StarSystems
                .Where(x => systemIds.Contains(x.Id))
                .ToList()
                .ToDictionary(x => x.Id);

            foreach (var system in systems)
            {
                if (!entityMap.ContainsKey(system.Id))
                {
                    throw new InvalidOperationException();
                }

                var entity = entityMap[system.Id];

                Update(entity, system);
            }

            dbContext.SaveChanges();
        }

        private void Update(Persistence.Entities.StarSystem entity, StarSystem system)
        {
            entity.Name = system.Name;
            entity.X = system.Coordinates.X;
            entity.Y = system.Coordinates.Y;
            entity.Z = system.Coordinates.Z;
            entity.SectorX = (int)Math.Floor(system.Coordinates.X / SECTOR_SIZE);
            entity.SectorY = (int)Math.Floor(system.Coordinates.Y / SECTOR_SIZE);
            entity.SectorZ = (int)Math.Floor(system.Coordinates.Z / SECTOR_SIZE);
            entity.Date = system.Date.Date;
            entity.DistanceToNeutron = system.HasNeutron ? (int?)system.DistanceToNeutron : null;
            entity.DistanceToScoopable = system.HasScoopable ? (int?)system.DistanceToScoopable : null;
        }
    }
}
