FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["CRM_ERP_UMG.csproj", "."]
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 10000
ENV ASPNETCORE_URLS=http://+:10000
ENTRYPOINT ["dotnet", "CRM_ERP_UMG.dll"]
