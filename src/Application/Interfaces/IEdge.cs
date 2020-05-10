using System;
using System.Collections.Generic;
using System.Text;

namespace EliteBuckyball.Application.Interfaces
{
    public interface IEdge
    {
        INode From { get; }

        INode To { get; }

        double Distance { get; }
    }
}
