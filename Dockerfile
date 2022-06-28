FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /source

# copy csproj files
COPY engi-substrate/*.csproj engi-substrate/
COPY engi-server/*.csproj engi-server/
WORKDIR /source/engi-server
RUN dotnet restore

# copy apps
WORKDIR /source
COPY engi-substrate/ engi-substrate/
COPY engi-server/ engi-server/
WORKDIR /source/engi-server
RUN dotnet build -c release --no-restore

FROM build AS test
WORKDIR /source
COPY engi-tests/ engi-tests/
WORKDIR /source/engi-tests
RUN dotnet build -c release
ADD https://github.com/ufoscout/docker-compose-wait/releases/download/2.9.0/wait /wait
RUN chmod +x /wait
ENTRYPOINT ["bash", "-c", "/wait && dotnet test --logger:trx --no-build -c release"]

FROM build AS publish
WORKDIR /source/engi-server
RUN dotnet publish -c release --no-build -o /app

FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=publish /app .
## Add the wait script to the image
ADD https://github.com/ufoscout/docker-compose-wait/releases/download/2.9.0/wait /wait
RUN chmod +x /wait
ENTRYPOINT ["bash", "-c", "/wait && dotnet Engi.Substrate.Server.dll"]