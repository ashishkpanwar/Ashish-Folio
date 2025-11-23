using System.ComponentModel.DataAnnotations;

namespace Ashish_Backend_Folio.Models
{
    public class FailedOutboxMessage
    {
        [Key]
        public Guid Id { get; set; }
        public string Destination { get; set; }
        public string MessageId { get; set; }
        public string ContentType { get; set; }
        public byte[] Payload { get; set; }
        public string PropertiesJson { get; set; }
        public string FailureReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public int Retries { get; set; }
    }
}