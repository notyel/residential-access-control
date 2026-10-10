using AccessControl.Application.Exceptions;
using AccessControl.Common.DTOs;
using Microsoft.AspNetCore.Diagnostics;

namespace AccessControl.API.Infrastructure
{
    /// <summary>
    /// Único punto que convierte excepciones en respuestas. Los errores de negocio conservan su
    /// mensaje; cualquier otro responde 500 genérico y el detalle queda solo en el log.
    /// </summary>
    public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            var (status, message) = exception switch
            {
                NotFoundException e => (StatusCodes.Status404NotFound, e.Message),
                ConflictException e => (StatusCodes.Status409Conflict, e.Message),
                BusinessRuleException e => (StatusCodes.Status400BadRequest, e.Message),
                ForbiddenException e => (StatusCodes.Status403Forbidden, e.Message),
                _ => (StatusCodes.Status500InternalServerError, "Ocurrió un error inesperado.")
            };

            if (status == StatusCodes.Status500InternalServerError)
            {
                logger.LogError(exception, "Error no controlado en {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
            }

            httpContext.Response.StatusCode = status;
            await httpContext.Response.WriteAsJsonAsync(new ResponseModel<object>(false, message, null, [message]), cancellationToken);
            return true;
        }
    }
}
