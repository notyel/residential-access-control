using AccessControl.Application.Parking;
using AccessControl.Application.Reports;
using AccessControl.Application.Units;

namespace AccessControl.API.Infrastructure
{
    /// <summary>
    /// Tareas que deben ocurrir aunque nadie use la aplicación: cerrar las ocupaciones que
    /// vencieron por fecha y revocar lo que dependía de ellas. Corre al arrancar y luego cada hora;
    /// cada tarea es idempotente, así que repetirla no hace daño.
    /// </summary>
    public class DailyMaintenanceService(IServiceScopeFactory scopes, ILogger<DailyMaintenanceService> logger) : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(Interval);
            do
            {
                try
                {
                    using var scope = scopes.CreateScope();
                    var closed = await scope.ServiceProvider.GetRequiredService<IOccupancyService>().CloseExpiredAsync();
                    if (closed > 0)
                    {
                        logger.LogInformation("Mantenimiento: se cerraron {Count} ocupaciones vencidas.", closed);
                    }

                    var released = await scope.ServiceProvider.GetRequiredService<IParkingService>().CancelUnusedReservationsAsync();
                    if (released > 0)
                    {
                        logger.LogInformation("Mantenimiento: se liberaron {Count} reservas de parqueadero sin usar.", released);
                    }

                    var anonymized = await scope.ServiceProvider.GetRequiredService<IPrivacyService>().AnonymizeExpiredVisitorsAsync();
                    if (anonymized > 0)
                    {
                        logger.LogInformation("Mantenimiento: se anonimizaron {Count} visitantes por retención de datos.", anonymized);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // un fallo no detiene el servicio: se reintenta en el siguiente ciclo
                    logger.LogError(ex, "Falló el mantenimiento periódico.");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
    }
}
