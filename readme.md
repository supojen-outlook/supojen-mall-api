
# Manian 電商平台 API 系統

一個基於 .NET 9.0 的現代化電商平台後端 API，採用整潔架構（Clean Architecture）設計模式，提供完整的電商業務功能。

## 📋 目錄

- [專案概述](#專案概述)
- [技術架構](#技術架構)
- [功能模組](#功能模組)
- [檔案結構](#檔案結構)
- [環境需求](#環境需求)
- [本地開發](#本地開發)
- [部署](#部署)
- [使用指南](#使用指南)
- [API 文件](#api-文件)
- [開發指南](#開發指南)
- [授權條款](#授權條款)

## 🎯 專案概述

Manian 是一個功能完整的電商平台後端系統，支援：

- **會員管理**：用戶註冊、登入、權限控制、點數系統
- **商品管理**：商品分類、品牌、屬性、SKU、庫存管理
- **訂單系統**：訂單處理、付款、出貨、退貨
- **促銷活動**：優惠券、折扣、促銷規則
- **購物車**：購物車商品管理
- **資產管理**：檔案上傳與管理

## 🏗️ 技術架構

### 架構模式
採用 **整潔架構（Clean Architecture）** 分層設計：

```
┌─────────────────────────────────────┐
│        Presentation Layer           │ ← API 端點、中間件
├─────────────────────────────────────┤
│       Application Layer             │ ← 業務邏輯、命令處理
├─────────────────────────────────────┤
│         Domain Layer                │ ← 實體、值物件、聚合根
├─────────────────────────────────────┤
│      Infrastructure Layer           │ ← 資料庫、外部服務
└─────────────────────────────────────┘
```

### 技術棧
- **.NET 9.0** - 主要框架
- **ASP.NET Core** - Web API 框架
- **Entity Framework Core** - ORM 資料存取
- **PostgreSQL** - 主要資料庫
- **Docker** - 容器化部署
- **Scalar** - API 文件生成
- **Mapster** - 物件映射
- **MediatR** - CQRS 模式實現

## � 功能模組

### 1. 會員管理 (Membership)
- 用戶註冊與驗證
- 身份認證與授權
- 角色權限管理
- 點數帳戶與交易記錄
- 個人資料管理

### 2. 商品中心 (Product Center)
- 商品分類管理
- 品牌管理
- 商品屬性系統
- 商品與 SKU 管理
- 標籤系統
- 計量單位管理

### 3. 倉庫管理 (Warehouse)
- 庫存管理
- 儲位管理
- 庫存交易記錄
- 入庫配置檔案

### 4. 訂單系統 (Order)
- 訂單管理
- 訂單項目處理
- 出貨管理
- 付款處理
- 退貨管理
- 運費規則

### 5. 促銷活動 (Promotion)
- 促銷活動管理
- 促銷規則設定
- 促銷範圍配置
- 促銷使用記錄

### 6. 購物車 (Cart)
- 購物車商品管理
- 購物車項目操作

### 7. 資產管理 (Asset)
- 檔案上傳與管理
- 媒體資源處理

## � 檔案結構

```
Manian/
├── 📄 README.md                    # 專案說明文件
├── 📄 Manian.sln                   # Visual Studio 解決方案
├── 📄 Dockerfile                   # Docker 映像檔建置腳本
├── 📄 .gitignore                   # Git 忽略檔案配置
├── 📄 .env                         # 環境變數（敏感設定，不進版本控制）
├── � .github/                     # GitHub 配置
│   └── � workflows/
│       └── 📄 docker-publish.yml   # GitHub Actions CI/CD
├── 📁 .vscode/                     # VS Code 配置
├── 📁 src/                         # 原始碼目錄
│   ├── 📁 Manian.Domain/           # 領域層
│   │   ├── 📄 Manian.Domain.csproj
│   │   ├── 📁 Entities/            # 實體類別
│   │   ├── 📁 Repositories/        # 倉儲接口
│   │   ├── 📁 Services/            # 領域服務
│   │   └── 📁 ValueObjects/        # 值物件
│   ├── 📁 Manian.Application/      # 應用層
│   │   ├── 📄 Manian.Application.csproj
│   │   ├── 📁 Commands/            # 命令處理
│   │   ├── 📁 Queries/             # 查詢處理
│   │   ├── 📁 Models/              # 資料傳輸物件
│   │   ├── 📁 Mappers/             # 物件映射
│   │   └── 📁 Services/            # 應用服務
│   ├── 📁 Manian.Infrastructure/   # 基礎設施層
│   │   ├── 📄 Manian.Infrastructure.csproj
│   │   ├── 📁 Persistence/          # 資料存取
│   │   ├── 📁 Repositories/        # 倉儲實現
│   │   ├── 📁 Services/            # 外部服務整合
│   │   └── 📁 Settings/            # 配置設定
│   └── 📁 Manian.Presentation/      # 展示層
│       ├── 📄 Manian.Presentation.csproj
│       ├── 📄 Program.cs           # 應用程式入口點
│       ├── 📁 Endpoints/           # API 端點定義
│       ├── 📁 Extensions/          # 擴展方法
│       └── 📁 Middleware/          # 中間件
└── 📁 sql/                         # 資料庫腳本
    ├── 📁 00-asset/                # 資產管理相關表
    ├──  01-membership/           # 會員管理相關表
    ├── 📁 02-productcenter/        # 商品中心相關表
    ├──  03-warehouse/            # 倉庫管理相關表
    ├──  04-order/                # 訂單系統相關表
    └──  05-promotion/            # 促銷活動相關表
```

## 🔧 環境需求

### 開發環境
- **.NET 9.0 SDK** - 開發框架
- **PostgreSQL** - 資料庫 (建議版本 15+)
- **Docker** - 容器化開發 (可選)
- **Git** - 版本控制

### 生產環境
- **Docker & Docker Compose** - 容器化部署
- **PostgreSQL Server** - 資料庫服務
- **Nginx** - 反向代理 (可選)
- **SSL 憑證** - HTTPS 支援

## � 本地開發

1. **克隆專案**
```bash
git clone <repository-url>
cd Manian
```

2. **安裝依賴**
```bash
dotnet restore Manian.sln
```

3. **配置資料庫**
```bash
# 執行 SQL 腳本建立資料庫結構
psql -U postgres -d manian_db -f sql/01-membership/01-role.sql
# ... 依序執行所有 SQL 檔案
```

4. **設定配置檔案**
```bash
# 編輯 appsettings.Development.json
code src/Manian.Presentation/appsettings.Development.json
```

5. **執行專案**
```bash
dotnet run --project src/Manian.Presentation
```

## 🚀 部署

### CI/CD 流程（GitHub Actions）

本專案使用 GitHub Actions 自動建置並發佈 Docker 映像檔到 GitHub Container Registry (ghcr.io)。

**觸發條件：** 推送標籤 `v*`（例如 `v1.0.0`）

**發佈流程：**
```
推送標籤 v1.0.0
    ↓
GitHub Actions 自動觸發
    ↓
dotnet publish → Docker build → push to ghcr.io
    ↓
映像檔: ghcr.io/{username}/mall-api:v1.0.0
```

**發佈新版本：**
```bash
# 1. 更新版本號標籤
git tag v1.0.0

# 2. 推送標籤到遠端（觸發 CI/CD）
git push origin v1.0.0

# 3. GitHub Actions 自動建置並發佈映像檔
# 可在 Actions 頁面查看進度
```

### 伺服器部署

**1. 準備環境**

在伺服器上建立目錄結構：
```bash
mkdir -p ~/mall-api
cd ~/mall-api
```

建立 `docker-compose.yml`：
```yaml
services:
  mall-api:
    image: ghcr.io/supojen/mall-api:v1.0.0  # 替換為最新版本
    container_name: mall-api
    restart: unless-stopped
    ports:
      - "7175:7175"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:7175
      - TZ=Asia/Taipei
      - ConnectionStrings__Main=${MAIN_CONNECTION_STRING}
      - EmailSettings__SenderName=${EMAIL_SENDER_NAME}
      - EmailSettings__Email=${EMAIL_ACCOUNT}
      - EmailSettings__Password=${EMAIL_PASSWORD}
      - S3__AccessKey=${S3_ACCESS_KEY}
      - S3__Secret=${S3_SECRET}
      - S3__Url=${S3_URL}
      - S3__CDN=${S3_CDN}
    volumes:
      - ./keys/id_aes:/app/id_aes
      - ./keys/id_rsa.v1:/app/id_rsa.v1
      - ./keys/id_rsa.v2:/app/id_rsa.v2
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "3"
```

建立 `.env` 檔案（敏感設定）：
```bash
# 資料庫連線
MAIN_CONNECTION_STRING="Host=localhost;Database=mall_db;Username=...;Password=..."

# Email 設定
EMAIL_SENDER_NAME="Mall API"
EMAIL_ACCOUNT="noreply@example.com"
EMAIL_PASSWORD="your-password"

# S3 設定
S3_ACCESS_KEY="your-access-key"
S3_SECRET="your-secret"
S3_URL="https://s3.amazonaws.com"
S3_CDN="https://cdn.example.com"
```

**2. 生成加密金鑰**
```bash
mkdir -p keys

# 使用 .NET 程式碼生成金鑰，或手動建立
# AES 金鑰（32 bytes）
openssl rand -out keys/id_aes 32

# RSA 金鑰對（需要 PKCS#8 DER 格式）
openssl genrsa -out /tmp/rsa1.pem 2048
openssl pkcs8 -topk8 -inform PEM -outform DER -in /tmp/rsa1.pem -out keys/id_rsa.v1 -nocrypt

openssl genrsa -out /tmp/rsa2.pem 2048
openssl pkcs8 -topk8 -inform PEM -outform DER -in /tmp/rsa2.pem -out keys/id_rsa.v2 -nocrypt
```

**3. 啟動服務**
```bash
docker compose up -d
```

**4. 設定 Nginx（可選）**
```bash
# 編輯 Nginx 設定檔
sudo nano /etc/nginx/conf.d/api.example.com.conf

# 添加反向代理
location /api/ {
    proxy_pass http://127.0.0.1:7175;
    proxy_set_header Host $host;
    proxy_set_header X-Real-IP $remote_addr;
}

# 測試並重載
sudo nginx -t
sudo systemctl reload nginx
```

### 更新版本

當有新版本發佈時：

```bash
cd ~/mall-api

# 1. 編輯 docker-compose.yml，更新映像檔版本
# image: ghcr.io/supojen/mall-api:v1.1.0

# 2. 拉取新映像檔並重啟
docker compose pull
docker compose up -d

# 3. 查看日誌確認正常
docker compose logs -f
```

### 常用指令

```bash
# 查看容器狀態
docker compose ps

# 查看即時日誌
docker compose logs -f

# 重啟服務
docker compose restart

# 停止服務
docker compose down

# 進入容器除錯
docker compose exec mall-api sh

# 清理舊映像檔
docker image prune -f
```

## 📖 使用指南

### API 端點

系統提供 RESTful API，主要端點包括：

#### 會員管理
- `POST /api/identity/signup` - 用戶註冊
- `POST /api/identity/signin` - 用戶登入
- `POST /api/identity/signout` - 用戶登出
- `GET /api/members/profile` - 獲取個人資料
- `PUT /api/members/profile` - 更新個人資料

#### 商品管理
- `GET /api/categories` - 獲取商品分類
- `POST /api/categories` - 新增商品分類
- `GET /api/products` - 獲取商品列表
- `POST /api/products` - 新增商品
- `GET /api/skus` - 獲取 SKU 列表

#### 訂單管理
- `GET /api/orders` - 獲取訂單列表
- `POST /api/orders` - 建立訂單
- `GET /api/orders/{id}` - 獲取訂單詳情
- `PUT /api/orders/{id}` - 更新訂單

#### 購物車
- `GET /api/cart/items` - 獲取購物車內容
- `POST /api/cart/items` - 新增商品到購物車
- `PUT /api/cart/items/{id}` - 更新購物車項目
- `DELETE /api/cart/items/{id}` - 移除購物車項目

### API 文件

開發環境中可訪問：
- **Scalar UI**: `http://localhost:7175/scalar/v1`
- **OpenAPI 規範**: `http://localhost:7175/openapi/v1.json`

### 認證與授權

系統使用 Cookie 認證機制：
- 登入後會設置認證 Cookie
- API 需要適當的權限才能訪問
- 支援角色基礎的授權控制

### 錯誤處理

API 使用統一的錯誤回應格式：
```json
{
  "success": false,
  "message": "錯誤訊息",
  "errors": ["詳細錯誤資訊"],
  "timestamp": "2024-01-01T00:00:00Z"
}
```

## 🛠️ 開發指南

### 程式碼規範

- 遵循 C# 命名慣例
- 使用整潔架構分層原則
- 實作 CQRS 模式
- 使用依賴注入
- 編寫單元測試

### 資料庫遷移

1. 建立新的遷移腳本在 `sql/` 目錄下
2. 按照模組編號順序命名
3. 包含向上和向下遷移腳本

### 新增功能模組

1. **Domain Layer**: 定義實體和值物件
2. **Application Layer**: 實現命令和查詢處理
3. **Infrastructure Layer**: 實現倉儲和外部服務
4. **Presentation Layer**: 定義 API 端點

### 測試

```bash
# 執行單元測試
dotnet test

# 執行整合測試
dotnet test --filter "Category=Integration"
```

## � 安全性

### 資料加密
- 使用 AES 加密敏感資料
- RSA 金鑰用於非對稱加密
- 金鑰檔案存放在 `keys/` 目錄

### 認證安全
- CSRF 保護
- 安全的 Cookie 設定
- 密碼雜湊處理

### 網路安全
- HTTPS 強制使用
- CORS 跨域保護
- 請求速率限制

## 📝 授權條款

```
Manian 電商平台 API 系統
Copyright (C) 2024 Manian Project

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
```

---

## 📞 支援與貢獻

如有問題或建議，請透過以下方式聯繫：

- 提交 Issue：回報錯誤或功能請求
- 提交 Pull Request：貢獻程式碼
- 查看文件：獲取詳細使用說明

---

**感謝使用 Manian 電商平台！** 🎉

