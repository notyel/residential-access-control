using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccessControl.Domain.Entities
{
    public class AccessLog
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public UserBase User { get; set; } = null!;
        public DateTime EntryTime { get; set; }
        public DateTime? ExitTime { get; set; }
        public string? AuthorizedBy { get; set; }
    }
}
