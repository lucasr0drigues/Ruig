FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Ruig.sln ./
COPY src/Ruig.Api/Ruig.Api.csproj src/Ruig.Api/
COPY src/Ruig.Application/Ruig.Application.csproj src/Ruig.Application/
COPY src/Ruig.Domain/Ruig.Domain.csproj src/Ruig.Domain/
COPY src/Ruig.Infrastructure/Ruig.Infrastructure.csproj src/Ruig.Infrastructure/

RUN dotnet restore src/Ruig.Api/Ruig.Api.csproj

COPY . .

RUN dotnet publish src/Ruig.Api/Ruig.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM build AS migrations
RUN dotnet tool install --global dotnet-ef --version 10.0.0
ENV PATH="${PATH}:/root/.dotnet/tools"
ENTRYPOINT ["dotnet", "ef", "database", "update", "--project", "src/Ruig.Infrastructure/Ruig.Infrastructure.csproj", "--startup-project", "src/Ruig.Api/Ruig.Api.csproj"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

USER $APP_UID
ENTRYPOINT ["dotnet", "Ruig.Api.dll"]
