using AccessControl.Common.DTOs.Unit;
using AccessControl.Common.DTOs.User;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AccessControl.Common.DTOs.Person
{
    public class PersonListItemDto : PersonDto
    {
        /// <summary>Unidades de las que es propietaria hoy.</summary>
        public List<string> OwnedUnits { get; set; } = new();

        /// <summary>Unidades en las que vive hoy.</summary>
        public List<string> OccupiedUnits { get; set; } = new();

        public bool HasUser { get; set; }
    }

    /// <summary>Ficha de persona: todas sus unidades y la relación con cada una, vigentes y pasadas.</summary>
    public class PersonDetailDto
    {
        public PersonDto Person { get; set; } = null!;
        public List<OwnershipDto> Ownerships { get; set; } = new();
        public List<OccupancyDto> Occupancies { get; set; } = new();
        public UserDto? User { get; set; }
    }

    public class PersonFilterDto
    {
        [Range(1, int.MaxValue)]
        public int PageNumber { get; set; } = 1;

        [Range(1, 500)]
        public int PageSize { get; set; } = 10;

        public string? Search { get; set; }

        /// <summary>Solo propietarios y ocupantes vigentes (deja fuera a quienes solo han sido visitantes).</summary>
        public bool OnlyLinked { get; set; }
    }

    public class GrantAccessDto
    {
        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
        [MaxLength(200)]
        public string Email { get; set; } = null!;

        /// <summary>owner o resident.</summary>
        [Required(ErrorMessage = "El rol es obligatorio.")]
        public string RoleKey { get; set; } = null!;
    }
}
