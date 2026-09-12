# Imagem base oficial do Azure Functions para .NET 8 isolated worker.
# Roda o mesmo host de Functions que o serviço gerenciado, com os mesmos
# triggers e bindings — muda apenas onde o container é hospedado.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/PosTech.AuthFunction/PosTech.AuthFunction.csproj src/PosTech.AuthFunction/
RUN dotnet restore src/PosTech.AuthFunction/PosTech.AuthFunction.csproj

COPY src/ src/
RUN dotnet publish src/PosTech.AuthFunction/PosTech.AuthFunction.csproj \
    -c Release \
    -o /home/site/wwwroot \
    --no-restore

FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0

ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true \
    FUNCTIONS_WORKER_RUNTIME=dotnet-isolated

COPY --from=build /home/site/wwwroot /home/site/wwwroot

EXPOSE 80
