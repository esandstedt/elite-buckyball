using EliteBuckyball.Application.Interfaces;
using EliteBuckyball.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace EliteBuckyball.Application
{
    public struct Node : INode
    {

        public object Id { get; }

        public StarSystem StarSystem { get; }

        public FuelRange Fuel { get; }

        public FuelRange? Refuel { get; }

        public int Jumps { get; }

        public Node(object id, StarSystem system, FuelRange fuel, FuelRange? refuel, int jumps)
        {
            this.Id = id;
            this.StarSystem = system;
            this.Fuel = fuel;
            this.Refuel = refuel;
            this.Jumps = jumps;
        }

        public override int GetHashCode()
        {
            return this.Id.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is Node)
            {
                return Equals((Node)obj);
            }

            return false;
        }

        public bool Equals(Node that)
        {
            return this.Id.Equals(that.Id);
        }

        public override string ToString()
        {
            return this.StarSystem.Name;
        }

    }
}
