using System;
using System.Collections.Generic;
using System.Text;

namespace EliteBuckyball.Infrastructure.Repository
{
    public class Coordinate
    {
        public int X { get;  }
        public int Y { get;  }
        public int Z { get;  }

        public Coordinate(int x, int y, int z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }
    }
}
