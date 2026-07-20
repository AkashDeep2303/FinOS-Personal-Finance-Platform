FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5003

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Shared/FinOS.Common/FinOS.Common.csproj", "Shared/FinOS.Common/"]
COPY ["Shared/FinOS.EventBus/FinOS.EventBus.csproj", "Shared/FinOS.EventBus/"]
COPY ["Shared/FinOS.EventBus.RabbitMQ/FinOS.EventBus.RabbitMQ.csproj", "Shared/FinOS.EventBus.RabbitMQ/"]
COPY ["Services/Budget/FinOS.Budget.Domain/FinOS.Budget.Domain.csproj", "Services/Budget/FinOS.Budget.Domain/"]
COPY ["Services/Budget/FinOS.Budget.Application/FinOS.Budget.Application.csproj", "Services/Budget/FinOS.Budget.Application/"]
COPY ["Services/Budget/FinOS.Budget.Infrastructure/FinOS.Budget.Infrastructure.csproj", "Services/Budget/FinOS.Budget.Infrastructure/"]
COPY ["Services/Budget/FinOS.Budget.API/FinOS.Budget.API.csproj", "Services/Budget/FinOS.Budget.API/"]
RUN dotnet restore "Services/Budget/FinOS.Budget.API/FinOS.Budget.API.csproj"
COPY . .
RUN dotnet build "Services/Budget/FinOS.Budget.API/FinOS.Budget.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Services/Budget/FinOS.Budget.API/FinOS.Budget.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FinOS.Budget.API.dll"]
