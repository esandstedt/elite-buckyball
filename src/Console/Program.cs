using EliteBuckyball.Application;
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
using System.Net.Mime;
using System.Threading.Tasks;

namespace EliteBuckyball.ConsoleApp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            var serviceProvider = new ServiceCollection()
                .AddLogging()
                .AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseMySql(configuration.GetConnectionString("Default"));
                })
                .AddTransient<INodeHandler, NodeHandler>()
                .AddTransient<IStarSystemRepository, StarSystemRepository>()
                .BuildServiceProvider();

            var repository = serviceProvider.GetService<IStarSystemRepository>();

            var ship = new Ship
            {
                Name = "DSV Phoenix (Bucky)",
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

            var refuelLevels = new List<double> { 6 };
             */

            var start = repository.Get("Lagoon Sector FW-W d1-122");
            var goal = repository.Get("Rohini");

            var nodeHandler = new CylinderConstraintNodeHandler(
                new BacktrackingConstraintNodeHandler(
                    new NodeHandler(repository, ship, refuelLevels, start, goal),
                    goal
                ),
                start,
                goal
            );

            var pathfinder = new Pathfinder(
                nodeHandler,
                start,
                goal
            );

            var tStart = DateTime.UtcNow;

            var route = await pathfinder.InvokeAsync();

            var tEnd = DateTime.UtcNow;

            Console.WriteLine();
            Console.WriteLine("Time: {0}", (tEnd - tStart));

            Console.WriteLine();
            Console.WriteLine("route:");
            foreach (var node in route.Cast<NodeHandler.Node>())
            {
                Console.WriteLine("  - name: {0}", node.StarSystem.Name);

                if (node.StarSystem.DistanceToNeutron != 0)
                {
                    Console.WriteLine("    neutron: true");
                }

                if (node.Refuel != null)
                {
                    if (node.StarSystem.HasScoopable)
                    {
                        Console.WriteLine("    scoopable: true");
                    }

                    var fuel = (node.Fuel.Min + node.Fuel.Max) / 2;
                    Console.WriteLine("    fuel: {0:0.00}", fuel);
                }
            }
        }
    }
}
