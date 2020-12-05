using EliteBuckyball.Application;
using EliteBuckyball.Application.EdgeConstraints;
using EliteBuckyball.Application.Interfaces;
using EliteBuckyball.Domain.Entities;
using EliteBuckyball.Infrastructure;
using EliteBuckyball.Infrastructure.Persistence;
using EliteBuckyball.Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace EliteBuckyball.ConsoleApp.GenerateRoute
{
    public class Program
    {
        public static void Main()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.ignored.json")
                .Build();

            var app = new AppSettings();
            configuration.GetSection("App").Bind(app);

            var serviceProvider = new ServiceCollection()
                .AddLogging()
                .AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseMySql(
                        configuration.GetConnectionString("Default"),
                        new MySqlServerVersion(new Version(8, 0, 21)),
                        mysqlOptions =>
                        {
                            mysqlOptions.EnableRetryOnFailure();
                        }
                    );
                })
                .BuildServiceProvider();

            var dbContext = serviceProvider.GetService<ApplicationDbContext>();

            var repository = new StarSystemRepository(
                dbContext,
                new StarSystemRepository.Options
                {
                    Mode = "neutron"
                }
            );

            var ship = new Ship
            {
                Name = app.Ship.Name,
                DryMass = app.Ship.DryMass,
                FuelCapacity = app.Ship.FuelCapacity,
                FSD = new FrameShiftDrive
                {
                    FuelPower = app.Ship.FsdFuelPower,
                    FuelMultiplier = app.Ship.FsdFuelMultiplier,
                    MaxFuelPerJump = app.Ship.FsdMaxFuelPerJump,
                    OptimisedMass = app.Ship.FsdOptimisedMass
                },
                GuardianBonus = app.Ship.GuardianBonus,
                FuelScoopRate = app.Ship.FuelScoopRate
            };

            var refuelLevels = app.Ship.RefuelLevels
                .Select(x => new RefuelRange(x.Type, x.Min, x.Max))
                .ToList();

            var start = repository.GetByName(app.Start);
            if (app.NeutronBoostedAtStart)
            {
                start.HasNeutron = true;
                start.DistanceToNeutron = 0;
            }

            var goal = repository.GetByName(app.Goal);

            var edgeConstraints = app.EdgeConstraints
                .Select<EdgeConstraintSettings, IEdgeConstraint>(x =>
                {
                    if (x.Type == "Angle")
                    {
                        return new AngleEdgeConstraint(
                            goal,
                            double.Parse(x.Parameters["Angle"])
                        );
                    }
                    else if (x.Type == "Cylinder")
                    {
                        return new CylinderEdgeConstraint(
                            start,
                            goal,
                            float.Parse(x.Parameters["Radius"])
                        );
                    }
                    else if (x.Type == "MaximumJumps")
                    {
                        return new MaximumJumpsEdgeConstraint(
                            int.Parse(x.Parameters["Jumps"])
                        );
                    }
                    else if (x.Type == "MinimumDistance")
                    {
                        return new MinimumDistanceEdgeConstraint(
                            double.Parse(x.Parameters["Distance"])
                        );
                    }
                    else if (x.Type == "FuelRestriction")
                    {
                        var min = x.Parameters["Min"];
                        var max = x.Parameters["Max"];

                        return new FuelRestrictionEdgeConstraint(
                            repository.GetByName(x.Parameters["System"]),
                            string.IsNullOrEmpty(min) ? (double?)null : double.Parse(min),
                            string.IsNullOrEmpty(max) ? (double?)null : double.Parse(max)
                        );
                    }
                    else if (x.Type == "Exclude")
                    {
                        return new ExcludeEdgeConstraint(
                            x.Parameters["Names"]
                                .Split(';')
                                .Select(y => y.Trim())
                                .Where(y => !string.IsNullOrEmpty(y))
                                .Select(y => {
                                    var parts = y.Split(",");
                                    return (parts[0], parts[1]);
                                })
                                .ToList()
                        );
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                })
                .ToList();

            var shipHandler = new ShipHandler(ship);

            var refuelStarFinder = new RefuelStarFinder(
                dbContext,
                new StarSystemRepository(
                    dbContext,
                    new StarSystemRepository.Options
                    {
                        Mode = "scoopable"
                    }
                ),
                shipHandler,
                app.UseFsdBoost
            );

            var nodeHandler = new NodeHandler(
                repository,
                refuelStarFinder,
                edgeConstraints,
                shipHandler,
                refuelLevels,
                start,
                goal,
                new NodeHandler.Options
                {
                    UseFsdBoost = app.UseFsdBoost,
                    UseRefuelStarFinder = app.UseRefuelStarFinder,
                    MultiJumpRangeFactor = app.MultiJumpRangeFactor,
                    NeighborRangeMin = Math.Max(500, 6 * shipHandler.BestJumpRange),
                    NeighborRangeMax = 5000,
                    NeighborRangeMultiplier = 2,
                    NeighborCountMin = 10,
                    NeighborCountMax = 1000,
                }
            );

            var pathfinder = new Pathfinder(nodeHandler);

            var tStart = DateTime.UtcNow;

            var route = pathfinder.Invoke()
                .Cast<Node>()
                .ToList();

            if (app.UseRefuelStarFinder)
            {
                route = refuelStarFinder.Invoke(route).ToList();
            }

            var tEnd = DateTime.UtcNow;

            Console.WriteLine();
            Console.WriteLine(
                "Neighbors cache: [Hits={0}, Misses={1}]",
                nodeHandler.cacheHits,
                nodeHandler.cacheMisses
            );

            Console.WriteLine();
            Console.WriteLine("Time: {0}", (tEnd - tStart));


            Console.WriteLine();
            Console.WriteLine("route:");
            for (var i = 0; i < route.Count; i++) 
            {
                var prev = 0 < i ? route[i - 1] : default;
                var node = route[i];
                
                var system = node.StarSystem;

                Console.WriteLine("  - name: {0}", system.Name);

                if (system.Name.Equals("???"))
                {
                    Console.WriteLine("    x: {0:0}", system.Coordinates.X);
                    Console.WriteLine("    y: {0:0}", system.Coordinates.Y);
                    Console.WriteLine("    z: {0:0}", system.Coordinates.Z);
                }

                if (system.HasNeutron && system.DistanceToNeutron != 0)
                {
                    Console.WriteLine("    neutron: true");
                }

                if (node.Jumps == 0)
                {
                    Console.WriteLine("    fuel: {0:0.00}", node.FuelAvg);
                }
                //if (prev.Fuel.Avg < node.Fuel.Avg)
                else if (node.RefuelType != RefuelType.None && node.Jumps == 1)
                {
                    Console.WriteLine("    scoopable: true");
                    Console.WriteLine("    fuel: {0:0.00}", node.FuelAvg);
                }
                else if (system.HasScoopable && system.DistanceToScoopable == 0)
                {
                    Console.WriteLine("    scoopable: false");
                }

                if (!system.HasNeutron && app.UseFsdBoost)
                {
                    Console.WriteLine("    boost: true");
                }

                if (i < route.Count - 1)
                {
                    //Console.WriteLine("    jumps: {0}", node.Jumps);
                    //Console.WriteLine("    time: {0:0}", pathfinder.GetDistance(node, route[i+1]));
                }
            }
        }
    }
}
