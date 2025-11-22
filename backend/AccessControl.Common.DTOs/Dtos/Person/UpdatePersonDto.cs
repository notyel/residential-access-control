namespace AccessControl.Common.DTOs.Person
{
    public class UpdatePersonDto
    {
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string DocumentType { get; set; } = null!;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public int PersonType { get; set; }
    }
}
