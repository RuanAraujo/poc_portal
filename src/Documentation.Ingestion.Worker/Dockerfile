FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Documentation.Contracts/Documentation.Contracts.csproj", "src/Documentation.Contracts/"]
COPY ["src/Documentation.Ingestion.Domain/Documentation.Ingestion.Domain.csproj", "src/Documentation.Ingestion.Domain/"]
COPY ["src/Documentation.Ingestion.Application/Documentation.Ingestion.Application.csproj", "src/Documentation.Ingestion.Application/"]
COPY ["src/Documentation.Ingestion.Infrastructure/Documentation.Ingestion.Infrastructure.csproj", "src/Documentation.Ingestion.Infrastructure/"]
COPY ["src/Documentation.Ingestion.Worker/Documentation.Ingestion.Worker.csproj", "src/Documentation.Ingestion.Worker/"]
RUN dotnet restore "src/Documentation.Ingestion.Worker/Documentation.Ingestion.Worker.csproj"

COPY . .
RUN dotnet publish "src/Documentation.Ingestion.Worker/Documentation.Ingestion.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
RUN apt-get update \
    && apt-get install --yes --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Documentation.Ingestion.Worker.dll"]
