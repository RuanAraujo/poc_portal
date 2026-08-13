FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Documentation.Embeddings/Documentation.Embeddings.csproj", "src/Documentation.Embeddings/"]
RUN dotnet restore "src/Documentation.Embeddings/Documentation.Embeddings.csproj"
COPY . .
RUN dotnet publish "src/Documentation.Embeddings/Documentation.Embeddings.csproj" --no-restore --configuration Release --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080 8081
ENTRYPOINT ["dotnet", "Documentation.Embeddings.dll"]
