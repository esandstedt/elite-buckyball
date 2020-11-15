using EliteBuckyball.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EliteBuckyball.Application.EdgeConstraints
{
    public class ExcludeEdgeConstraint : BaseEdgeConstraint
    {
        private readonly Dictionary<string, HashSet<string>> data;

        public ExcludeEdgeConstraint(List<(string, string)> systemPairs)
        {
            this.data = systemPairs
                .GroupBy(x => x.Item1)
                .ToDictionary(g => g.Key, g => new HashSet<string>(g.Select(x => x.Item2)));
        }

        public override bool ValidBefore(StarSystem from, StarSystem to)
        {
            if (this.data.ContainsKey(from.Name))
            {
                return !this.data[from.Name].Contains(to.Name);
            }

            return true;
        }
    }
}
