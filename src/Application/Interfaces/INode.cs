using EliteBuckyball.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace EliteBuckyball.Application.Interfaces
{
    public interface INode
    {

        public object Id { get; }

        public StarSystem StarSystem { get; }

        public bool IsGoal { get; }

    }
}
