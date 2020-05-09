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

            // Hillary Depot - Blu Thua AI-A c14-10

            var start = await repository.GetAsync("Sol");
            var goal = await repository.GetAsync("Sagittarius A*");


            var pathfinder = new Pathfinder(
                new NodeHandler(repository, goal, 68.54),
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
            foreach (var item in route)
            {
                Console.WriteLine("  - name: {0}", item);
            }
        }
    }
}
