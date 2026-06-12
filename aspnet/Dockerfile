FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["aspnet/aspnet.csproj", "aspnet/"]
RUN dotnet restore "aspnet/aspnet.csproj"
COPY . .
WORKDIR "/src/aspnet"
RUN dotnet build "./aspnet.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./aspnet.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Mape za uploade i DataProtection kljuceve moraju postojati i biti zapisive
# za non-root app korisnika; named volume preuzima ovo vlasništvo pri prvom mountu
USER root
RUN mkdir -p /app/wwwroot/uploads /app/keys && chown -R $APP_UID /app/wwwroot/uploads /app/keys
USER $APP_UID
ENTRYPOINT ["dotnet", "aspnet.dll"]
