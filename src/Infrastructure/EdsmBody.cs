using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace EliteBuckyball.Infrastructure
{
    public struct EdsmBody
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("subType")]
        public string SubType { get; set; }

        [JsonPropertyName("distanceToArrival")]
        public int DistanceToArrival { get; set; }

        [JsonPropertyName("systemId64")]
        public long SystemId64 { get; set; }
    }
}
