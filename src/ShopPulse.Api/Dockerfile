# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ShopPulse.Api.csproj ./
RUN dotnet restore ShopPulse.Api.csproj

COPY . ./
RUN dotnet publish ShopPulse.Api.csproj -c Release -o /app/publish /p:UseAppHost=false --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
# Npgsql's optional GSSAPI negotiation loads this system library when present.
RUN apt-get update \\
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \\
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish ./

# Railway injects PORT at runtime. Program.cs binds Kestrel to 0.0.0.0:$PORT.
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["dotnet", "ShopPulse.Api.dll"]
