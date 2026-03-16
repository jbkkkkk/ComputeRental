FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY *.sln .

COPY src/API/*.csproj src/API/
COPY src/Application/*.csproj src/Application/
COPY src/Domain/*.csproj src/Domain/
COPY src/Infrastructure/*.csproj src/Infrastructure/

RUN dotnet restore

COPY . .

WORKDIR /src/src/API
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
EXPOSE 80
EXPOSE 443

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "API.dll"]