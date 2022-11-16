ARG DOTNET_CR=mcr.microsoft.com/dotnet
ARG DOTNET_VERSION=6.0
ARG BIN_DIR=/usr/local/bin
ARG SRC_DIR=/source

FROM $DOTNET_CR/sdk:$DOTNET_VERSION AS build
ENV PATH="/root/.dotnet/tools:$PATH"
ARG BUILD_VERSION=1
ARG SRC_DIR

# copy csproj files
WORKDIR $SRC_DIR
COPY engi-substrate/*.csproj engi-substrate/
COPY engi-server/*.csproj engi-server/
WORKDIR $SRC_DIR/engi-server
RUN dotnet restore

# copy apps
WORKDIR $SRC_DIR
COPY engi-substrate/ engi-substrate/
COPY engi-server/ engi-server/
RUN dotnet tool install -g dotnet-setversion
RUN setversion -r $BUILD_VERSION
WORKDIR $SRC_DIR/engi-server
RUN dotnet build -c release --no-restore

FROM build AS test
ARG BIN_DIR SRC_DIR
ARG APT_GET_PKGS="jq curl dnsutils"
WORKDIR $SRC_DIR
COPY engi-tests/ engi-tests/
WORKDIR $SRC_DIR/engi-tests
RUN dotnet build -c release
ADD https://github.com/ufoscout/docker-compose-wait/releases/download/2.9.0/wait /wait
RUN chmod +x /wait
COPY scripts/aws-ecs-env $BIN_DIR/aws-ecs-env
RUN apt update && apt-get --no-install-recommends install -y $APT_GET_PKGS
RUN apt autoremove && apt clean
ENTRYPOINT ["aws-ecs-env"]
CMD ["/wait && dotnet test --logger:trx --no-build -c release"]

FROM build AS publish
ARG SRC_DIR
WORKDIR $SRC_DIR/engi-server
RUN dotnet publish -c release --no-build -o /app

FROM $DOTNET_CR/aspnet:$DOTNET_VERSION
ARG BIN_DIR
WORKDIR /app
COPY --from=publish /app .
## Add the wait script to the image
ADD https://github.com/ufoscout/docker-compose-wait/releases/download/2.9.0/wait /wait
RUN chmod +x /wait
COPY scripts/aws-ecs-env $BIN_DIR/aws-ecs-env
RUN apt update && apt-get --no-install-recommends install -y curl jq && apt clean
ENTRYPOINT ["aws-ecs-env"]
CMD ["/wait && dotnet Engi.Substrate.Server.dll"]
