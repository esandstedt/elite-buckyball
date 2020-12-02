using EliteBuckyball.Domain.Entities;
using EliteBuckyball.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace EliteBuckyball.Infrastructure.Repository
{
    public class Sector
    {

        public int X { get; }
        public int Y { get; }
        public int Z { get; }
        public DateTime Timestamp { get; private set; }

        private readonly List<StarSystem> list;

        public static Sector Create(ApplicationDbContext dbContext, string mode, Coordinate coord)
        {
            return CreateMany(dbContext, mode, new List<Coordinate> { coord }).Single();
        }

        public static IEnumerable<Sector> CreateMany(ApplicationDbContext dbContext, string mode, List<Coordinate> coords)
        {
            var tStart = DateTime.Now;

            foreach (var coord in coords)
            {
                List<StarSystem> list;

                if (mode == "all")
                {
                    list = dbContext.StarSystems
                        .AsNoTracking()
                        .Where(s =>
                            s.SectorX == coord.X &&
                            s.SectorY == coord.Y &&
                            s.SectorZ == coord.Z
                        )
                        .Select(x => x.AsDomainObject())
                        .ToList();
                }
                else if (mode == "neutron")
                {
                    list = dbContext.StarSystems
                        .AsNoTracking()
                        .Where(s =>
                            s.DistanceToNeutron.HasValue && s.DistanceToNeutron.Value < 100 &&
                            s.SectorX == coord.X &&
                            s.SectorY == coord.Y &&
                            s.SectorZ == coord.Z
                        )
                        .Select(x => x.AsDomainObject())
                        .ToList();
                }
                else if (mode == "scoopable")
                {
                    list = dbContext.StarSystems
                        .AsNoTracking()
                        .Where(s =>
                            s.DistanceToScoopable.HasValue && s.DistanceToScoopable.Value == 0 &&
                            s.SectorX == coord.X &&
                            s.SectorY == coord.Y &&
                            s.SectorZ == coord.Z
                        )
                        .Select(x => x.AsDomainObject())
                        .ToList();
                }
                else
                {
                    throw new InvalidOperationException();
                }

                yield return new Sector(coord.X, coord.Y, coord.Z, list);
            }

            /*
            Console.WriteLine("{0} Sector.CreateMany: {1} {2} {3} ms",
                DateTime.Now.ToString(@"HH\:mm\:ss"),
                mode.ToString(),
                coords.Count,
                (DateTime.Now - tStart).TotalMilliseconds
            );
             */
        }

        private Sector(int x, int y, int z, List<StarSystem> list)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
            this.Timestamp = DateTime.Now;
            this.list = list;
        }

        public IEnumerable<StarSystem> GetNeighbors(Vector3 coordinate, double distance)
        {
            this.Timestamp = DateTime.Now;
            return this.list.Where(x => Vector3.DistanceSquared(coordinate, x.Coordinates) < distance * distance);
        }

    }
}
