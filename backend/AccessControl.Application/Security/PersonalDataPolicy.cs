using AccessControl.Common.DTOs.Access;
using AccessControl.Common.DTOs.Person;
using AccessControl.Domain.Common;
using System.Collections.Generic;

namespace AccessControl.Application.Security
{
    /// <summary>
    /// Único punto que decide qué datos personales salen del servidor según los permisos de quien
    /// consulta. Datos básicos (nombre, unidad) siempre; el documento exige people.view_identity y
    /// el teléfono y el correo exigen people.view_contact. Sin el permiso el dato sale enmascarado
    /// o vacío: el navegador nunca recibe el valor completo.
    /// </summary>
    public interface IPersonalDataPolicy
    {
        bool CanViewIdentity { get; }
        bool CanViewContact { get; }

        T Apply<T>(T person) where T : PersonDto;
        VisitDto Apply(VisitDto visit);
        AccessEventDto Apply(AccessEventDto accessEvent);
        void Apply<T>(IEnumerable<T>? people) where T : PersonDto;
        void Apply(IEnumerable<VisitDto>? visits);
        void Apply(IEnumerable<AccessEventDto>? events);

        /// <summary>"1020304321" → "••••4321". Con permiso devuelve el valor tal cual.</summary>
        string? Document(string? value);
    }

    public class PersonalDataPolicy : IPersonalDataPolicy
    {
        public const string Mask = "••••";

        private readonly ICurrentUser _currentUser;

        public PersonalDataPolicy(ICurrentUser currentUser)
        {
            _currentUser = currentUser;
        }

        public bool CanViewIdentity => _currentUser.HasPermission(Permissions.PeopleViewIdentity);
        public bool CanViewContact => _currentUser.HasPermission(Permissions.PeopleViewContact);

        public T Apply<T>(T person) where T : PersonDto
        {
            if (!CanViewIdentity)
            {
                person.DocumentNumber = MaskValue(person.DocumentNumber)!;
            }
            if (!CanViewContact)
            {
                person.Phone = null;
                person.Email = null;
            }
            return person;
        }

        public VisitDto Apply(VisitDto visit)
        {
            Apply(visit.Person);
            return visit;
        }

        public AccessEventDto Apply(AccessEventDto accessEvent)
        {
            accessEvent.PersonDocument = Document(accessEvent.PersonDocument);
            return accessEvent;
        }

        public void Apply<T>(IEnumerable<T>? people) where T : PersonDto
        {
            foreach (var person in people ?? new List<T>())
            {
                Apply(person);
            }
        }

        public void Apply(IEnumerable<VisitDto>? visits)
        {
            foreach (var visit in visits ?? new List<VisitDto>())
            {
                Apply(visit);
            }
        }

        public void Apply(IEnumerable<AccessEventDto>? events)
        {
            foreach (var accessEvent in events ?? new List<AccessEventDto>())
            {
                Apply(accessEvent);
            }
        }

        public string? Document(string? value) => CanViewIdentity ? value : MaskValue(value);

        /// <summary>Conserva el tipo ("CC ") y los últimos cuatro caracteres; lo demás se oculta.</summary>
        public static string? MaskValue(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;

            var separator = value.LastIndexOf(' ');
            var prefix = separator >= 0 ? value[..(separator + 1)] : string.Empty;
            var number = separator >= 0 ? value[(separator + 1)..] : value;
            return prefix + Mask + (number.Length > 4 ? number[^4..] : string.Empty);
        }
    }
}
