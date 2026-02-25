FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore Nlb.Workshop.Consumer.Worker/Nlb.Workshop.Consumer.Worker.csproj
RUN dotnet publish Nlb.Workshop.Consumer.Worker/Nlb.Workshop.Consumer.Worker.csproj -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Nlb.Workshop.Consumer.Worker.dll"]
