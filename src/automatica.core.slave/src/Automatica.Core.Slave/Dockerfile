FROM automaticacore/automatica-plugin-runtime:amd64-6 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
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