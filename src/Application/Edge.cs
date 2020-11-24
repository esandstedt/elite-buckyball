using EliteBuckyball.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace EliteBuckyball.Application
{
    public class Edge : IEdge
    {
        public Node From { get; set; }
        INode IEdge.From => this.From;

        public Node To { get; set; }
        INode IEdge.To => this.To;

        public double Distance { get; set; }
        public int Jumps { get; set; }

        public Edge() { }

        public Edge(Node from, Node to, double distance, int jumps)
        {
            this.From = from;
            this.To = to;
            this.Distance = distance;
            this.Jumps = jumps;
        }
    }
}
