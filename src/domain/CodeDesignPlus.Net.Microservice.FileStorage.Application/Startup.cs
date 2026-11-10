using CodeDesignPlus.Net.Core.Abstractions;
using CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage;
using CodeDesignPlus.Net.Microservice.FileStorage.Application.Setup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CodeDesignPlus.Net.Microservice.FileStorage.Application
{
    public class Startup : IStartup
    {
        public void Initialize(IServiceCollection services, IConfiguration configuration)
        {
            MapsterConfigFileStorage.Configure();

            // Qué carpetas son de la plataforma lo dice el despliegue, no el código (pendings/302).
            services
                .AddOptions<FileScopeOptions>()
                .Bind(configuration.GetSection(FileScopeOptions.Section))
                .Validate(x => x.PlatformContainer != Guid.Empty, "FileScope:PlatformContainer is required: the storage refuses an empty container.")
                .ValidateOnStart();
        }
    }
}
