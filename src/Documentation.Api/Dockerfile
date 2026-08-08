FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Documentation.Contracts/Documentation.Contracts.csproj", "src/Documentation.Contracts/"]
COPY ["src/Documentation.Domain/Documentation.Domain.csproj", "src/Documentation.Domain/"]
COPY ["src/Documentation.Application/Documentation.Application.csproj", "src/Documentation.Application/"]
COPY ["src/Documentation.Infrastructure/Documentation.Infrastructure.csproj", "src/Documentation.Infrastructure/"]
COPY ["src/Documentation.Api/Documentation.Api.csproj", "src/Documentation.Api/"]
RUN dotnet restore "src/Documentation.Api/Documentation.Api.csproj"

COPY . .
RUN dotnet publish "src/Documentation.Api/Documentation.Api.csproj" --no-restore --configuration Release --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Documentation.Api.dll"]
