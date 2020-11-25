using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace ConsoleApp.LoadGalaxy
{
    public class StarSystemDto
    {

        [JsonPropertyName("id64")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("coords")]
        public CoordinatesDto Coordinates { get; set; }

        [JsonPropertyName("date")]
        public string Date { get; set; }

        [JsonPropertyName("bodies")]
        public List<BodyDto> Bodies { get; set; }

    }

    public class CoordinatesDto
    {

        [JsonPropertyName("x")]
        public float X { get; set; }

        [JsonPropertyName("y")]
        public float Y { get; set; }

        [JsonPropertyName("z")]
        public float Z { get; set; }

    }

    public class BodyDto
    {

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("subType")]
        public string SubType { get; set; }

        [JsonPropertyName("distanceToArrival")]
        public float DistanceToArrival { get; set; }

    }
}
