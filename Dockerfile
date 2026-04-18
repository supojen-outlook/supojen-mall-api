# 伺服器用 Dockerfile - 只包裝已建置好的檔案
FROM mcr.microsoft.com/dotnet/aspnet:9.0

# 設定工作目錄
WORKDIR /app

# 複製建置好的檔案
COPY publish/ .

# 啟動 ASP.NET Core
ENTRYPOINT ["dotnet", "Manian.Presentation.dll"]