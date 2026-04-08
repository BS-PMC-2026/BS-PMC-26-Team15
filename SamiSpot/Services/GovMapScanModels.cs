using System.Text.Json;
using System.Text.Json.Serialization;

namespace SamiSpot.Services
{
    public class GovMapRequest
    {
        [JsonPropertyName("point")]
        public double[] Point { get; set; } = Array.Empty<double>();

        [JsonPropertyName("layers")]
        public List<GovMapLayerRequest> Layers { get; set; } = new();

        [JsonPropertyName("tolerance")]
        public double Tolerance { get; set; }
    }

    public class GovMapLayerRequest
    {
        [JsonPropertyName("layerId")]
        public string LayerId { get; set; } = string.Empty;
    }

    public class GovMapLayerResponse
    {
        [JsonPropertyName("data")]
        public List<GovMapLayerData> Data { get; set; } = new();
    }

    public class GovMapLayerData
    {
        [JsonPropertyName("layerName")]
        public string? LayerName { get; set; }

        [JsonPropertyName("layerId")]
        public string? LayerId { get; set; }

        [JsonPropertyName("entities")]
        public List<GovMapEntity> Entities { get; set; } = new();
    }

    public class GovMapEntity
    {
        [JsonPropertyName("objectId")]
        public int? ObjectId { get; set; }

        [JsonPropertyName("centroid")]
        public JsonElement Centroid { get; set; }

        [JsonPropertyName("fields")]
        public List<GovMapField> Fields { get; set; } = new();
    }

    public class GovMapCentroid
    {
        [JsonPropertyName("x")]
        public double X { get; set; }

        [JsonPropertyName("y")]
        public double Y { get; set; }
    }

    public class GovMapField
    {
        [JsonPropertyName("fieldName")]
        public string? FieldName { get; set; }

        [JsonPropertyName("fieldValue")]
        public object? FieldValue { get; set; }
    }
}