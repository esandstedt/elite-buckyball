using EliteBuckyball.Application.Interfaces;
using EliteBuckyball.Infrastructure;
using EliteBuckyball.Infrastructure.Persistence;
using EliteBuckyball.Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace ConsoleApp.LoadSystems
{
    class Program
    {
        static void Main(string[] args)
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
                .AddSingleton<IConfiguration>(configuration)
                .AddTransient<IStarSystemRepository, StarSystemRepository>()
                .AddTransient<App>()
                .BuildServiceProvider();

            serviceProvider.GetService<App>().Invoke();
        }

    }
}
