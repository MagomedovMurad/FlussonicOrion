namespace FlussonicOrion.Models
{
    public class VideoSource
    {
        public VideoSource(string id, int accessPointId, PassageDirection direction)
        {
            Id = id;
            AccessPointId = accessPointId;
            PassageDirection = direction;
        }

        public string Id { get; set; }
        public int AccessPointId { get; set; }
        public PassageDirection PassageDirection { get; set; }
        public string LastRecognizedId { get; set; } 
    }
}
