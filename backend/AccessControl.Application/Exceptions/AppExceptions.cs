using System;

namespace AccessControl.Application.Exceptions
{
    /// <summary>Errores de negocio; el manejador global de la API los traduce a un código HTTP.</summary>
    public abstract class AppException : Exception
    {
        protected AppException(string message) : base(message) { }
    }

    /// <summary>404: el recurso no existe (o no es visible para quien lo pide).</summary>
    public class NotFoundException : AppException
    {
        public NotFoundException(string message) : base(message) { }
    }

    /// <summary>409: duplicado o estado incompatible con la operación.</summary>
    public class ConflictException : AppException
    {
        public ConflictException(string message) : base(message) { }
    }

    /// <summary>400: los datos enviados no cumplen una regla de negocio.</summary>
    public class BusinessRuleException : AppException
    {
        public BusinessRuleException(string message) : base(message) { }
    }

    /// <summary>403: autenticado, pero sin permiso sobre este recurso.</summary>
    public class ForbiddenException : AppException
    {
        public ForbiddenException(string message = "No tiene permiso para esta acción.") : base(message) { }
    }
}
