using EliteBuckyball.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace EliteBuckyball.Application
{
    public struct Edge : IEdge
    {

        public INode From { get; set; }

        public INode To { get; set; }

        public double Distance { get; set; }

    }
}
