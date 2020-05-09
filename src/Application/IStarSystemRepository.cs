using EliteBuckyball.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EliteBuckyball.Application
{
    public interface IStarSystemRepository
    {

        Task<StarSystem> GetAsync(string name);

        Task<IList<StarSystem>> GetNeighborsAsync(StarSystem system, double distance);

    }
}
