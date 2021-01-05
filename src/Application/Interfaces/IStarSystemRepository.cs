using EliteBuckyball.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace EliteBuckyball.Application.Interfaces
{
    public interface IStarSystemRepository
    {

        StarSystem Get(long id);

        StarSystem GetByName(string name);

        bool Exists(long id);

        ISet<long> Exists(List<long> ids);

        IEnumerable<StarSystem> GetNeighbors(StarSystem system, double distance);

        IEnumerable<StarSystem> GetNeighbors(Vector3 position, double distance);

        void Create(StarSystem system);

        void CreateMany(IEnumerable<StarSystem> system);

        void Update(StarSystem system);

        void Update(List<StarSystem> system);
    }
}
