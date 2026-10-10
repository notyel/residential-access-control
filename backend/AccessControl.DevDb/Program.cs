using MysticMind.PostgresEmbed;

// PostgreSQL local para desarrollo, sin Docker ni instalación: descarga los binarios la primera
// vez y conserva los datos entre ejecuciones. La API se conecta con el perfil "local-db".
const int Port = 5433;
var dataDir = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AccessControl", "devdb");
Directory.CreateDirectory(dataDir);

using var server = new PgServer(
    "17.4.0",
    dbDir: dataDir,
    instanceId: new Guid("5b0c1a6e-2f4d-4c9a-9d57-3a1f0e7c2b11"),
    port: Port,
    // El log va a archivos (carpeta log/ de la instancia): si quedara en la salida estándar,
    // que nadie lee, el servidor se bloquea al llenarse el búfer
    pgServerParams: new Dictionary<string, string> { ["logging_collector"] = "on" },
    clearInstanceDirOnStop: false);

await server.StartAsync();

Console.WriteLine($"PostgreSQL local escuchando en el puerto {Port}. Datos en {dataDir}");
Console.WriteLine($"Host=localhost;Port={Port};Username=postgres;Password=test;Database=postgres");
Console.WriteLine("Ctrl+C para detener.");

var stop = new TaskCompletionSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.TrySetResult(); };
AppDomain.CurrentDomain.ProcessExit += (_, _) => stop.TrySetResult();
await stop.Task;

await server.StopAsync();
