using AccessControl.Shared.Enums;
using System;

namespace AccessControl.Common.DTOs.User
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? ApartmentNumber { get; set; }
        public Role Role { get; set; }
    }
}
