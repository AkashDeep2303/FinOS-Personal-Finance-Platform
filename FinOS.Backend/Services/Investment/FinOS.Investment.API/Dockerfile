FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5004

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Shared/FinOS.Common/FinOS.Common.csproj", "Shared/FinOS.Common/"]
COPY ["Shared/FinOS.EventBus/FinOS.EventBus.csproj", "Shared/FinOS.EventBus/"]
COPY ["Shared/FinOS.EventBus.RabbitMQ/FinOS.EventBus.RabbitMQ.csproj", "Shared/FinOS.EventBus.RabbitMQ/"]
COPY ["Services/Investment/FinOS.Investment.Domain/FinOS.Investment.Domain.csproj", "Services/Investment/FinOS.Investment.Domain/"]
COPY ["Services/Investment/FinOS.Investment.Application/FinOS.Investment.Application.csproj", "Services/Investment/FinOS.Investment.Application/"]
COPY ["Services/Investment/FinOS.Investment.Infrastructure/FinOS.Investment.Infrastructure.csproj", "Services/Investment/FinOS.Investment.Infrastructure/"]
COPY ["Services/Investment/FinOS.Investment.API/FinOS.Investment.API.csproj", "Services/Investment/FinOS.Investment.API/"]
RUN dotnet restore "Services/Investment/FinOS.Investment.API/FinOS.Investment.API.csproj"
COPY . .
RUN dotnet build "Services/Investment/FinOS.Investment.API/FinOS.Investment.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Services/Investment/FinOS.Investment.API/FinOS.Investment.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FinOS.Investment.API.dll"]
