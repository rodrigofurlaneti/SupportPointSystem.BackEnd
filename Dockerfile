﻿# ESTÁGIO 1: Build e Publish
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia os arquivos de projeto primeiro para otimizar o cache de camadas
COPY ["src/FSI.SupportPointSystem.Api/FSI.SupportPointSystem.Api.csproj", "src/FSI.SupportPointSystem.Api/"]
COPY ["src/FSI.SupportPointSystem.Application/FSI.SupportPointSystem.Application.csproj", "src/FSI.SupportPointSystem.Application/"]
COPY ["src/FSI.SupportPointSystem.Domain/FSI.SupportPointSystem.Domain.csproj", "src/FSI.SupportPointSystem.Domain/"]
COPY ["src/FSI.SupportPointSystem.Infrastructure/FSI.SupportPointSystem.Infrastructure.csproj", "src/FSI.SupportPointSystem.Infrastructure/"]

# Restaura as dependências
RUN dotnet restore "src/FSI.SupportPointSystem.Api/FSI.SupportPointSystem.Api.csproj"

# Copia todo o restante do código e compila
COPY . .
WORKDIR "/src/FSI.SupportPointSystem.Api"
RUN dotnet publish "FSI.SupportPointSystem.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ESTÁGIO 2: Runtime (Imagem final leve)
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
EXPOSE 8080
EXPOSE 443
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FSI.SupportPointSystem.Api.dll"]