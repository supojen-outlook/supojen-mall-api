#!/bin/bash

# deploy.sh - Manian .NET API 專案部署到 nginx-proxy (Docker 版本)
# 注意：這個腳本假設已經解壓縮完成，publish/、keys/ 等檔案已存在

# ===== 設定顏色輸出 =====
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# ===== 設定參數 =====
DOMAIN="manian.supojen.com"                    # 你的 Manian API 網域（預設值）
API_PROJECT_DIR="$(pwd)"                        # API 專案目錄（目前位置）
NGINX_PROXY_DIR="$(dirname "$API_PROJECT_DIR")/nginx-proxy"  # nginx-proxy 目錄
# =========================================

# 顯示標題
echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}    Manian .NET API 部署工具 (Docker 版本)${NC}"
echo -e "${BLUE}========================================${NC}"
echo -e "${YELLOW}預設網域:${NC} $DOMAIN"
echo -e "${YELLOW}API 專案:${NC} $API_PROJECT_DIR"
echo -e "${YELLOW}Nginx Proxy:${NC} $NGINX_PROXY_DIR"
echo -e "${BLUE}========================================${NC}\n"

# ===== 確認 domain name =====
confirm_domain() {
    echo -e "${YELLOW}[確認] 請確認網域設定...${NC}"
    
    while true; do
        echo -e "目前的網域為: ${GREEN}$DOMAIN${NC}"
        read -p "這個網域是否正確？(y/n) " -n 1 -r
        echo
        
        if [[ $REPLY =~ ^[Yy]$ ]]; then
            echo -e "${GREEN}✅ 網域確認: $DOMAIN${NC}\n"
            break
        elif [[ $REPLY =~ ^[Nn]$ ]]; then
            read -p "請輸入正確的網域: " NEW_DOMAIN
            if [ -n "$NEW_DOMAIN" ]; then
                DOMAIN="$NEW_DOMAIN"
                echo -e "${GREEN}✅ 網域已更新為: $DOMAIN${NC}\n"
                break
            else
                echo -e "${RED}❌ 網域不能為空，請重新輸入${NC}"
            fi
        else
            echo -e "${RED}❌ 請輸入 y 或 n${NC}"
        fi
    done
}

# 檢查必要目錄
check_directories() {
    echo -e "${YELLOW}[1/5] 檢查目錄結構...${NC}"
    
    # 檢查必要檔案是否存在（已經解壓縮後的狀態）
    if [ ! -d "publish" ]; then
        echo -e "${RED}❌ publish 目錄不存在！${NC}"
        echo -e "請先解壓縮部署包：tar -xzf manian-deploy.tar.gz"
        exit 1
    fi
    
    if [ ! -f "docker-compose.yaml" ]; then
        echo -e "${RED}❌ docker-compose.yaml 不存在${NC}"
        exit 1
    fi
    
    if [ ! -d "$NGINX_PROXY_DIR" ]; then
        echo -e "${RED}❌ nginx-proxy 目錄不存在: $NGINX_PROXY_DIR${NC}"
        exit 1
    fi
    
    # 檢查 Makefile 是否存在
    if [ ! -f "$NGINX_PROXY_DIR/Makefile" ]; then
        echo -e "${RED}❌ nginx-proxy/Makefile 不存在${NC}"
        exit 1
    fi
    
    # 檢查 Docker 容器是否在運行
    if ! docker ps | grep -q nginx-proxy; then
        echo -e "${YELLOW}⚠️  Nginx 容器未運行，嘗試啟動...${NC}"
        cd "$NGINX_PROXY_DIR" && docker compose up -d
        sleep 3
    fi
    
    echo -e "${GREEN}✅ 目錄檢查通過${NC}"
    echo -e "  API 專案: $API_PROJECT_DIR"
    echo -e "  Nginx Proxy: $NGINX_PROXY_DIR\n"
}

# 啟動 API 容器
start_api_container() {
    echo -e "${YELLOW}[2/5] 啟動 API 容器...${NC}"
    
    # 停止舊容器
    echo -e "停止舊容器..."
    docker compose down || true
    
    # 重新建置映像檔
    echo -e "重新建置映像檔..."
    docker compose build --no-cache
    
    # 啟動新容器
    echo -e "啟動新容器..."
    docker compose up -d
    
    # 清理未使用的映像檔
    echo -e "清理未使用的映像檔..."
    docker image prune -f
    
    echo -e "${GREEN}✅ Manian API 容器啟動成功${NC}\n"
    
    # 等待容器完全啟動
    sleep 3
}

# 檢查 API 是否正常
check_api_health() {
    echo -e "${YELLOW}[3/5] 檢查 API 健康狀態...${NC}"
    
    # 嘗試連線 API 的健康檢查端點
    MAX_RETRIES=5
    RETRY_COUNT=0
    
    while [ $RETRY_COUNT -lt $MAX_RETRIES ]; do
        if curl -s -f http://localhost:7175/api/health > /dev/null 2>&1; then
            echo -e "${GREEN}✅ Manian API 健康檢查通過${NC}\n"
            return 0
        else
            RETRY_COUNT=$((RETRY_COUNT + 1))
            if [ $RETRY_COUNT -lt $MAX_RETRIES ]; then
                echo -e "${YELLOW}⏳ 等待 API 啟動... (${RETRY_COUNT}/${MAX_RETRIES})${NC}"
                sleep 2
            fi
        fi
    done
    
    echo -e "${RED}❌ Manian API 健康檢查失敗${NC}"
    echo -e "請手動檢查容器狀態："
    echo -e "  docker compose logs"
    exit 1
}

