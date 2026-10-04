FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["nuget.config", "nuget.config"]
COPY ["src/domain/CodeDesignPlus.Net.Microservice.FileStorage.Application/CodeDesignPlus.Net.Microservice.FileStorage.Application.csproj", "domain/CodeDesignPlus.Net.Microservice.FileStorage.Application/"]
COPY ["src/domain/CodeDesignPlus.Net.Microservice.FileStorage.Domain/CodeDesignPlus.Net.Microservice.FileStorage.Domain.csproj", "domain/CodeDesignPlus.Net.Microservice.FileStorage.Domain/"]
COPY ["src/domain/CodeDesignPlus.Net.Microservice.FileStorage.Infrastructure/CodeDesignPlus.Net.Microservice.FileStorage.Infrastructure.csproj", "domain/CodeDesignPlus.Net.Microservice.FileStorage.Infrastructure/"]
COPY ["src/entrypoints/CodeDesignPlus.Net.Microservice.FileStorage.gRpc/CodeDesignPlus.Net.Microservice.FileStorage.gRpc.csproj", "entrypoints/CodeDesignPlus.Net.Microservice.FileStorage.gRpc/"]
RUN dotnet restore "./entrypoints/CodeDesignPlus.Net.Microservice.FileStorage.gRpc/CodeDesignPlus.Net.Microservice.FileStorage.gRpc.csproj" /p:PublishReadyToRun=true
COPY ["src/domain/", "domain/"]
COPY ["src/entrypoints/CodeDesignPlus.Net.Microservice.FileStorage.gRpc/", "entrypoints/CodeDesignPlus.Net.Microservice.FileStorage.gRpc/"]

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
RUN dotnet publish "./entrypoints/CodeDesignPlus.Net.Microservice.FileStorage.gRpc/CodeDesignPlus.Net.Microservice.FileStorage.gRpc.csproj" \
    -c "$BUILD_CONFIGURATION" \
    -o /app/publish \
    /p:UseAppHost=false \
    /p:PublishReadyToRun=true

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final
WORKDIR /app
EXPOSE 5000 5001
ENV DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_GCDynamicAdaptationMode=1 \
    DOTNET_GCConserveMemory=5 \
    DOTNET_GCHeapHardLimitPercent=70
USER app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CodeDesignPlus.Net.Microservice.FileStorage.gRpc.dll"]
