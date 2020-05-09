using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace EliteBuckyball.Infrastructure.Persistence.Entities
{
    [Table("system")]
    public class StarSystem
    {

        [Column("id")]
        public long Id { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("x")]
        public float X { get; set; }

        [Column("y")]
        public float Y { get; set; }

        [Column("z")]
        public float Z { get; set; }

        [Column("sectorX")]
        public int SectorX { get; set; }

        [Column("sectorY")]
        public int SectorY { get; set; }

        [Column("sectorZ")]
        public int SectorZ { get; set; }

        [Column("distanceToNeutron")]
        public int? DistanceToNeutron { get; set; }

        [Column("distanceToScoopable")]
        public int? DistanceToScoopable { get; set; }

        [Column("date")]
        public DateTime? Date { get; set; }

    }
}
