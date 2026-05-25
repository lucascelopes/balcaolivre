FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY BalcaoLivre.Admin.csproj .
RUN dotnet restore BalcaoLivre.Admin.csproj

COPY . .
RUN dotnet publish BalcaoLivre.Admin.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 10000

ENTRYPOINT ["dotnet", "BalcaoLivreAdmin.dll"]
