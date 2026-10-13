# Aşama 1: derle (SDK imajı büyük, yalnızca derlemede kullanılır)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Simulator.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app --no-restore

# Aşama 2: çalıştır (küçük çalışma zamanı imajı)
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Simulator.dll"]
