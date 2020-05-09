using EliteBuckyball.Application;
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
                .AddTransient<IStarSystemRepository, StarSystemRepository>()
                .BuildServiceProvider();

            var repository = serviceProvider.GetService<IStarSystemRepository>();

            var start = await repository.GetAsync("Prue Phreia QI-R d5-0");
            var goal = await repository.GetAsync("Dryeekoo HL-W d2-0");

            var pathfind = new Pathfind(repository, null, start, goal);

            var route = await pathfind.InvokeAsync();

            Console.WriteLine();
            foreach (var item in route)
            {
                Console.WriteLine(item);
            }
        }
    }
}
