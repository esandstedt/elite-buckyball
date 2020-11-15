using EliteBuckyball.Application.Interfaces;
using EliteBuckyball.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace EliteBuckyball.Application
{
    public class Node : INode
    {
        public (long, ushort) Id { get; set; }
        public StarSystem StarSystem { get; set; }
        public bool IsGoal { get; set; }
        public double FuelMin { get; set; }
        public double FuelMax { get; set; }
        public RefuelType RefuelType { get; set; }
        public double? RefuelMin { get; set; }
        public double? RefuelMax { get; set; }
        public int Jumps { get; set; }

        object INode.Id => this.Id;
        public double FuelAvg => (this.FuelMin + this.FuelMax) / 2;

        public Node() { }

        public Node(
            (long, ushort) id,
            StarSystem system,
            bool isGoal,
            double fuelMin,
            double fuelMax, 
            RefuelType refuelType,
            double? refuelMin,
            double? refuelMax,
            int jumps)
        {
            this.Id = id;
            this.StarSystem = system;
            this.IsGoal = isGoal;
            this.FuelMin = fuelMin;
            this.FuelMax = fuelMax;
            this.RefuelType = refuelType;
            this.RefuelMin = refuelMin;
            this.RefuelMax = refuelMax;
            this.Jumps = jumps;
        }

        public override int GetHashCode() => this.Id.GetHashCode();

        public override bool Equals(object obj)
        {
            if (obj is Node)
            {
                return Equals((Node)obj);
            }

            return false;
        }

        private bool Equals(Node that)
        {
            return this.Id.Equals(that.Id);
        }

        public override string ToString()
        {
            return this.StarSystem.Name;
        }

    }
}
