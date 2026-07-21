FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY src/EMS.Core/EMS.Core.csproj EMS.Core/
COPY src/EMS.Infrastructure/EMS.Infrastructure.csproj EMS.Infrastructure/
COPY src/EMS.Shared/EMS.Shared.csproj EMS.Shared/
COPY src/EMS.WebAPI/EMS.WebAPI.csproj EMS.WebAPI/

RUN dotnet restore EMS.WebAPI/EMS.WebAPI.csproj

# Copy source code
COPY src/EMS.Core/ EMS.Core/
COPY src/EMS.Infrastructure/ EMS.Infrastructure/
COPY src/EMS.Shared/ EMS.Shared/
COPY src/EMS.WebAPI/ EMS.WebAPI/

# Publish
WORKDIR /src/EMS.WebAPI
RUN dotnet publish -c Release -o /app/publish

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:7296
EXPOSE 7296

ENTRYPOINT ["dotnet", "EMS.WebAPI.dll"]
