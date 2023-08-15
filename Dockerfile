ARG DOTNET_CR=mcr.microsoft.com/dotnet
ARG DOTNET_VERSION=6.0
ARG DOTNET_VERSION_SHA256=19760ecbe8a7e911ae94baa9d084a0a5779fc8f24f8dc500e5cac86b077adbf9
ARG BIN_DIR=/usr/local/bin
ARG SRC_DIR=/source
ARG CI_LINUX_VERSION=1.68

FROM $DOTNET_CR/sdk:${DOTNET_VERSION}-alpine@sha256:${DOTNET_VERSION_SHA256} AS build
ENV PATH="/root/.dotnet/tools:$PATH"
ARG BUILD_VERSION=1
ARG SRC_DIR

# build libgit2sharp first
WORKDIR $SRC_DIR
COPY libgit2sharp/ libgit2sharp/
WORKDIR $SRC_DIR/libgit2sharp/LibGit2Sharp
RUN dotnet build -c release

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

FROM rust:$CI_LINUX_VERSION as rust_builder
ARG SRC_DIR
WORKDIR $SRC_DIR
COPY engi-crypto/ .
WORKDIR $SRC_DIR
RUN cargo b -r

FROM build AS test
ARG BIN_DIR SRC_DIR
WORKDIR $SRC_DIR
COPY engi-tests/ engi-tests/
WORKDIR $SRC_DIR/engi-tests
RUN dotnet build -c release
ADD https://github.com/ufoscout/docker-compose-wait/releases/download/2.9.0/wait /wait
RUN chmod +x /wait
COPY scripts/aws-ecs-env $BIN_DIR/aws-ecs-env
ENTRYPOINT ["dotnet", "test", "--filter", "FullyQualifiedName~Engi.Substrate.Integration", "--logger:trx", "--no-build", "-c", "release"]
#CMD ["/wait && dotnet test --logger:trx --no-build -c release"]

FROM build AS publish
ARG SRC_DIR
WORKDIR $SRC_DIR/engi-server
RUN dotnet publish -c release --no-build -o /app
COPY --from=rust_builder $SRC_DIR/target/release/libengi_crypto.so /app/lib
COPY --from=rust_builder $SRC_DIR/target/release/libengi_crypto.so /app/lib/libengi_crypto_arm64.so

FROM $DOTNET_CR/aspnet:$DOTNET_VERSION
ARG BIN_DIR
ARG APT_GET_PKGS="jq curl dnsutils"
WORKDIR /app
COPY --from=publish /app .
## Add the wait script to the image
ADD https://github.com/ufoscout/docker-compose-wait/releases/download/2.9.0/wait /wait
RUN chmod +x /wait
COPY scripts/aws-ecs-env $BIN_DIR/aws-ecs-env
RUN apt update && apt-get --no-install-recommends install -y $APT_GET_PKGS
RUN apt autoremove && apt clean
ENTRYPOINT ["aws-ecs-env"]
CMD ["/wait && dotnet Engi.Substrate.Server.dll"]
