using EliteBuckyball.Application.Interfaces;
using EliteBuckyball.Domain.Entities;
using EliteBuckyball.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EliteBuckyball.Infrastructure
{
    public class StarSystemRepository : IStarSystemRepository
    {

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
            var minSectorX = (int)Math.Floor((coordinate.X - distance) / 1000);
            var maxSectorX = (int)Math.Floor((coordinate.X + distance) / 1000);
            var minSectorY = (int)Math.Floor((coordinate.Y - distance) / 1000);
            var maxSectorY = (int)Math.Floor((coordinate.Y + distance) / 1000);
            var minSectorZ = (int)Math.Floor((coordinate.Z - distance) / 1000);
            var maxSectorZ = (int)Math.Floor((coordinate.Z + distance) / 1000);

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
                            this.sectors[key] = new Sector(this.dbContext, this.mode, x, y, z);
                        }

                        sectors.Add(this.sectors[key]);
                    }
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
                SectorX = (int)Math.Floor(system.Coordinates.X / 1000),
                SectorY = (int)Math.Floor(system.Coordinates.Y / 1000),
                SectorZ = (int)Math.Floor(system.Coordinates.Z / 1000),
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
            entity.SectorX = (int)Math.Floor(system.Coordinates.X / 1000);
            entity.SectorY = (int)Math.Floor(system.Coordinates.Y / 1000);
            entity.SectorZ = (int)Math.Floor(system.Coordinates.Z / 1000);
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

            public Sector(ApplicationDbContext dbContext, Mode mode, int x, int y, int z)
            {
                if (mode == Mode.All)
                {
                    this.list = dbContext.StarSystems
                        .AsNoTracking()
                        .Where(s =>
                            s.SectorX == x &&
                            s.SectorY == y &&
                            s.SectorZ == z
                        )
                        .Select(Convert)
                        .ToList();
                }
                else if (mode == Mode.Neutron)
                {
                    this.list = dbContext.StarSystems
                        .AsNoTracking()
                        .Where(s =>
                            s.DistanceToNeutron.HasValue && s.DistanceToNeutron.Value < 100 &&
                            s.SectorX == x &&
                            s.SectorY == y &&
                            s.SectorZ == z
                        )
                        .Select(Convert)
                        .ToList();
                }
                else if (mode == Mode.Scoopable)
                {
                    this.list = dbContext.StarSystems
                        .AsNoTracking()
                        .Where(s =>
                            s.DistanceToScoopable.HasValue && s.DistanceToScoopable.Value == 0 &&
                            s.SectorX == x &&
                            s.SectorY == y &&
                            s.SectorZ == z
                        )
                        .Select(Convert)
                        .ToList();
                }
                else
                {
                    throw new InvalidOperationException();
                }
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
    }
}
