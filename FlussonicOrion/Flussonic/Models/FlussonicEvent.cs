using FlussonicOrion.Flussonic.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace FlussonicOrion.Models
{
    public class FlussonicEvent
    {
        [JsonProperty(propertyName: "id")]
        public string Id { get; set; }

        [JsonProperty(propertyName: "camera_id")]
        public string CameraId { get; set; }

        [JsonProperty(propertyName: "start_at")]
        public string StartAt { get; set; }

        [JsonProperty(propertyName: "end_at")]
        public string EndAt { get; set; }

        [JsonProperty(propertyName: "object_class")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ObjectClass ObjectClass { get; set; }

        [JsonProperty(propertyName: "object_id")]
        public string ObjectId { get; set; }

        [JsonProperty(propertyName: "object_action")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ObjectAction ObjectAction { get; set; }
    }
}
