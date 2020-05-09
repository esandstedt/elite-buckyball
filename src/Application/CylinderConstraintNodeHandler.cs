﻿using EliteBuckyball.Application.Interfaces;
using EliteBuckyball.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace EliteBuckyball.Application
{
    public class CylinderConstraintNodeHandler : INodeHandler
    {
        private readonly INodeHandler handler;
        private readonly Vector start;
        private readonly Vector goal;

        private readonly Dictionary<INode, bool> cache;

        public CylinderConstraintNodeHandler(
            INodeHandler handler,
            StarSystem start,
            StarSystem goal)
        {
            this.handler = handler;
            this.start = (Vector)start;
            this.goal = (Vector)goal;

            this.cache = new Dictionary<INode, bool>();
        }
        public INode Create(StarSystem system)
        {
            return this.handler.Create(system);
        }

        public double Distance(INode a, INode b)
        {
            return this.handler.Distance(a, b);
        }

        public double ShortestDistance(INode a, INode b)
        {
            return this.handler.ShortestDistance(a, b);
        }

        public async Task<List<INode>> Neighbors(INode node)
        {
            return (await this.handler.Neighbors(node))
                .Where(node => {
                    if (!this.cache.ContainsKey(node))
                    {
                        this.cache[node] = this.DistanceFromCenterLine(node) < 2000;
                    }

                    return this.cache[node];
                })
                .ToList();
        }

        private double DistanceFromCenterLine(INode node)
        {
            // https://mathworld.wolfram.com/Point-LineDistance3-Dimensional.html

            var x0 = (Vector)node.StarSystem;
            var x1 = this.start;
            var x2 = this.goal;

            var x1m0 = x1.Subtract(x0);
            var x2m1 = x2.Subtract(x1);

            var t = -1 * x1m0.Dot(x2m1) / Math.Pow(x2m1.Magnitude(), 2);
            if (t < 0)
            {
                return x0.Distance(x1);
            }
            else if (t < 1)
            {
                var x3 = x1.Add(x2m1.Multiply(t));
                return x0.Distance(x3);
            }
            else
            {
                return x0.Distance(x2);
            }
        }
    }
}
