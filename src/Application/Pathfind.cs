using EliteBuckyball.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EliteBuckyball.Application
{

    public class Pathfind
    {

        private readonly IStarSystemRepository starSystemRepository;
        private readonly Ship ship;
        private readonly StarSystem start;
        private readonly StarSystem goal;

        private Dictionary<string, double> g;
        private Dictionary<string, double> f;
        private Dictionary<string, string> cameFrom;
        private PriorityQueue<StarSystem> open;

        public Pathfind(
            IStarSystemRepository starSystemRepository,
            Ship ship,
            StarSystem start,
            StarSystem goal)
        {
            this.starSystemRepository = starSystemRepository;
            this.ship = ship;
            this.start = start;
            this.goal = goal;

            this.g = new Dictionary<string, double>();
            this.f = new Dictionary<string, double>();
            this.cameFrom = new Dictionary<string, string>();
            this.open = new PriorityQueue<StarSystem>();
        }

        public async Task<List<string>> InvokeAsync()
        {
            this.Enqueue(this.start, 0);

            var i = 0;
            while (this.open.Any())
            {
                i += 1;

                var (current, f) = this.open.Dequeue();

                Console.WriteLine("{0,8} {1,8} {2,8} {3,6} {4,6} {5,6}   {6}",
                    i,
                    this.open.Count,
                    this.cameFrom.Count,
                    (int)this.f[current.Name],
                    (int)this.g[current.Name],
                    (int)Distance(current, this.goal),
                    current.Name
                );

                if (current.Id == goal.Id)
                {
                    return this.GenerateRoute();
                }

                var neighbors = await this.starSystemRepository.GetNeighborsAsync(current, 500);
                foreach (var neighbor in neighbors)
                {
                    var g = this.g[current.Name] + this.Distance(current, neighbor);

                    if (g < this.g.GetValueOrDefault(neighbor.Name, double.MaxValue))
                    {
                        this.cameFrom[neighbor.Name] = current.Name;
                        this.Enqueue(neighbor, g);
                    }
                }
            }

            return new List<string>();
        }

        private double Distance(StarSystem a, StarSystem b)
        {
            return Math.Sqrt(
                Math.Pow(a.X - b.X, 2) + 
                Math.Pow(a.Y - b.Y, 2) + 
                Math.Pow(a.Z - b.Z, 2)
            ); 
        }

        private void Enqueue(StarSystem system, double g)
        {
            this.g[system.Name] = g;
            var f = g + this.Distance(system, this.goal);
            this.f[system.Name] = f;
            this.open.Enqueue(system, f);
        }

        private List<string> GenerateRoute()
        {
            var result = new List<string>();

            var current = this.goal.Name;
            while (this.cameFrom.ContainsKey(current))
            {
                result.Insert(0, current);
                current = this.cameFrom[current];
            }

            result.Insert(0, current);

            return result;
        }

    }
}
