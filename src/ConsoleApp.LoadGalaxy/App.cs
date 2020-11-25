using EliteBuckyball.Application.Interfaces;
using EliteBuckyball.Domain.Entities;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json;

namespace ConsoleApp.LoadGalaxy
{
    public class App
    {

        private readonly IConfiguration configuration;
        private readonly IStarSystemRepository starSystemRepository;

        private static readonly HashSet<string> SCOOPABLE_SUBTYPES = new HashSet<string>(new List<string>
        {
            "O (Blue-White) Star",
            "B (Blue-White super giant) Star",
            "B (Blue-White) Star",
            "A (Blue-White super giant) Star",
            "A (Blue-White) Star",
            "F (White super giant) Star",
            "F (White) Star",
            "G (White-Yellow super giant) Star",
            "G (White-Yellow) Star",
            "K (Yellow-Orange giant) Star",
            "K (Yellow-Orange) Star",
            "M (Red super giant) Star",
            "M (Red giant) Star",
            "M (Red dwarf) Star"
        });

        public App(
            IConfiguration configuration,
            IStarSystemRepository starSystemRepository)
        {
            this.configuration = configuration;
            this.starSystemRepository = starSystemRepository;
        }

        public void Invoke()
        {
            var filePath = this.configuration.GetValue<string>("InputPath");

            using var fileStream = new FileStream(filePath, FileMode.Open);
            using var zipStream = new GZipStream(fileStream, CompressionMode.Decompress);
            using var streamReader = new StreamReader(zipStream);
            {
                // Skip the first line
                streamReader.ReadLine();

                var tStart = DateTime.Now;

                int i = 0;
                int j = 0;
                var systems = new List<StarSystem>();

                string line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    if (this.TryDeserialize(line, out var result))
                    {
                        i += 1;

                        // Restricted to SagA* route
                        var coords = result.Coordinates;
                        if (-1500 < coords.X && coords.X < 500 &&
                            -1500 < coords.Y && coords.Y < 500 &&
                             -500 < coords.Z && coords.Z < 26000)
                        {
                            var neutron = result.Bodies
                                .Where(x => x.DistanceToArrival < 100)
                                .OrderBy(x => x.DistanceToArrival)
                                .FirstOrDefault(x => x.SubType == "Neutron Star");

                            var scoopable = result.Bodies
                                .Where(x => x.DistanceToArrival < 100)
                                .OrderBy(x => x.DistanceToArrival)
                                .FirstOrDefault(x => SCOOPABLE_SUBTYPES.Contains(x.SubType));

                            if (neutron != null || scoopable != null)
                            {
                                var system = new StarSystem
                                {
                                    Id = result.Id,
                                    Coordinates = new Vector3(coords.X, coords.Y, coords.Z),
                                    Name = result.Name,
                                    Date = DateTime.Today,
                                    HasNeutron = neutron != null,
                                    DistanceToNeutron = neutron != null ? (int)neutron.DistanceToArrival : 0,
                                    HasScoopable = scoopable != null,
                                    DistanceToScoopable = scoopable != null ? (int)scoopable.DistanceToArrival : 0,
                                };

                                systems.Add(system);

                                j += 1;
                                Console.WriteLine("{0} {1,8} {2,8} {3}", 
                                    (DateTime.Now - tStart).ToString(@"hh\:mm\:ss"),
                                    i,
                                    j,
                                    system.Name
                                );

                                if (5000 <= systems.Count)
                                {
                                    this.starSystemRepository.CreateMany(systems);
                                    systems.Clear();
                                }
                            }
                        }
                    }
                }

                this.starSystemRepository.CreateMany(systems);
            }
        }

        private bool TryDeserialize(string line, out StarSystemDto result)
        {
            try
            {
                var dto = JsonSerializer.Deserialize<StarSystemDto>(line.Trim(','));

                dto.Bodies = dto.Bodies
                    .Where(x => x.Name.StartsWith(dto.Name))
                    .ToList();

                result = dto;
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }
    }
}
