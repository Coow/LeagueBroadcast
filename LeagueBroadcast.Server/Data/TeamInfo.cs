using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Server.Data
{
    public class TeamInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("nameTag")]
        public string NameTag { get; set; } = "";

        [JsonPropertyName("score")]
        public int Score { get; set; } = 0;

        [JsonPropertyName("coach")]
        public string Coach { get; set; } = "";

        [JsonPropertyName("color")]
        public string ColorString { get; set; } = "rgb(0,0,0)";

        [JsonIgnore]
        public Color Color
        {
            get => ColorTranslator.FromHtml(ColorString);
            set => ColorString = ColorTranslator.ToHtml(value);
        }
    }
}
