FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy project files first to layer-cache NuGet restore
COPY Directory.Build.props Directory.Packages.props ./
COPY src/Payroll.Engine/Payroll.Engine.csproj           src/Payroll.Engine/
COPY src/Payroll.Domain/Payroll.Domain.csproj           src/Payroll.Domain/
COPY src/Payroll.Application/Payroll.Application.csproj src/Payroll.Application/
COPY src/Payroll.Infrastructure/Payroll.Infrastructure.csproj src/Payroll.Infrastructure/
COPY src/Payroll.Api/Payroll.Api.csproj                 src/Payroll.Api/
RUN dotnet restore src/Payroll.Api/Payroll.Api.csproj

COPY src/ src/
RUN dotnet publish src/Payroll.Api/Payroll.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app

RUN adduser --disabled-password --gecos "" appuser
USER appuser

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Payroll.Api.dll"]
