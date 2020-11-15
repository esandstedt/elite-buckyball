using EliteBuckyball.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace EliteBuckyball.Application
{
    public class Edge : IEdge
    {
        public INode From { get; set; }
        public INode To { get; set; }
        public double Distance { get; set; }
        public int Jumps { get; set; }

        public Edge() { }

        public Edge(INode from, INode to, double distance, int jumps)
        {
            this.From = from;
            this.To = to;
            this.Distance = distance;
            this.Jumps = jumps;
        }
    }
}
