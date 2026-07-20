FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5008

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Shared/FinOS.Common/FinOS.Common.csproj", "Shared/FinOS.Common/"]
COPY ["Shared/FinOS.EventBus/FinOS.EventBus.csproj", "Shared/FinOS.EventBus/"]
COPY ["Shared/FinOS.EventBus.RabbitMQ/FinOS.EventBus.RabbitMQ.csproj", "Shared/FinOS.EventBus.RabbitMQ/"]
COPY ["Services/AIAssistant/FinOS.AIAssistant.Domain/FinOS.AIAssistant.Domain.csproj", "Services/AIAssistant/FinOS.AIAssistant.Domain/"]
COPY ["Services/AIAssistant/FinOS.AIAssistant.Application/FinOS.AIAssistant.Application.csproj", "Services/AIAssistant/FinOS.AIAssistant.Application/"]
COPY ["Services/AIAssistant/FinOS.AIAssistant.Infrastructure/FinOS.AIAssistant.Infrastructure.csproj", "Services/AIAssistant/FinOS.AIAssistant.Infrastructure/"]
COPY ["Services/AIAssistant/FinOS.AIAssistant.API/FinOS.AIAssistant.API.csproj", "Services/AIAssistant/FinOS.AIAssistant.API/"]
RUN dotnet restore "Services/AIAssistant/FinOS.AIAssistant.API/FinOS.AIAssistant.API.csproj"
COPY . .
RUN dotnet build "Services/AIAssistant/FinOS.AIAssistant.API/FinOS.AIAssistant.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Services/AIAssistant/FinOS.AIAssistant.API/FinOS.AIAssistant.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FinOS.AIAssistant.API.dll"]
