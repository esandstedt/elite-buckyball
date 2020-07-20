using EliteBuckyball.Application.Interfaces;
using EliteBuckyball.Domain.Entities;
using EliteBuckyball.Infrastructure;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace ConsoleApp.LoadBodies
{
    public class App
    {
        private static List<string> SCOOPABLE_SUB_TYPES = new List<string>
        {
            "O (Blue-White) Star",
            "B (Blue-White) Star",
            "A (Blue-White super giant) Star",
            "A (Blue-White) Star",
            "F (White) Star",
            "G (White-Yellow super giant) Star",
            "G (White-Yellow) Star",
            "K (Yellow-Orange giant) Star",
            "K (Yellow-Orange) Star",
            "M (Red dwarf) Star",
            "M (Red giant) Star",
            "M (Red super giant) Star"
        };

        private IConfiguration configuration;
        private IStarSystemRepository repository;

        public App(
            IConfiguration configuration,
            IStarSystemRepository repository)
        {
            this.configuration = configuration;
            this.repository = repository;
        }

        public void Invoke()
        {
            var bodies = new DumpFileReader<EdsmBody>(configuration.GetValue<string>("InputPath"))
                .Where(x => x.Type == "Star")
                .Where(x => x.DistanceToArrival <= short.MaxValue)
                .ToList();

            var systemsToUpdate = new List<StarSystem>();

            var c = 0;
            foreach (var body in bodies)
            {
                var isNeutron = body.SubType == "Neutron Star";
                var isScoopable = SCOOPABLE_SUB_TYPES.Contains(body.SubType);

                if (!isNeutron && !isScoopable)
                {
                    continue;
                }

                var system = repository.Get(body.SystemId64);

                if (system == null)
                {
                    continue;
                }

                if (isNeutron)
                {
                    c += 1;
                    Console.WriteLine("{0,9} {1}", c, body.Name);

                    if (!system.HasNeutron || body.DistanceToArrival < system.DistanceToNeutron)
                    {
                        system.HasNeutron = true;
                        system.DistanceToNeutron = body.DistanceToArrival;
                        systemsToUpdate.Add(system);
                    }
                }

                if (isScoopable)
                {
                    c += 1;
                    Console.WriteLine("{0,9} {1}", c, body.Name);

                    if (!system.HasScoopable || body.DistanceToArrival < system.DistanceToScoopable)
                    {
                        system.HasScoopable = true;
                        system.DistanceToScoopable = body.DistanceToArrival;
                        systemsToUpdate.Add(system);
                    }
                }

                if (1000 <= systemsToUpdate.Count) {
                    repository.Update(systemsToUpdate);
                    systemsToUpdate.Clear();
                }
            }

            if (systemsToUpdate.Any())
            {
                repository.Update(systemsToUpdate);
            }
        }
    }
}
