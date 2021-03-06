﻿using System;
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

        public int DistanceToNeutron { get; set; }

        public bool HasScoopable { get; set; }

        public int DistanceToScoopable { get; set; }

        public bool HasStation { get; set; }

        public int DistanceToStation { get; set; }

        public bool HasWhiteDwarf { get; set; }

        public int DistanceToWhiteDwarf { get; set; }

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
