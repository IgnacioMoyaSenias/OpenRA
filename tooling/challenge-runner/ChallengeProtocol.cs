using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChallengeRunner
{
    // Simplified protocol classes for the runner
    public class ChallengeAction
    {
        [JsonPropertyName("kind")]
        public string Kind { get; set; }

        [JsonPropertyName("unit_id")]
        public string UnitId { get; set; } = string.Empty;

        [JsonPropertyName("target_actor_id")]
        public string TargetActorId { get; set; } = string.Empty;

        [JsonPropertyName("target")]
        public ChallengePosition Target { get; set; } = new();

        [JsonPropertyName("producer_id")]
        public string ProducerId { get; set; } = string.Empty;

        [JsonPropertyName("unit_type")]
        public string UnitType { get; set; } = string.Empty;
    }

    public class ChallengeActionBatch
    {
        [JsonPropertyName("decision_id")]
        public int DecisionId { get; set; }

        [JsonPropertyName("actions")]
        public List<ChallengeAction> Actions { get; set; } = new();
    }

    public class ChallengePosition
    {
        [JsonPropertyName("x")]
        public int X { get; set; }

        [JsonPropertyName("y")]
        public int Y { get; set; }
    }

    public class ChallengeObservation
    {
        public ChallengeObservation(JsonElement content)
        {
            // Parse from JSON element - simplified for baseline
            var root = content;

            if (root.TryGetProperty("tick", out var tickProp))
                Tick = tickProp.GetInt32();

            if (root.TryGetProperty("map", out var mapProp))
            {
                if (mapProp.TryGetProperty("width", out var widthProp))
                    Map = new ChallengeMapInfo { Width = widthProp.GetInt32() };

                if (mapProp.TryGetProperty("height", out var heightProp))
                    Map.Height = heightProp.GetInt32();
            }

            // Parse own units
            OwnUnits = new List<ChallengeUnitInfo>();
            if (root.TryGetProperty("own_units", out var ownUnitsProp) && ownUnitsProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var unitElement in ownUnitsProp.EnumerateArray())
                {
                    OwnUnits.Add(new ChallengeUnitInfo(unitElement));
                }
            }

            // Parse visible enemies
            VisibleEnemies = new List<ChallengeUnitInfo>();
            if (root.TryGetProperty("visible_enemies", out var enemiesProp) && enemiesProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var enemyElement in enemiesProp.EnumerateArray())
                {
                    VisibleEnemies.Add(new ChallengeUnitInfo(enemyElement));
                }
            }
        }

        public int Tick { get; set; }
        public ChallengeMapInfo Map { get; set; } = new ChallengeMapInfo();
        public List<ChallengeUnitInfo> OwnUnits { get; set; }
        public List<ChallengeUnitInfo> VisibleEnemies { get; set; }
    }

    public class ChallengeMapInfo
    {
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class ChallengeUnitInfo
    {
        public ChallengeUnitInfo(JsonElement element)
        {
            if (element.TryGetProperty("id", out var idProp))
                Id = idProp.GetString();

            if (element.TryGetProperty("type", out var typeProp))
                Type = typeProp.GetString();

            if (element.TryGetProperty("position", out var posProp))
            {
                if (posProp.TryGetProperty("x", out var xProp))
                    Position.X = xProp.GetInt32();

                if (posProp.TryGetProperty("y", out var yProp))
                    Position.Y = yProp.GetInt32();
            }
        }

        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public ChallengePosition Position { get; set; } = new();
    }
}
