FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY src/EMS.Core/EMS.Core.csproj EMS.Core/
COPY src/EMS.Infrastructure/EMS.Infrastructure.csproj EMS.Infrastructure/
COPY src/EMS.Shared/EMS.Shared.csproj EMS.Shared/
COPY src/EMS.BlazorWASM/EMS.BlazorWASM.csproj EMS.BlazorWASM/
COPY src/EMS.Mvc/EMS.Mvc.csproj EMS.Mvc/

RUN dotnet restore EMS.Mvc/EMS.Mvc.csproj

# Copy source code
COPY src/EMS.Core/ EMS.Core/
COPY src/EMS.Infrastructure/ EMS.Infrastructure/
COPY src/EMS.Shared/ EMS.Shared/
COPY src/EMS.BlazorWASM/ EMS.BlazorWASM/
COPY src/EMS.Mvc/ EMS.Mvc/

# Publish
WORKDIR /src/EMS.Mvc
RUN dotnet publish -c Release -o /app/publish

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:7115
EXPOSE 7115

ENTRYPOINT ["dotnet", "EMS.Mvc.dll"]
