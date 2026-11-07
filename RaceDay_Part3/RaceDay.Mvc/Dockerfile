# syntax=docker/dockerfile:1

# ---- Build stage: compiles the MVC app using the full .NET SDK ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy just the project files first so Docker can cache the NuGet restore
# layer and skip it on later builds unless a .csproj actually changed.
COPY RaceDay.Mvc/RaceDay.Mvc.csproj RaceDay.Mvc/
RUN dotnet restore RaceDay.Mvc/RaceDay.Mvc.csproj

# Now copy the rest of the source and publish a Release build.
COPY RaceDay.Mvc/ RaceDay.Mvc/
WORKDIR /src/RaceDay.Mvc
RUN dotnet publish -c Release -o /app/publish --no-restore

# ---- Runtime stage: the small ASP.NET runtime image, not the full SDK ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "RaceDay.Mvc.dll"]
