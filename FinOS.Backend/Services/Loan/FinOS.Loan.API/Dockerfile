FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5005

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Shared/FinOS.Common/FinOS.Common.csproj", "Shared/FinOS.Common/"]
COPY ["Shared/FinOS.EventBus/FinOS.EventBus.csproj", "Shared/FinOS.EventBus/"]
COPY ["Shared/FinOS.EventBus.RabbitMQ/FinOS.EventBus.RabbitMQ.csproj", "Shared/FinOS.EventBus.RabbitMQ/"]
COPY ["Services/Loan/FinOS.Loan.Domain/FinOS.Loan.Domain.csproj", "Services/Loan/FinOS.Loan.Domain/"]
COPY ["Services/Loan/FinOS.Loan.Application/FinOS.Loan.Application.csproj", "Services/Loan/FinOS.Loan.Application/"]
COPY ["Services/Loan/FinOS.Loan.Infrastructure/FinOS.Loan.Infrastructure.csproj", "Services/Loan/FinOS.Loan.Infrastructure/"]
COPY ["Services/Loan/FinOS.Loan.API/FinOS.Loan.API.csproj", "Services/Loan/FinOS.Loan.API/"]
RUN dotnet restore "Services/Loan/FinOS.Loan.API/FinOS.Loan.API.csproj"
COPY . .
RUN dotnet build "Services/Loan/FinOS.Loan.API/FinOS.Loan.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Services/Loan/FinOS.Loan.API/FinOS.Loan.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FinOS.Loan.API.dll"]