# 建立 Nginx 虛擬主機設定
create_nginx_config() {
    echo -e "${YELLOW}[4/5] 建立 Nginx 虛擬主機設定...${NC}"
    
    CONFIG_FILE="$NGINX_PROXY_DIR/nginx/conf.d/$DOMAIN.conf"
    
    # 備份舊設定
    if [ -f "$CONFIG_FILE" ]; then
        echo -e "${YELLOW}⚠️  設定檔已存在，將備份為 $DOMAIN.conf.backup${NC}"
        cp "$CONFIG_FILE" "$CONFIG_FILE.backup"
    fi
    
    # 產生設定檔
    cat > "$CONFIG_FILE" << EOF
server {
    listen 80;
    listen [::]:80;
    server_name $DOMAIN;
    
    # Let's Encrypt 驗證路徑（給 certbot 用）
    location ^~ /.well-known/acme-challenge/ {
        root /var/www/certbot;
        default_type "text/plain";
        try_files \$uri =404;
    }
    
    # 其他 HTTP 請求重定向到 HTTPS
    location / {
        return 301 https://\$host\$request_uri;
    }
}

server {
    listen 443 ssl;
    listen [::]:443 ssl;
    http2 on;
    server_name $DOMAIN;

    # SSL 證書配置（容器內的路徑）
    ssl_certificate /etc/letsencrypt/live/$DOMAIN/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/$DOMAIN/privkey.pem;
    
    # 引入 SSL 參數（從 ssl-params.conf）
    include /etc/nginx/conf.d/ssl-params.conf;

    # API 服務
    location /api {
        proxy_pass http://api:5000;
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
        client_max_body_size 30M;
    }

    # WebSocket 服務
    location /ws {
        proxy_pass http://api:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
        proxy_read_timeout 86400s;
        proxy_send_timeout 86400s;
    }
}
EOF
    
    echo -e "${GREEN}✅ 虛擬主機設定完成${NC}"
    echo -e "設定檔位置: $CONFIG_FILE\n"
}

# ===== 檢查 SSL 憑證 =====
check_ssl_certificate() {
    echo -e "${YELLOW}[5/5] 檢查 SSL 憑證...${NC}"
    
    cd "$NGINX_PROXY_DIR"
    
    if make cert-check DOMAIN="$DOMAIN" > /dev/null 2>&1; then
        echo -e "${GREEN}✅ SSL 憑證已存在${NC}"
        
        CERT_FILE="certbot/letsencrypt/live/$DOMAIN/fullchain.pem"
        if [ -f "$CERT_FILE" ]; then
            EXPIRY=$(openssl x509 -enddate -noout -in "$CERT_FILE" 2>/dev/null | cut -d= -f2)
            echo -e "  網域: $DOMAIN"
            echo -e "  到期日: $EXPIRY"
        fi
        echo -e "  證書路徑: certbot/letsencrypt/live/$DOMAIN/\n"
    else
        echo -e "${YELLOW}⚠️  SSL 憑證不存在${NC}"
        echo -e "請確認以下事項："
        echo -e "  1. 域名 $DOMAIN 的 DNS 已指向本機"
        echo -e "  2. 防火牆已開放 80 和 443 埠"
        echo ""
        read -p "是否要現在申請 SSL 憑證？(y/n) " -n 1 -r
        echo
        if [[ $REPLY =~ ^[Yy]$ ]]; then
            echo -e "申請 SSL 憑證中...\n"
            if make add-domain DOMAIN="$DOMAIN"; then
                echo -e "${GREEN}✅ SSL 憑證申請成功${NC}\n"
            else
                echo -e "${RED}❌ SSL 憑證申請失敗${NC}"
                exit 1
            fi
        else
            echo -e "${RED}❌ 需要 SSL 憑證才能繼續部署${NC}"
            exit 1
        fi
    fi
}

# 重新載入 Nginx
reload_nginx() {
    echo -e "${YELLOW}[額外步驟] 重新載入 Nginx...${NC}"
    
    cd "$NGINX_PROXY_DIR"
    
    echo -e "測試 Nginx 配置..."
    if ! docker exec nginx-proxy nginx -t; then
        echo -e "${RED}❌ Nginx 配置測試失敗${NC}"
        exit 1
    fi
    
    echo -e "重新載入 Nginx..."
    if docker exec nginx-proxy nginx -s reload; then
        echo -e "${GREEN}✅ Nginx 重新載入成功${NC}\n"
    else
        echo -e "${RED}❌ Nginx 重新載入失敗${NC}"
        exit 1
    fi
}

# 顯示部署資訊
show_summary() {
    echo -e "${BLUE}========================================${NC}"
    echo -e "${GREEN}✅ Manian API 部署完成！${NC}"
    echo -e "${BLUE}========================================${NC}"
    echo -e "${YELLOW}API 網域:${NC} https://$DOMAIN"
    echo -e "${YELLOW}API 端點:${NC}"
    echo -e "  - API: https://$DOMAIN/api"
    echo -e "  - WebSocket: wss://$DOMAIN/ws"
    echo -e "  - Local Port: 7175"
    echo -e "${BLUE}========================================${NC}"
}

# ===== 主程式 =====
main() {
    START_TIME=$(date +%s)
    
    confirm_domain
    check_directories
    start_api_container
    check_api_health
    create_nginx_config
    check_ssl_certificate
    reload_nginx
    
    END_TIME=$(date +%s)
    DURATION=$((END_TIME - START_TIME))
    
    show_summary
    echo -e "${GREEN}✨ Manian API 部署花費: ${DURATION} 秒${NC}"
}

main