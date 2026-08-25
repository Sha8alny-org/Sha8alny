# Use the official ASP.NET Core runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Use the SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# 1. Copy the Solution File
COPY ["Sh8lnySolution.sln", "."]

# 2. Copy the Project Files (Restore Dependencies)
COPY ["Sh8lny.Web/Sh8lny.Web.csproj", "Sh8lny.Web/"]
COPY ["Core/Sh8lny.Abstraction/Sh8lny.Abstraction.csproj", "Core/Sh8lny.Abstraction/"]
COPY ["Core/Sh8lny.Domain/Sh8lny.Domain.csproj", "Core/Sh8lny.Domain/"]
COPY ["Core/Sh8lny.Service/Sh8lny.Service.csproj", "Core/Sh8lny.Service/"]
COPY ["Infrastructure/Sh8lny.Persistence/Sh8lny.Persistence.csproj", "Infrastructure/Sh8lny.Persistence/"]
COPY ["Sh8lny.Shared/Sh8lny.Shared.csproj", "Sh8lny.Shared/"]

# 3. Restore
RUN dotnet restore "./Sh8lny.Web/Sh8lny.Web.csproj"

# 4. Copy the rest of the source code
COPY . .

# 5. Build
WORKDIR "/src/Sh8lny.Web"
RUN dotnet build "./Sh8lny.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

# 6. Publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Sh8lny.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# 7. Final Stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Sh8lny.Web.dll"]
