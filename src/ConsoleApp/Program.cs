using EliteBuckyball.Application;
using EliteBuckyball.Application.EdgeConstraints;
using EliteBuckyball.Application.Interfaces;
using EliteBuckyball.Domain.Entities;
using EliteBuckyball.Infrastructure;
using EliteBuckyball.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EliteBuckyball.ConsoleApp.GenerateRoute
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.ignored.json")
                .Build();

            var serviceProvider = new ServiceCollection()
                .AddLogging()
                .AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseMySql(configuration.GetConnectionString("Default"), b =>
                    {
                        b.EnableRetryOnFailure();
                    });
                })
                .AddTransient<INodeHandler, NodeHandler>()
                .AddTransient<IStarSystemRepository, StarSystemRepository>()
                .BuildServiceProvider();

            var app = new AppSettings();
            configuration.GetSection("App").Bind(app);

            var repository = serviceProvider.GetService<IStarSystemRepository>();

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
                .Select(x => new FuelRange(x.Min, x.Max))
                .ToList();

            var start = repository.GetByName(app.Start);
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
                    else if (x.Type == "StartMaximumFuel")
                    {
                        return new StartMaximumFuelEdgeConstraint(
                            start,
                            double.Parse(x.Parameters["MaxFuel"])
                        );
                    }
                    else if (x.Type == "GoalMinimumFuel")
                    {
                        return new GoalMinimumFuelEdgeConstraint(
                            goal,
                            double.Parse(x.Parameters["MinFuel"])
                        );
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                })
                .ToList();

            var nodeHandler = new NodeHandler(
                repository,
                edgeConstraints,
                ship,
                refuelLevels,
                start,
                goal,
                app.UseFsdBoost,
                app.NeighborDistance
            );

            var pathfinder = new Pathfinder(nodeHandler);

            var tStart = DateTime.UtcNow;

            var route = pathfinder.Invoke();

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
            INode prev = null;
            foreach (var node in route.Cast<Node>())
            {
                var system = node.StarSystem;

                Console.WriteLine("  - name: {0}", system.Name);

                if (system.DistanceToNeutron != 0)
                {
                    Console.WriteLine("    neutron: true");
                }

                if (node.Refuel.HasValue)
                {
                    Console.WriteLine("    scoopable: {0}", node.Jumps == 1);

                    if (node.Jumps < 2)
                    {
                        var fuel = (node.Refuel.Value.Min + node.Refuel.Value.Max) / 2;
                        Console.WriteLine("    fuel: {0:0.00}", fuel);
                    }
                    else
                    {
                        var fuel = (node.Fuel.Min + node.Fuel.Max) / 2;
                        Console.WriteLine("    fuel: {0:0.00}", fuel);
                    }
                }
                else if (system.HasScoopable && system.DistanceToScoopable == 0)
                {
                    Console.WriteLine("    scoopable: {0}", false);
                }

                if (prev != null)
                {
                    Console.WriteLine("    time: {0:0}", pathfinder.GetDistance(prev, node));
                }

                prev = node;
            }
        }
    }
}
