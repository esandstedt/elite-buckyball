using EliteBuckyball.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace EliteBuckyball.Application.EdgeConstraints
{
    public class ExcludeEdgeConstraint : BaseEdgeConstraint
    {
        private readonly ISet<string> systemNames;

        public ExcludeEdgeConstraint(List<string> systemNames)
        {
            this.systemNames = new HashSet<string>(systemNames);
        }

        public override bool ValidBefore(StarSystem from, StarSystem to)
        {
            return !this.systemNames.Contains(to.Name);
        }
    }
}
