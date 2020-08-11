﻿using EliteBuckyball.Application.Interfaces;
using EliteBuckyball.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace EliteBuckyball.Application
{
    public class Pathfinder
    {

        private readonly INodeHandler nodeHandler;

        private Dictionary<INode, double> g;
        private Dictionary<INode, double> f;
        private Dictionary<INode, INode> cameFrom;
        private PriorityQueue<INode> open;

        public Pathfinder(INodeHandler nodeHandler)
        {
            this.nodeHandler = nodeHandler;

            this.g = new Dictionary<INode, double>();
            this.f = new Dictionary<INode, double>();
            this.cameFrom = new Dictionary<INode, INode>();
            this.open = new PriorityQueue<INode>();
        }

        public List<INode> Invoke()
        {
            foreach (var node in this.nodeHandler.GetInitialNodes())
            {
                this.Enqueue(node, 0);
            }

            double closestDistance = double.MaxValue;
            INode closest = null;

            var i = 0;
            while (this.open.Any())
            {
                var (current, f) = this.open.Dequeue();

                if (this.f[current] < f)
                {
                    continue;
                }

                i += 1;

                var distance = this.nodeHandler.GetShortestDistanceToGoal(current);

                if (distance < closestDistance)
                {
                    closest = current;
                    closestDistance = distance;
                }

                Console.WriteLine("{0,8} {1,8} {2,8} | {3,6} {4,6} | {5,6} {6,6} {7,6}   {8}",
                    i,
                    this.open.Count,
                    this.cameFrom.Count,
                    TimeSpan.FromSeconds((int)this.g[closest]),
                    TimeSpan.FromSeconds((int)closestDistance),
                    TimeSpan.FromSeconds((int)this.f[current]),
                    TimeSpan.FromSeconds((int)this.g[current]),
                    TimeSpan.FromSeconds((int)distance),
                    current
                );

                if (current.IsGoal)
                {
                    return this.GenerateRoute(current);
                }

                foreach (var edge in this.nodeHandler.GetEdges(current))
                {
                    var g = this.g[edge.From] + edge.Distance;

                    if (g < this.g.GetValueOrDefault(edge.To, double.MaxValue))
                    {
                        this.cameFrom[edge.To] = edge.From;
                        this.Enqueue(edge.To, g);
                    }
                }
            }

            return this.GenerateRoute(closest);
        }

        private void Enqueue(INode node, double g)
        {
            this.g[node] = g;
            var f = g + this.nodeHandler.GetShortestDistanceToGoal(node);
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

        public double GetDistance(INode from, INode to)
        {
            if (!this.cameFrom[to].Equals(from))
            {
                throw new ArgumentException();
            }

            return this.g[to] - this.g[from];
        }

    }
}
