using EliteBuckyball.Application.Interfaces;
using EliteBuckyball.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace EliteBuckyball.Application
{
    public class Node : INode
    {

        public object Id { get; }

        public StarSystem StarSystem { get; }

        public FuelRange Fuel { get; }

        public FuelRange Refuel { get; }

        public Node(object id, StarSystem system, FuelRange fuel, FuelRange refuel)
        {
            this.Id = id;
            this.StarSystem = system;
            this.Fuel = fuel;
            this.Refuel = refuel;
        }

        public override int GetHashCode()
        {
            return this.Id.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Node);
        }

        public bool Equals(Node that)
        {
            return that != null && this.Id.Equals(that.Id);
        }

        public override string ToString()
        {
            return this.StarSystem.Name;
        }

    }
}
