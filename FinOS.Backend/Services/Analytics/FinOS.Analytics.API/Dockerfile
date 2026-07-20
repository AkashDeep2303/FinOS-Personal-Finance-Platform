FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5007

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Shared/FinOS.Common/FinOS.Common.csproj", "Shared/FinOS.Common/"]
COPY ["Shared/FinOS.EventBus/FinOS.EventBus.csproj", "Shared/FinOS.EventBus/"]
COPY ["Shared/FinOS.EventBus.RabbitMQ/FinOS.EventBus.RabbitMQ.csproj", "Shared/FinOS.EventBus.RabbitMQ/"]
COPY ["Services/Analytics/FinOS.Analytics.Domain/FinOS.Analytics.Domain.csproj", "Services/Analytics/FinOS.Analytics.Domain/"]
COPY ["Services/Analytics/FinOS.Analytics.Application/FinOS.Analytics.Application.csproj", "Services/Analytics/FinOS.Analytics.Application/"]
COPY ["Services/Analytics/FinOS.Analytics.Infrastructure/FinOS.Analytics.Infrastructure.csproj", "Services/Analytics/FinOS.Analytics.Infrastructure/"]
COPY ["Services/Analytics/FinOS.Analytics.API/FinOS.Analytics.API.csproj", "Services/Analytics/FinOS.Analytics.API/"]
RUN dotnet restore "Services/Analytics/FinOS.Analytics.API/FinOS.Analytics.API.csproj"
COPY . .
RUN dotnet build "Services/Analytics/FinOS.Analytics.API/FinOS.Analytics.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Services/Analytics/FinOS.Analytics.API/FinOS.Analytics.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FinOS.Analytics.API.dll"]
