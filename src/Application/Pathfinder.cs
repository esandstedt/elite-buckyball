﻿using EliteBuckyball.Application.Interfaces;
using EliteBuckyball.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EliteBuckyball.Application
{
    public class Pathfinder
    {

        private readonly INodeHandler nodeHandler;
        private readonly INode start;
        private readonly StarSystem goal;

        private Dictionary<INode, double> g;
        private Dictionary<INode, double> f;
        private Dictionary<INode, INode> cameFrom;
        private PriorityQueue<INode> open;

        public Pathfinder(
            INodeHandler nodeHandler,
            StarSystem start,
            StarSystem goal)
        {
            this.nodeHandler = nodeHandler;
            this.start = nodeHandler.Create(start);
            this.goal = goal;

            this.g = new Dictionary<INode, double>();
            this.f = new Dictionary<INode, double>();
            this.cameFrom = new Dictionary<INode, INode>();
            this.open = new PriorityQueue<INode>();
        }

        public async Task<List<INode>> InvokeAsync()
        {
            this.Enqueue(this.start, 0);

            var i = 0;
            while (this.open.Any())
            {
                i += 1;

                var (current, f) = this.open.Dequeue();

                if (this.f[current] < f)
                {
                    continue;
                }

                Console.WriteLine("{0,8} {1,8} {2,8} {3,6} {4,6} {5,6}   {6}",
                    i,
                    this.open.Count,
                    this.cameFrom.Count,
                    (int)this.f[current],
                    (int)this.g[current],
                    (int)(((Vector)current.StarSystem).Distance((Vector)this.goal)),
                    current
                );

                if (current.StarSystem.Equals(goal))
                {
                    return this.GenerateRoute(current);
                }

                var edges = await this.nodeHandler.GetEdges(current);
                foreach (var edge in edges)
                {
                    var g = this.g[edge.From] + edge.Distance;

                    if (g < this.g.GetValueOrDefault(edge.To, double.MaxValue))
                    {
                        this.cameFrom[edge.To] = edge.From;
                        this.Enqueue(edge.To, g);
                    }
                }
            }

            return new List<INode>();
        }

        private void Enqueue(INode node, double g)
        {
            this.g[node] = g;
            var f = g + this.nodeHandler.GetShortestDistance(node, this.goal);
            this.f[node] = f;
            this.open.Enqueue(node, f);
        }

        private List<INode> GenerateRoute(INode current)
        {
            var result = new List<INode>();

            while (this.cameFrom.ContainsKey(current))
            {
                result.Insert(0, current);
                current = this.cameFrom[current];
            }

            result.Insert(0, current);

            return result;
        }

    }
}
