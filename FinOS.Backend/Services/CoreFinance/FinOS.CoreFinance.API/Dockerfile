FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5002

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Shared/FinOS.Common/FinOS.Common.csproj", "Shared/FinOS.Common/"]
COPY ["Shared/FinOS.EventBus/FinOS.EventBus.csproj", "Shared/FinOS.EventBus/"]
COPY ["Shared/FinOS.EventBus.RabbitMQ/FinOS.EventBus.RabbitMQ.csproj", "Shared/FinOS.EventBus.RabbitMQ/"]
COPY ["Services/CoreFinance/FinOS.CoreFinance.Domain/FinOS.CoreFinance.Domain.csproj", "Services/CoreFinance/FinOS.CoreFinance.Domain/"]
COPY ["Services/CoreFinance/FinOS.CoreFinance.Application/FinOS.CoreFinance.Application.csproj", "Services/CoreFinance/FinOS.CoreFinance.Application/"]
COPY ["Services/CoreFinance/FinOS.CoreFinance.Infrastructure/FinOS.CoreFinance.Infrastructure.csproj", "Services/CoreFinance/FinOS.CoreFinance.Infrastructure/"]
COPY ["Services/CoreFinance/FinOS.CoreFinance.API/FinOS.CoreFinance.API.csproj", "Services/CoreFinance/FinOS.CoreFinance.API/"]
RUN dotnet restore "Services/CoreFinance/FinOS.CoreFinance.API/FinOS.CoreFinance.API.csproj"
COPY . .
RUN dotnet build "Services/CoreFinance/FinOS.CoreFinance.API/FinOS.CoreFinance.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Services/CoreFinance/FinOS.CoreFinance.API/FinOS.CoreFinance.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FinOS.CoreFinance.API.dll"]
