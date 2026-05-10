FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Funds.Api.csproj", "./"]
RUN dotnet restore "Funds.Api.csproj"

COPY . .
RUN dotnet publish "Funds.Api.csproj" -c Release -o /app/publish

FROM runtime AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Funds.Api.dll"]