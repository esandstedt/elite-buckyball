﻿using EliteBuckyball.Application;
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
                FuelCapacity = 64,
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

            var start = await repository.GetAsync("Sol");
            var goal = await repository.GetAsync("Rohini");

            var nodeHandler = new CylinderConstraintNodeHandler(
                new BacktrackingConstraintNodeHandler(
                    new NodeHandler(repository, ship, goal),
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
            foreach (var node in route)
            {
                Console.WriteLine("  - name: {0}", node.StarSystem.Name);

                if (node.StarSystem.DistanceToNeutron != 0)
                {
                    Console.WriteLine("    neutron: true");
                }

                Console.WriteLine("    fuel: {0}", (int)((NodeHandler.Node)node).Fuel);
            }
        }
    }
}
