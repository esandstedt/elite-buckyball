﻿using EliteBuckyball.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EliteBuckyball.Application.Interfaces
{
    public interface INodeHandler
    {
        List<INode> GetInitialNodes();

        double GetShortestDistanceToGoal(INode a);

        Task<List<IEdge>> GetEdges(INode node);
    }
}
