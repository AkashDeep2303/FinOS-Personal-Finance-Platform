FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5009

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Shared/FinOS.Common/FinOS.Common.csproj", "Shared/FinOS.Common/"]
COPY ["Shared/FinOS.EventBus/FinOS.EventBus.csproj", "Shared/FinOS.EventBus/"]
COPY ["Shared/FinOS.EventBus.RabbitMQ/FinOS.EventBus.RabbitMQ.csproj", "Shared/FinOS.EventBus.RabbitMQ/"]
COPY ["Services/Notification/FinOS.Notification.Domain/FinOS.Notification.Domain.csproj", "Services/Notification/FinOS.Notification.Domain/"]
COPY ["Services/Notification/FinOS.Notification.Application/FinOS.Notification.Application.csproj", "Services/Notification/FinOS.Notification.Application/"]
COPY ["Services/Notification/FinOS.Notification.Infrastructure/FinOS.Notification.Infrastructure.csproj", "Services/Notification/FinOS.Notification.Infrastructure/"]
COPY ["Services/Notification/FinOS.Notification.API/FinOS.Notification.API.csproj", "Services/Notification/FinOS.Notification.API/"]
RUN dotnet restore "Services/Notification/FinOS.Notification.API/FinOS.Notification.API.csproj"
COPY . .
RUN dotnet build "Services/Notification/FinOS.Notification.API/FinOS.Notification.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Services/Notification/FinOS.Notification.API/FinOS.Notification.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FinOS.Notification.API.dll"]
