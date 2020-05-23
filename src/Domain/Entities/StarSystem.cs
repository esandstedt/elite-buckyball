using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace EliteBuckyball.Domain.Entities
{
    public class StarSystem
    {

        public long Id { get; set; }

        public string Name { get; set; }

        public Vector3 Coordinates { get; set; }

        public bool HasNeutron { get; set; }

        public float DistanceToNeutron { get; set; }

        public bool HasScoopable { get; set; }

        public float DistanceToScoopable { get; set; }

        public DateTime Date { get; set; }

        public override int GetHashCode()
        {
            return this.Id.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var that = obj as StarSystem;
            return that != null && this.Id.Equals(that.Id);
        }

    }
}
