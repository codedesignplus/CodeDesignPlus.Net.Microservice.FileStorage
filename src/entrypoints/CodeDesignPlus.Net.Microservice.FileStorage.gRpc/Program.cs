using CodeDesignPlus.Net.File.Storage.Extensions;
using CodeDesignPlus.Net.Observability.Interceptors;
using CodeDesignPlus.Net.gRpc.Clients.Extensions;
using CodeDesignPlus.Net.Logger.Extensions;
using CodeDesignPlus.Net.Microservice.Commons.EntryPoints.gRpc.Interceptors;
using CodeDesignPlus.Net.Microservice.Commons.FluentValidation;
using CodeDesignPlus.Net.Microservice.Commons.HealthChecks;
using CodeDesignPlus.Net.Microservice.Commons.MediatR;
using CodeDesignPlus.Net.Microservice.FileStorage.gRpc.Services;
using CodeDesignPlus.Net.Mongo.Extensions;
using CodeDesignPlus.Net.Observability.Extensions;
using CodeDesignPlus.Net.RabbitMQ.Extensions;
using CodeDesignPlus.Net.ServiceBus.Extensions;
using CodeDesignPlus.Net.Redis.Cache.Extensions;
using CodeDesignPlus.Net.Redis.Extensions;
using CodeDesignPlus.Net.Security.Extensions;
using CodeDesignPlus.Net.Vault.Extensions;

var builder = WebApplication.CreateSlimBuilder(args);

Serilog.Debugging.SelfLog.Enable(Console.Error);

builder.Host.UseSerilog();

builder.Configuration.AddVault();

builder.Services
    .AddGrpc(options =>
    {
        options.Interceptors.Add<ErrorInterceptor>();
        options.Interceptors.Add<TraceContextInterceptor>();
    })
    // EL TOPE SE SUBE SOLO PARA ESTE SERVICIO. gRPC rechaza por defecto todo mensaje de más de 4 MB, y el frontend
    // deja subir archivos de hasta 10 MB: un documento del vehículo que entró por la pantalla no podría guardarse
    // después desde el job. 16 MB cubre esos 10 con margen para el resto del mensaje, sin abrir la puerta a cualquier
    // tamaño en el resto del servidor.
    .AddServiceOptions<FilesService>(options => options.MaxReceiveMessageSize = 16 * 1024 * 1024);
builder.Services.AddGrpcReflection();

builder.Services.AddVault(builder.Configuration);
builder.Services.AddMapster();
builder.Services.AddMediatR<CodeDesignPlus.Net.Microservice.FileStorage.Application.Startup>();
builder.Services.AddFluentValidation<CodeDesignPlus.Net.Microservice.FileStorage.Application.Startup>();

builder.Services.AddMongo<CodeDesignPlus.Net.Microservice.FileStorage.Infrastructure.Startup>(builder.Configuration);
builder.Services.AddRedis(builder.Configuration);
builder.Services.AddRabbitMQ<CodeDesignPlus.Net.Microservice.FileStorage.Domain.Startup>(builder.Configuration);
builder.Services.AddServiceBus<CodeDesignPlus.Net.Microservice.FileStorage.Domain.Startup>(builder.Configuration);
builder.Services.AddSecurity(builder.Configuration);
builder.Services.AddObservability(builder.Configuration, builder.Environment);
builder.Services.AddLogger(builder.Configuration);
builder.Services.AddCache(builder.Configuration);
builder.Services.AddHealthChecksServices();
builder.Services.AddFileStorage(builder.Configuration);
builder.Services.AddGrpcClients(builder.Configuration);

var app = builder.Build();

app.UseHealthChecks();

app.UseAuth();

// SIN RequireAuthorization A PROPÓSITO. Quien llama es un job o un consumidor de otro microservicio, que no tiene JWT:
// la copropiedad y el usuario viajan en la petición (pendings/260). El servicio no se publica fuera del clúster: no
// tiene VirtualService.
app.MapGrpcService<FilesService>();

if (app.Environment.IsDevelopment())
{
    app.MapGrpcReflectionService();
}

app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

await app.RunAsync();

public partial class Program
{
    protected Program() { }
}
