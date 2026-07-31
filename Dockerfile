# Stage 1: Runtime Base Image (.NET 9)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 80

# Stage 2: SDK Image for restoring dependencies and building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy csproj files for optimal layer caching
COPY ["src/EfbisMuhasebe.Web/EfbisMuhasebe.Web.csproj", "src/EfbisMuhasebe.Web/"]
COPY ["src/EfbisMuhasebe.Infrastructure/EfbisMuhasebe.Infrastructure.csproj", "src/EfbisMuhasebe.Infrastructure/"]
COPY ["src/EfbisMuhasebe.Application/EfbisMuhasebe.Application.csproj", "src/EfbisMuhasebe.Application/"]
COPY ["src/EfbisMuhasebe.Domain/EfbisMuhasebe.Domain.csproj", "src/EfbisMuhasebe.Domain/"]

# Restore dependencies
RUN dotnet restore "src/EfbisMuhasebe.Web/EfbisMuhasebe.Web.csproj"

# Copy all source files and build project
COPY . .
WORKDIR "/src/src/EfbisMuhasebe.Web"
RUN dotnet build "EfbisMuhasebe.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Stage 3: Publish application
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "EfbisMuhasebe.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Stage 4: Final Runtime Container
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EfbisMuhasebe.Web.dll"]
