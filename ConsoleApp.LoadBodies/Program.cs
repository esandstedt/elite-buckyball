using EliteBuckyball.Application.Interfaces;
using EliteBuckyball.Infrastructure;
using EliteBuckyball.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace ConsoleApp.LoadBodies
{
    class Program
    {
        static void Main(string[] args)
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
                .AddSingleton<IConfiguration>(configuration)
                .AddTransient<IStarSystemRepository, StarSystemRepository>()
                .AddTransient<App>()
                .BuildServiceProvider();

            serviceProvider.GetService<App>().Invoke();
        }
    }
}
