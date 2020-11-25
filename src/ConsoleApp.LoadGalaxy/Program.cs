using EliteBuckyball.Application.Interfaces;
using EliteBuckyball.Infrastructure;
using EliteBuckyball.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace ConsoleApp.LoadGalaxy
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
                .AddSingleton<IConfiguration>(configuration)
                .AddTransient<IStarSystemRepository, StarSystemRepository>()
                .AddSingleton(new StarSystemRepository.Options { Mode = StarSystemRepository.Mode.All })
                .AddTransient<App>()
                .BuildServiceProvider();

            serviceProvider.GetService<App>().Invoke();
        }
    }
}
