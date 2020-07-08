using EliteBuckyball.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EliteBuckyball.Application.Interfaces
{
    public interface IStarSystemRepository
    {

        StarSystem Get(long id);

        StarSystem GetByName(string name);

        bool Exists(long id);

        ISet<long> Exists(IEnumerable<long> ids);

        IEnumerable<StarSystem> GetNeighbors(StarSystem system, double distance);

        void Create(StarSystem system);
    }
}
