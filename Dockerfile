FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["NoteApi/NoteApi.csproj", "NoteApi/"]
RUN dotnet restore "NoteApi/NoteApi.csproj"

COPY . .
WORKDIR "/src/NoteApi"
RUN dotnet build "NoteApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NoteApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .

# Ensure appsettings.json is copied
COPY ["NoteApi/appsettings.json", "./"]

RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

EXPOSE 80

ENTRYPOINT ["dotnet", "NoteApi.dll"]