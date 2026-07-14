# Build context is the repo root (see .dockerignore), so paths below are
# relative to the solution, not this file's own directory.

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY src/BlockchainLedger/BlockchainLedger.csproj src/BlockchainLedger/
RUN dotnet restore src/BlockchainLedger/BlockchainLedger.csproj

COPY src/BlockchainLedger/ src/BlockchainLedger/
RUN dotnet publish src/BlockchainLedger/BlockchainLedger.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Fixed internal port so peers on the same Docker network can always reach
# this node at <container-alias>:8080, regardless of what port the host
# maps it to externally.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "BlockchainLedger.dll"]
