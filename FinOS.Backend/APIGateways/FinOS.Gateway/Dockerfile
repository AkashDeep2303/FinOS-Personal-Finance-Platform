FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8000 8001

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Shared/FinOS.Common/FinOS.Common.csproj", "Shared/FinOS.Common/"]
COPY ["Shared/FinOS.EventBus/FinOS.EventBus.csproj", "Shared/FinOS.EventBus/"]
COPY ["Shared/FinOS.EventBus.RabbitMQ/FinOS.EventBus.RabbitMQ.csproj", "Shared/FinOS.EventBus.RabbitMQ/"]
COPY ["APIGateways/FinOS.Gateway/FinOS.Gateway.csproj", "APIGateways/FinOS.Gateway/"]
RUN dotnet restore "APIGateways/FinOS.Gateway/FinOS.Gateway.csproj"
COPY . .
RUN dotnet build "APIGateways/FinOS.Gateway/FinOS.Gateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "APIGateways/FinOS.Gateway/FinOS.Gateway.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FinOS.Gateway.dll"]
