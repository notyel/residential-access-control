using System.ComponentModel.DataAnnotations;

namespace AccessControl.Common.DTOs.Validation
{
    /// <summary>
    /// Como [EmailAddress], pero acepta el campo vacío: los formularios envían "" cuando un dato
    /// opcional no se diligencia.
    /// </summary>
    public class OptionalEmailAddressAttribute : ValidationAttribute
    {
        private static readonly EmailAddressAttribute Inner = new();

        public OptionalEmailAddressAttribute()
        {
            ErrorMessage = "El correo no tiene un formato válido.";
        }

        public override bool IsValid(object? value)
        {
            return value is null || (value is string text && (string.IsNullOrWhiteSpace(text) || Inner.IsValid(text)));
        }
    }
}
