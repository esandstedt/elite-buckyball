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
using System.Threading.Tasks;

namespace EliteBuckyball.ConsoleApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
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

            var repository = serviceProvider.GetService<IStarSystemRepository>();

            var ship = new Ship
            {
                Name = "DSV Phoenix (Bucky 2)",
                DryMass = 482,
                FuelCapacity = 128,
                FSD = new FrameShiftDrive
                {
                    FuelPower = 2.6,
                    FuelMultiplier = 0.012,
                    MaxFuelPerJump = 8,
                    OptimisedMass = 2902
                },
                GuardianBonus = 10.5,
                FuelScoopRate = 1.245
            };

            var refuelLevels = new List<FuelRange>
            {
                new FuelRange(28,36),
                new FuelRange(44,52),
                new FuelRange(60,68),
                new FuelRange(68,76),
                new FuelRange(76,84),
                new FuelRange(84,92),
                new FuelRange(92,100),
                new FuelRange(100,108),
                new FuelRange(108,116),
                new FuelRange(116,124),
                new FuelRange(124,128),
            };

            /*
            var ship = new Ship
            {
                Name = "BBV Neutrino",
                DryMass = 34,
                FuelCapacity = 6,
                FSD = new FrameShiftDrive
                {
                    FuelPower = 2,
                    FuelMultiplier = 0.012,
                    MaxFuelPerJump = 1,
                    OptimisedMass = 140
                },
                GuardianBonus = 6.0,
                FuelScoopRate = 0.075
            };

            var refuelLevels = new List<FuelRange> { 
                new FuelRange(6, 6)
            };
             */

            /*
            var ship = new Ship
            {
                Name = "DSV Aurora",
                DryMass = 340,
                FuelCapacity = 32,
                FSD = new FrameShiftDrive
                {
                    FuelPower = 2.45,
                    FuelMultiplier = 0.012,
                    MaxFuelPerJump = 5,
                    OptimisedMass = 1693
                },
                GuardianBonus = 10.5,
                FuelScoopRate = 0.878
            };

            var refuelLevels = new List<FuelRange> { 
                new FuelRange(32,32)
            };
             */

            /*
            var ship = new Ship
            {
                Name = "DSV Phoenix (Exploration)",
                DryMass = 580,
                FuelCapacity = 96,
                FSD = new FrameShiftDrive
                {
                    FuelPower = 2.6,
                    FuelMultiplier = 0.012,
                    MaxFuelPerJump = 8,
                    OptimisedMass = 2902
                },
                GuardianBonus = 10.5,
                FuelScoopRate = 1.245
            };

            var refuelLevels = new List<FuelRange>
            {
                new FuelRange(96,96)
            };
             */

            var start = repository.Get("3 Capricorni");
            var goal = repository.Get("Phua Aub QT-W b1-4");

            var nodeHandler = new NodeHandler(
                repository,
                new List<IEdgeConstraint>
                {
                    new MinimumDistanceEdgeConstraint(3 * ship.GetJumpRange()),
                    new AngleEdgeConstraint(goal, 60),
                    new CylinderEdgeConstraint(start, goal, 2000),
                    new MaximumJumpsEdgeConstraint(2),
                },
                ship,
                refuelLevels,
                start,
                goal,
                true,
                500
            );

            var tStart = DateTime.UtcNow;

            var route = new Pathfinder(nodeHandler).Invoke();

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
            foreach (var node in route.Cast<Node>())
            {
                Console.WriteLine("  - name: {0}", node.StarSystem.Name);

                if (node.StarSystem.DistanceToNeutron != 0)
                {
                    Console.WriteLine("    neutron: true");
                }

                var hasPrimaryScoopable = node.StarSystem.HasScoopable && Math.Abs(node.StarSystem.DistanceToScoopable) < 1e-6;

                if (node.Refuel == null)
                {
                    if (hasPrimaryScoopable)
                    {
                        Console.WriteLine("    scoopable: false");
                    }
                }
                else 
                {
                    if (node.StarSystem.HasScoopable)
                    {
                        if (node.Jumps == 1)
                        {
                            Console.WriteLine("    scoopable: true");
                        }
                        else
                        {
                            Console.WriteLine("    scoopable: false");
                        }
                    }

                    var fuel = (node.Fuel.Min + node.Fuel.Max) / 2;
                    Console.WriteLine("    fuel: {0:0.00}", fuel);
                }
                
            }
        }
    }
}
