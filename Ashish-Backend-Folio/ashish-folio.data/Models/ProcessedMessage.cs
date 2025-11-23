using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ashish_Backend_Folio.data.Models
{
    public class ProcessedMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string MessageId { get; set; } = null!;    // broker MessageId
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    }

}
