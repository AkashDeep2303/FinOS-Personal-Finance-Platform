FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5001

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Shared/FinOS.Common/FinOS.Common.csproj", "Shared/FinOS.Common/"]
COPY ["Shared/FinOS.EventBus/FinOS.EventBus.csproj", "Shared/FinOS.EventBus/"]
COPY ["Shared/FinOS.EventBus.RabbitMQ/FinOS.EventBus.RabbitMQ.csproj", "Shared/FinOS.EventBus.RabbitMQ/"]
COPY ["Services/Identity/FinOS.Identity.Domain/FinOS.Identity.Domain.csproj", "Services/Identity/FinOS.Identity.Domain/"]
COPY ["Services/Identity/FinOS.Identity.Application/FinOS.Identity.Application.csproj", "Services/Identity/FinOS.Identity.Application/"]
COPY ["Services/Identity/FinOS.Identity.Infrastructure/FinOS.Identity.Infrastructure.csproj", "Services/Identity/FinOS.Identity.Infrastructure/"]
COPY ["Services/Identity/FinOS.Identity.API/FinOS.Identity.API.csproj", "Services/Identity/FinOS.Identity.API/"]
RUN dotnet restore "Services/Identity/FinOS.Identity.API/FinOS.Identity.API.csproj"
COPY . .
RUN dotnet build "Services/Identity/FinOS.Identity.API/FinOS.Identity.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Services/Identity/FinOS.Identity.API/FinOS.Identity.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FinOS.Identity.API.dll"]
