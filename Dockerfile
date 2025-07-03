FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
ARG PROJECT_NAME
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG PROJECT_NAME
WORKDIR /src
COPY . . 
WORKDIR "/src"
RUN dotnet restore "$PROJECT_NAME.csproj"
RUN dotnet build "$PROJECT_NAME.csproj" --no-restore -c Release -o /app/build

FROM build AS publish
ARG PROJECT_NAME
WORKDIR "/src"
RUN dotnet publish "$PROJECT_NAME.csproj" --no-restore -c Release -o /app/publish
COPY appsettings.* /app/publish/

FROM base AS final
ARG PROJECT_NAME
ENV EXE_NAME=${PROJECT_NAME}.dll
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT dotnet $EXE_NAME
# ENTRYPOINT ["tail", "-f", "/dev/null"]
