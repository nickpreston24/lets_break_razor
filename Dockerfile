FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
RUN dotnet tool install -g Microsoft.Web.LibraryManager.Cli
ENV PATH="$PATH:/root/.dotnet/tools"
WORKDIR /src
COPY ["lets_break_razor.csproj", "./"]
COPY ["nuget.config", "./"]
RUN dotnet restore "lets_break_razor.csproj"
COPY . .
WORKDIR "/src/"
RUN libman restore
RUN dotnet build "lets_break_razor.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "lets_break_razor.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "lets_break_razor.dll"]
