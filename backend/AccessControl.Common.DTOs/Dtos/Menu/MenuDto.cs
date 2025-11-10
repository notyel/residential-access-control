using System;

namespace AccessControl.Common.DTOs.Menu
{
    public class MenuDto
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Path { get; set; }
        public string? Icon { get; set; }
    }
}
