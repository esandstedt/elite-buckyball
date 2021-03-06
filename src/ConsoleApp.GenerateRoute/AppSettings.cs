﻿using System;
using System.Collections.Generic;
using System.Text;

namespace EliteBuckyball.ConsoleApp.GenerateRoute
{
    public class AppSettings
    {
        public ShipSettings Ship { get; set; }
        public string Start { get; set; }
        public string Goal { get; set; }
        public List<EdgeConstraintSettings> EdgeConstraints { get; set; }
        public bool UseFsdBoost { get; set; }
        public bool UseRefuelStarFinder { get; set; }
        public bool NeutronBoostedAtStart { get; set; }
        public string RepositoryMode { get; set; }
        public int RepositorySectorSize { get; set; }
        public int NeighborRangeMin { get; set; }
    }

    public class ShipSettings
    {
        public string Name { get; set; }
        public double DryMass { get; set; }
        public double FuelCapacity { get; set; }
        public double FsdFuelPower { get; set; }
        public double FsdFuelMultiplier { get; set; }
        public double FsdMaxFuelPerJump { get; set; }
        public double FsdOptimisedMass { get; set; }
        public double GuardianBonus { get; set; }
        public double FuelScoopRate { get; set; }
        public List<RefuelSettings> RefuelLevels { get; set; }
    }

    public class RefuelSettings
    {
        public string RefuelType { get; set; }
        public double RefuelMin { get; set; }
        public double RefuelMax { get; set; }
        public int? JumpsMin { get; set; }
        public int? JumpsMax { get; set; }
        public double? MultiJumpRangeFactor { get; set; }
    }

    public class EdgeConstraintSettings
    {
        public string Type { get; set; }
        public Dictionary<string, string> Parameters { get; set; }
    }

}
