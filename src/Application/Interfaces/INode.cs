﻿using EliteBuckyball.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace EliteBuckyball.Application.Interfaces
{
    public interface INode
    {

        public string Id { get; }

        public StarSystem StarSystem { get; }

    }
}