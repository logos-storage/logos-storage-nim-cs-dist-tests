FROM mcr.microsoft.com/dotnet/sdk:10.0

COPY --chmod=0755 docker/docker-entrypoint.sh /

ENTRYPOINT ["/docker-entrypoint.sh"]
CMD ["dotnet", "test"]

