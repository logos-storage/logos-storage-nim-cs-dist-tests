FROM mcr.microsoft.com/dotnet/sdk:10.0

RUN apt-get update && apt-get install -y curl && \
    curl -sL https://github.com/digitalocean/doctl/releases/download/v1.154.0/doctl-1.154.0-linux-amd64.tar.gz | tar -xz -C /usr/local/bin && \
    rm -rf /var/lib/apt/lists/*

COPY --chmod=0755 docker/docker-entrypoint.sh /

ENTRYPOINT ["/docker-entrypoint.sh"]
CMD ["dotnet", "test"]

