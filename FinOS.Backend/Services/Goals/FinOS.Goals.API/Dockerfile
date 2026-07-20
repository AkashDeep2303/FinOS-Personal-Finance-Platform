FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5006

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Shared/FinOS.Common/FinOS.Common.csproj", "Shared/FinOS.Common/"]
COPY ["Shared/FinOS.EventBus/FinOS.EventBus.csproj", "Shared/FinOS.EventBus/"]
COPY ["Shared/FinOS.EventBus.RabbitMQ/FinOS.EventBus.RabbitMQ.csproj", "Shared/FinOS.EventBus.RabbitMQ/"]
COPY ["Services/Goals/FinOS.Goals.Domain/FinOS.Goals.Domain.csproj", "Services/Goals/FinOS.Goals.Domain/"]
COPY ["Services/Goals/FinOS.Goals.Application/FinOS.Goals.Application.csproj", "Services/Goals/FinOS.Goals.Application/"]
COPY ["Services/Goals/FinOS.Goals.Infrastructure/FinOS.Goals.Infrastructure.csproj", "Services/Goals/FinOS.Goals.Infrastructure/"]
COPY ["Services/Goals/FinOS.Goals.API/FinOS.Goals.API.csproj", "Services/Goals/FinOS.Goals.API/"]
RUN dotnet restore "Services/Goals/FinOS.Goals.API/FinOS.Goals.API.csproj"
COPY . .
RUN dotnet build "Services/Goals/FinOS.Goals.API/FinOS.Goals.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Services/Goals/FinOS.Goals.API/FinOS.Goals.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FinOS.Goals.API.dll"]
