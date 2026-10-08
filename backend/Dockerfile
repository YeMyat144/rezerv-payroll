# ---- build ---------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so dependency layers are cached between source changes.
COPY Rezerv.Payroll.slnx ./
COPY src/Rezerv.Payroll.Domain/Rezerv.Payroll.Domain.csproj src/Rezerv.Payroll.Domain/
COPY src/Rezerv.Payroll.Application/Rezerv.Payroll.Application.csproj src/Rezerv.Payroll.Application/
COPY src/Rezerv.Payroll.Infrastructure/Rezerv.Payroll.Infrastructure.csproj src/Rezerv.Payroll.Infrastructure/
COPY src/Rezerv.Payroll.Api/Rezerv.Payroll.Api.csproj src/Rezerv.Payroll.Api/
RUN dotnet restore src/Rezerv.Payroll.Api/Rezerv.Payroll.Api.csproj

COPY src/ src/
RUN dotnet publish src/Rezerv.Payroll.Api/Rezerv.Payroll.Api.csproj -c Release -o /app/publish --no-restore

# ---- runtime -------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Rezerv.Payroll.Api.dll"]
