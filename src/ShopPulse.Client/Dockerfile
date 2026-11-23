FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ShopPulse.Client.csproj ./
RUN dotnet restore ShopPulse.Client.csproj

COPY . .
RUN dotnet restore ShopPulse.Client.csproj
RUN dotnet publish ShopPulse.Client.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM nginx:1.27-alpine AS final
RUN apk add --no-cache gettext
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY docker-entrypoint.d/40-api-config.sh /docker-entrypoint.d/40-api-config.sh
RUN chmod +x /docker-entrypoint.d/40-api-config.sh
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html

EXPOSE 80
