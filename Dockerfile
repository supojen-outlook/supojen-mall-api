# 伺服器用 Dockerfile - 只包裝已建置好的檔案
FROM mcr.microsoft.com/dotnet/aspnet:9.0

WORKDIR /app

# 複製建置好的檔案
COPY publish/ .

# 複製金鑰檔案
COPY keys/id_aes .
COPY keys/id_rsa.v1 .
COPY keys/id_rsa.v2 .

# 環境變數
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true

# 暴露端口
EXPOSE 8080
EXPOSE 8081

ENTRYPOINT ["dotnet", "Manian.Presentation.dll"]