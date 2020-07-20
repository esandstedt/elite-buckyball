using EliteBuckyball.Application.Interfaces;
using EliteBuckyball.Domain.Entities;
using EliteBuckyball.Infrastructure;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace ConsoleApp.LoadSystems
{
    public class App
    {
        private readonly IConfiguration configuration;
        private readonly IStarSystemRepository repository;

        private int created;
        private int total;

        public App(
            IConfiguration configuration,
            IStarSystemRepository repository)
        {
            this.configuration = configuration;
            this.repository = repository;
        }

        public void Invoke()
        {
            var systems = new DumpFileReader<EdsmSystem>(configuration.GetValue<string>("InputPath"))
                .Where(x => x.Id64.HasValue)
                .ToList();

            this.created = 0;
            this.total = 0;

            foreach (var partition in systems.Partition(1000))
            {
                Handle(partition);
            }

            Console.WriteLine("Created: {0}", this.created);
            Console.WriteLine("Total: {0}", this.total);
        }

        private void Handle(List<EdsmSystem> systems)
        {
            var existingIds = repository.Exists(systems.Select(x => x.Id64.Value).ToList());

            foreach (var system in systems)
            {
                var id = system.Id64.Value;

                this.total += 1;

                if (existingIds.Contains(id))
                {
                    continue;
                }

                this.created += 1;
                Console.WriteLine("{0,8} {1}", this.total, system.Name);

                repository.Create(new StarSystem
                {
                    Id = id,
                    Name = system.Name,
                    Coordinates = new Vector3(
                        system.Coords.X,
                        system.Coords.Y,
                        system.Coords.Z
                    ),
                    Date = DateTime.Parse(system.Date).Date,
                    HasNeutron = false,
                    HasScoopable = false,
                });
            }
        }
    }
}
