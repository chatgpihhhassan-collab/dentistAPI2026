# Stage 1: Build ASP.NET Core Web API
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["DentistAPI/DentistAPI.csproj", "DentistAPI/"]
RUN dotnet restore "DentistAPI/DentistAPI.csproj"

# Copy everything and build
COPY . .
WORKDIR "/src/DentistAPI"
RUN dotnet build "DentistAPI.csproj" -c Release -o /app/build

# Publish release binary
FROM build AS publish
RUN dotnet publish "DentistAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Final Runtime Image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Render exposes PORT dynamically; bind Kestrel to 0.0.0.0:${PORT:-8080}
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "DentistAPI.dll"]
