using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace EliteBuckyball.Infrastructure
{
    public struct EdsmSystem 
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }
        [JsonPropertyName("id64")]
        public long? Id64 { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("date")]
        public string Date { get; set; }
        [JsonPropertyName("coords")]
        public EdsmSystemCoordinates Coords { get; set; }
    }

    public struct EdsmSystemCoordinates
    {
        [JsonPropertyName("x")]
        public float X { get; set; }
        [JsonPropertyName("y")]
        public float Y { get; set; }
        [JsonPropertyName("z")]
        public float Z { get; set; }
    }
}
