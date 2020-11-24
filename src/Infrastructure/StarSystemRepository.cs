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

namespace EliteBuckyball.Infrastructure
{
    public class StarSystemRepository : IStarSystemRepository
    {

        public const int SECTOR_SIZE = 500;

        public enum Mode
        {
            All,
            Neutron,
            Scoopable
        }

        public class Options
        {
            public Mode Mode { get; set; }
        }

        private readonly ApplicationDbContext dbContext;
        private readonly Mode mode;
        private readonly Dictionary<(int, int, int), Sector> sectors;

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
                return Convert(result);
            }

            return null;
        }

        public StarSystem GetByName(string name)
        {
            var result = this.dbContext.StarSystems.SingleOrDefault(x => x.Name == name);
            if (result != null)
            {
                return Convert(result);
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
            var coords = new List<Coordinate>();

            for (var x = minSectorX; x <= maxSectorX; x++)
            {
                for (var y = minSectorY; y <= maxSectorY; y++)
                {
                    for (var z = minSectorZ; z <= maxSectorZ; z++)
                    {
                        var key = (x, y, z);
                        if (!this.sectors.ContainsKey(key))
                        {
                            coords.Add(new Coordinate(x, y, z));
                        }
                        else
                        {
                            sectors.Add(this.sectors[key]);
                        }
                    }
                }
            }

            if (coords.Any())
            {
                foreach (var sector in Sector.CreateMany(this.dbContext, this.mode, coords))
                {
                    var key = (sector.X, sector.Y, sector.Z);
                    this.sectors[key] = sector;

                    sectors.Add(sector);
                }
            }

            return sectors.SelectMany(s => s.GetNeighbors(coordinate, distance));
        }

        private static StarSystem Convert(Persistence.Entities.StarSystem system)
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
                Date = system.Date ?? default
            };
        }

        public void Create(StarSystem system)
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
            dbContext.SaveChanges();

            dbContext.Entry(entity).State = EntityState.Detached;
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

        public void Clear()
        {
            this.sectors.Clear();
        }

        private class Sector
        {

            private readonly List<StarSystem> list;

            public int X { get; }
            public int Y { get; }
            public int Z { get; }

            public static Sector Create(ApplicationDbContext dbContext, Mode mode, Coordinate coord)
            {
                return CreateMany(dbContext, mode, new List<Coordinate> { coord }).Single();
            }

            public static IEnumerable<Sector> CreateMany(ApplicationDbContext dbContext, Mode mode, List<Coordinate> coords)
            {
                var tStart = DateTime.Now;

                foreach (var coord in coords)
                {
                    List<StarSystem> list;

                    if (mode == Mode.All)
                    {
                        list = dbContext.StarSystems
                            .AsNoTracking()
                            .Where(s =>
                                s.SectorX == coord.X &&
                                s.SectorY == coord.Y &&
                                s.SectorZ == coord.Z
                            )
                            .Select(Convert)
                            .ToList();
                    }
                    else if (mode == Mode.Neutron)
                    {
                        list = dbContext.StarSystems
                            .AsNoTracking()
                            .Where(s =>
                                s.DistanceToNeutron.HasValue && s.DistanceToNeutron.Value < 100 &&
                                s.SectorX == coord.X &&
                                s.SectorY == coord.Y &&
                                s.SectorZ == coord.Z
                            )
                            .Select(Convert)
                            .ToList();
                    }
                    else if (mode == Mode.Scoopable)
                    {
                        list = dbContext.StarSystems
                            .AsNoTracking()
                            .Where(s =>
                                s.DistanceToScoopable.HasValue && s.DistanceToScoopable.Value == 0 &&
                                s.SectorX == coord.X &&
                                s.SectorY == coord.Y &&
                                s.SectorZ == coord.Z
                            )
                            .Select(Convert)
                            .ToList();
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }

                    yield return new Sector(coord.X, coord.Y, coord.Z, list);
                }

                Console.WriteLine("Sector.CreateMany: {0} {1} ms", coords.Count, (DateTime.Now - tStart).TotalMilliseconds);
            }

            public Sector(int x, int y, int z, List<StarSystem> list)
            {
                this.X = x;
                this.Y = y;
                this.Z = z;
                this.list = list;
            }

            public IEnumerable<StarSystem> GetNeighbors(Vector3 coordinate, double distance)
            {
                return this.list.Where(x => Vector3.DistanceSquared(coordinate, x.Coordinates) < distance * distance);
            }

            private static StarSystem Convert(Persistence.Entities.StarSystem system)
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
                    Date = system.Date ?? default
                };
            }
        }

        private class Coordinate
        {
            public int X { get;  }
            public int Y { get;  }
            public int Z { get;  }

            public Coordinate(int x, int y, int z)
            {
                this.X = x;
                this.Y = y;
                this.Z = z;
            }
        }
    }
}
