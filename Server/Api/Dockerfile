FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Development
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Api/InstanceService.Api.csproj", "Api/"]
COPY ["Data/InstanceService.Data.csproj", "Data/"]
COPY ["Domain/InstanceService.Domain.csproj", "Domain/"]
COPY ["Models/InstanceService.Models.csproj", "Models/"]

RUN --mount=type=secret,id=nugetconfig \
    dotnet restore "Api/InstanceService.Api.csproj" --configfile /run/secrets/nugetconfig

COPY . .

WORKDIR "/src/Api"
RUN dotnet build "InstanceService.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "InstanceService.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "InstanceService.Api.dll"]
