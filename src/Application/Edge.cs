using EliteBuckyball.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace EliteBuckyball.Application
{
    public class Edge : IEdge
    {
        public INode From { get; }
        public INode To { get; }
        public double Distance { get; }
        public int Jumps { get; }

        public Edge(INode from, INode to, double distance, int jumps)
        {
            this.From = from;
            this.To = to;
            this.Distance = distance;
            this.Jumps = jumps;
        }
    }
}
