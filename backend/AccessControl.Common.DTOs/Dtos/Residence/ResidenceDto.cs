using System;

namespace AccessControl.Common.DTOs.Residence
{
    public class ResidenceDto
    {
        public Guid Id { get; set; }
        public string Identifier { get; set; } = null!;
        public Guid OwnerId { get; set; }
        public string OwnerFullName { get; set; } = null!;
    }
}
