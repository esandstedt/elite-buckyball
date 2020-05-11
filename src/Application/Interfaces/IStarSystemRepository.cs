using EliteBuckyball.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EliteBuckyball.Application.Interfaces
{
    public interface IStarSystemRepository
    {

        StarSystem Get(string name);

        IEnumerable<StarSystem> GetNeighbors(StarSystem system, double distance);

    }
}
