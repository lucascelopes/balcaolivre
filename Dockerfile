FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY BalcaoLivre.Admin/BalcaoLivre.Admin.csproj BalcaoLivre.Admin/
RUN dotnet restore BalcaoLivre.Admin/BalcaoLivre.Admin.csproj

COPY BalcaoLivre.Admin/ BalcaoLivre.Admin/
RUN dotnet publish BalcaoLivre.Admin/BalcaoLivre.Admin.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 10000

ENTRYPOINT ["dotnet", "BalcaoLivreAdmin.dll"]
