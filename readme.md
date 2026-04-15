
# Manian 電商平台 API 系統

一個基於 .NET 9.0 的現代化電商平台後端 API，採用整潔架構（Clean Architecture）設計模式，提供完整的電商業務功能。

## 📋 目錄

- [專案概述](#專案概述)
- [技術架構](#技術架構)
- [功能模組](#功能模組)
- [檔案結構](#檔案結構)
- [環境需求](#環境需求)
- [安裝與部署](#安裝與部署)
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
├── 📄 docker-compose.yaml          # Docker Compose 配置
├── 📄 build.sh                     # 本地建置腳本
├── 📄 deploy.sh                    # 部署腳本
├── 📄 undeploy.sh                  # 解除部署腳本
├── 📄 Makefile                     # 建置任務自動化
├── 📄 keygen-tool                  # 金鑰生成工具
├── 📄 .gitignore                   # Git 忽略檔案配置
├── 📁 .vscode/                     # VS Code 配置
├── 📁 src/                         # 原始碼目錄
│   ├── 📁 Manian.Domain/           # 領域層
│   │   ├── 📄 Manian.Domain.csproj
│   │   ├── 📁 Entities/            # 實體類別
│   │   │   ├── 📁 Assets/
│   │   │   ├── 📁 Carts/
│   │   │   ├── 📁 Memberships/
│   │   │   ├── 📁 Orders/
│   │   │   ├── 📁 Products/
│   │   │   ├── 📁 Promotions/
│   │   │   └── 📁 Warehouses/
│   │   ├── 📁 Repositories/        # 倉儲接口
│   │   ├── 📁 Services/            # 領域服務
│   │   └── 📁 ValueObjects/        # 值物件
│   ├── 📁 Manian.Application/      # 應用層
│   │   ├── 📄 Manian.Application.csproj
│   │   ├── 📁 Commands/            # 命令處理
│   │   │   ├── 📁 Assets/
│   │   │   ├── 📁 Carts/
│   │   │   ├── 📁 Memberships/
│   │   │   ├── 📁 Orders/
│   │   │   ├── 📁 Products/
│   │   │   ├── 📁 Promotions/
│   │   │   └── 📁 Warehouses/
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
│       │   ├── 📁 Memberships/
│       │   ├── 📁 Orders/
│       │   ├── 📁 Products/
│       │   ├── 📁 Promotions/
│       │   └── 📁 Warehouses/
│       ├── 📁 Extensions/          # 擴展方法
│       └── 📁 Middleware/          # 中間件
├── 📁 sql/                         # 資料庫腳本
│   ├── 📁 00-asset/                # 資產管理相關表
│   │   ├── 📄 01-asset.sql
│   ├── 📁 01-membership/           # 會員管理相關表
│   │   ├── 📄 01-role.sql
│   │   ├── 📄 02-user.sql
│   │   ├── 📄 03-user_role.sql
│   │   ├── 📄 04-identity.sql
│   │   ├── 📄 05-point_account.sql
│   │   ├── 📄 06-point_transaction.sql
│   │   └── 📄 07-maintenance.sql
│   ├── 📁 02-productcenter/        # 商品中心相關表
│   │   ├── 📄 01-unit_of_meaure.sql
│   │   ├── 📄 02-category.sql
│   │   ├── 📄 03-brand.sql
│   │   ├── 📄 04-attribute_key.sql
│   │   ├── 📄 05-attribute_value.sql
│   │   ├── 📄 06-category_attributes.sql
│   │   ├── 📄 07-product.sql
│   │   ├── 📄 08-sku.sql
│   │   ├── 📄 09-product_attributes.sql
│   │   ├── 📄 10-sku_attributes.sql
│   │   └── 📄 11-tag.sql
│   ├── 📁 03-warehouse/            # 倉庫管理相關表
│   │   ├── 📄 01-location.sql
│   │   ├── 📄 02-inventory.sql
│   │   ├── 📄 03-inventory_transaction.sql
│   │   └── 📄 04-putaway_profile.sql
│   ├── 📁 04-order/                # 訂單系統相關表
│   │   ├── 📄 00-shipping_rule.sql
│   │   ├── 📄 01-order.sql
│   │   ├── � 02-order_item.sql
│   │   ├── 📄 03-shipment.sql
│   │   ├── 📄 04-payment.sql
│   │   ├── 📄 05-pick_item.sql
│   │   ├── 📄 06-return.sql
│   │   └── 📄 README.md
│   └── 📁 05-promotion/            # 促銷活動相關表
│       ├── 📄 01-promotion.sql
│       ├── 📄 02-promotion_rule.sql
│       ├── 📄 03-promotion_scope.sql
│       └── 📄 04-promotion_usage.sql
└── 📁 deploy/                      # 部署相關檔案（建置後產生）
    ├── 📁 publish/                 # 發佈檔案
    ├── 📁 keys/                    # 加密金鑰
    └── 📄 README.md                # 部署說明
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

## 🚀 安裝與部署

### 本地開發環境設置

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
# 複製並編輯配置檔案
cp src/Manian.Presentation/appsettings.example.json src/Manian.Presentation/appsettings.json
# 編輯資料庫連線字串等設定
```

**appsettings.json 配置範例：**
```json
{
  "ConnectionStrings": {
    "Main": "Host=localhost;Database=manian_db;Username=your_username;Password=your_password;Port=5432"
  },
  "EmailSettings": {
    "Server": "smtp.gmail.com",
    "Port": 587,
    "Email": "noreply@yourapp.com",
    "Password": "your-app-password",
    "SenderName": "Manian系統通知",
    "UseSsl": true
  },
  "S3Settings": {
    "AccessKey": "your-access-key",
    "SecretKey": "your-secret-key",
    "BucketName": "your-bucket-name",
    "Region": "ap-northeast-1",
    "Endpoint": "https://s3.amazonaws.com"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

5. **執行專案**
```bash
dotnet run --project src/Manian.Presentation
```

### 生產環境部署

#### 推薦部署流程

**步驟 1：本地建置部署包**
```bash
# 在本地機器上執行
chmod +x build.sh
./build.sh
```
這會生成 `manian-deploy.tar.gz` 部署包。

**步驟 2：上傳到伺服器**
```bash
# 上傳部署包到伺服器
scp manian-deploy.tar.gz user@your-server:/app/
```

**步驟 3：伺服器端部署**
```bash
# SSH 連接到伺服器並執行部署
ssh user@your-server
cd /app
tar -xzf manian-deploy.tar.gz
make deploy
```

#### 其他部署方法

##### 方法一：直接使用腳本（不推薦）
```bash
# 在伺服器上直接執行
./deploy.sh
```

##### 方法二：使用 Docker
```bash
# 建置映像檔
docker build -t manian:latest .

# 執行容器
docker-compose up -d
```

#### 伺服器日常管理

部署完成後，使用 Makefile 進行日常管理：
```bash
# 查看容器狀態
make ps

# 查看即時日誌
make logs

# 重啟服務
make restart

# 進入容器除錯
make shell

# 檢查系統狀態
make status

# 清理未使用的映像檔
make clean

# 移除部署
make undeploy
```

#### 自動化部署腳本說明

**build.sh** - 本地建置腳本
- 還原 NuGet 套件
- 建置並發佈專案
- 生成加密金鑰
- 打包部署檔案

**deploy.sh** - 伺服器部署腳本
- 檢查部署環境
- 啟動 Docker 容器
- 配置 Nginx 反向代理
- 設定 SSL 憑證
- 健康檢查

**undeploy.sh** - 解除部署腳本
- 停止並移除容器
- 清理相關資源
- 移除 Nginx 配置

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

