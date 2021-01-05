using EliteBuckyball.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace EliteBuckyball.Application.EdgeConstraints
{
    public class BeagleEdgeConstraint : BaseEdgeConstraint
    {

        private List<CylinderEdgeConstraint> constraints = new List<CylinderEdgeConstraint>
        {
            new CylinderEdgeConstraint(
                new Vector3(0, 0, 0),
                new Vector3(-1000, -1000, 10000),
                1500
            ),
            new CylinderEdgeConstraint(
                new Vector3(-1000, -1000, 10000),
                new Vector3(-1000, -1000, 55000),
                250
            ),
            new CylinderEdgeConstraint(
                new Vector3(-1000, -1000, 55000),
                new Vector3(-1112, -134, 65270),
                1500
            )
        };

        public override bool ValidBefore(StarSystem from, StarSystem to)
        {
            if (to.Coordinates.Z < 10000)
            {
                return this.constraints[0].ValidBefore(from, to);
            }
            else if (to.Coordinates.Z < 55000)
            {
                return this.constraints[1].ValidBefore(from, to);
            }
            else
            {
                return this.constraints[2].ValidBefore(from, to);
            }
        }

    }
}
