FROM mcr.microsoft.com/dotnet/core/aspnet:2.2-stretch-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:2.2-stretch AS build
WORKDIR /src
COPY ["Automatica.Core.Slave/Automatica.Core.Slave.csproj", "Automatica.Core.Slave/"]
RUN dotnet restore "Automatica.Core.Slave/Automatica.Core.Slave.csproj"
COPY . .
WORKDIR "/src/Automatica.Core.Slave"
RUN dotnet build "Automatica.Core.Slave.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "Automatica.Core.Slave.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Automatica.Core.Slave.dll"]