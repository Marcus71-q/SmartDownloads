FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY SmartDownloads.Server/SmartDownloads.Server.csproj SmartDownloads.Server/
RUN dotnet restore SmartDownloads.Server/SmartDownloads.Server.csproj
COPY SmartDownloads.Server/ SmartDownloads.Server/
RUN dotnet publish SmartDownloads.Server/SmartDownloads.Server.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
WORKDIR /app
COPY --from=build /app .
USER appuser
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "SmartDownloads.Server.dll"]
