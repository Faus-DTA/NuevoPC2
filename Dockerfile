FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["NuevoPC2.csproj", "./"]
RUN dotnet restore "./NuevoPC2.csproj"

COPY . .
WORKDIR "/src/."
RUN dotnet build "NuevoPC2.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NuevoPC2.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Establece el Entrypoint
ENTRYPOINT ["dotnet", "NuevoPC2.dll"]
