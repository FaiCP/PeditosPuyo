FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

COPY PuyoDelivery.Core/*.csproj PuyoDelivery.Core/
COPY PuyoDelivery.Infrastructure/*.csproj PuyoDelivery.Infrastructure/
COPY PuyoDelivery.API/*.csproj PuyoDelivery.API/
RUN dotnet restore PuyoDelivery.API/PuyoDelivery.API.csproj

COPY PuyoDelivery.Core/ PuyoDelivery.Core/
COPY PuyoDelivery.Infrastructure/ PuyoDelivery.Infrastructure/
COPY PuyoDelivery.API/ PuyoDelivery.API/
RUN dotnet publish PuyoDelivery.API/PuyoDelivery.API.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENTRYPOINT ["dotnet", "PuyoDelivery.API.dll"]
