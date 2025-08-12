FROM mcr.microsoft.com/dotnet/aspnet:9.0

WORKDIR /app

COPY ./publish_output .

ENTRYPOINT ["dotnet", "TodoWebApi.Api.dll"]