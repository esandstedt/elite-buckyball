using System;
using System.Collections.Generic;
using System.Text;

namespace EliteBuckyball.Domain
{
    public class StarSystem
    {
        public string Name { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public bool HasNeutron { get; set; }
        public float DistanceToNeutron { get; set; }
        public bool HasScoopable { get; set; }
        public float DistanceToScoopable { get; set; }
    }
}
