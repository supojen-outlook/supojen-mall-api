#!/bin/bash

# undeploy.sh - Manian .NET API 專案解除部署腳本

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
BACKUP_DIR="backup.$(date +%Y%m%d_%H%M%S)"      # 備份目錄
# =========================================

# 顯示標題
echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}    Manian .NET API 解除部署工具${NC}"
echo -e "${BLUE}========================================${NC}"
echo -e "${YELLOW}警告：這個腳本會移除 Manian API 容器和相關設定${NC}"
echo -e "${YELLOW}網域:${NC} $DOMAIN"
echo -e "${YELLOW}API 專案:${NC} $API_PROJECT_DIR"
echo -e "${YELLOW}Nginx Proxy:${NC} $NGINX_PROXY_DIR"
echo -e "${BLUE}========================================${NC}\n"

# ===== 確認 domain name =====
confirm_domain() {
    echo -e "${YELLOW}[確認] 請確認要移除的網域...${NC}"
    
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

# ===== 最終確認 =====
final_confirmation() {
    echo -e "${RED}⚠️  危險操作警告 ⚠️${NC}"
    echo -e "這個操作將會："
    echo -e "  1. 停止並移除 Manian API 容器"
    echo -e "  2. 移除 Manian API 的 Docker 映像檔"
    echo -e "  3. 移除 Nginx 設定檔 ($DOMAIN.conf)"
    echo -e "  4. 備份目前的部署檔案到 $BACKUP_DIR/"
    echo ""
    
    read -p "確定要繼續嗎？(y/N) " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        echo -e "${YELLOW}⚠️  操作已取消${NC}"
        exit 0
    fi
}

# 備份目前的部署檔案
backup_current_deployment() {
    echo -e "${YELLOW}[1/5] 備份目前的部署檔案...${NC}"
    
    mkdir -p "$BACKUP_DIR"
    
    # 備份重要檔案
    [ -d "publish" ] && cp -r publish "$BACKUP_DIR/" 2>/dev/null && echo -e "  ✅ 備份 publish/"
    [ -d "keys" ] && cp -r keys "$BACKUP_DIR/" 2>/dev/null && echo -e "  ✅ 備份 keys/"
    [ -f "docker-compose.yaml" ] && cp docker-compose.yaml "$BACKUP_DIR/" 2>/dev/null && echo -e "  ✅ 備份 docker-compose.yaml"
    [ -f "Dockerfile" ] && cp Dockerfile "$BACKUP_DIR/" 2>/dev/null && echo -e "  ✅ 備份 Dockerfile"
    [ -f "deploy.sh" ] && cp deploy.sh "$BACKUP_DIR/" 2>/dev/null && echo -e "  ✅ 備份 deploy.sh"
    [ -f "manian-deploy.tar.gz" ] && cp manian-deploy.tar.gz "$BACKUP_DIR/" 2>/dev/null && echo -e "  ✅ 備份 manian-deploy.tar.gz"
    
    echo -e "${GREEN}✅ 備份完成，備份位置: $BACKUP_DIR/${NC}\n"
}

# 停止並移除 API 容器
stop_and_remove_container() {
    echo -e "${YELLOW}[2/5] 停止並移除 API 容器...${NC}"
    
    if [ -f "docker-compose.yaml" ]; then
        echo -e "停止容器..."
        docker compose down -v  # -v 也會移除 volumes
        
        # 確認容器是否已移除
        if ! docker ps -a | grep -q "manian\|your-api"; then
            echo -e "${GREEN}✅ 容器已移除${NC}"
        else
            echo -e "${YELLOW}⚠️  強制移除容器...${NC}"
            docker compose down --rmi all -v
        fi
    else
        echo -e "${YELLOW}⚠️  找不到 docker-compose.yaml，嘗試直接移除容器...${NC}"
        docker stop manian-api 2>/dev/null && docker rm manian-api 2>/dev/null
    fi
    
    echo -e "${GREEN}✅ API 容器處理完成${NC}\n"
}

# 移除 Docker 映像檔
remove_docker_image() {
    echo -e "${YELLOW}[3/5] 移除 Docker 映像檔...${NC}"
    
    # 移除 api:latest 映像檔
    if docker images | grep -q "manian.*latest"; then
        echo -e "找到 manian:latest 映像檔，移除中..."
        docker rmi manian:latest 2>/dev/null || true
    fi
    
    # 移除懸浮映像檔
    echo -e "清理懸浮映像檔..."
    docker image prune -f
    
    echo -e "${GREEN}✅ Docker 映像檔處理完成${NC}\n"
}

# 移除 Nginx 設定檔
remove_nginx_config() {
    echo -e "${YELLOW}[4/5] 移除 Nginx 設定檔...${NC}"
    
    CONFIG_FILE="$NGINX_PROXY_DIR/nginx/conf.d/$DOMAIN.conf"
    
    if [ -f "$CONFIG_FILE" ]; then
        # 備份設定檔到目前目錄
        cp "$CONFIG_FILE" "./$DOMAIN.conf.backup.$(date +%Y%m%d_%H%M%S)"
        echo -e "已備份設定檔到目前目錄"
        
        # 移除設定檔
        rm -f "$CONFIG_FILE"
        echo -e "${GREEN}✅ 已移除 Nginx 設定檔: $CONFIG_FILE${NC}"
        
        # 重新載入 Nginx
        echo -e "重新載入 Nginx 套用變更..."
        cd "$NGINX_PROXY_DIR" && docker exec nginx-proxy nginx -s reload
        cd "$API_PROJECT_DIR"
    else
        echo -e "${YELLOW}⚠️  Nginx 設定檔不存在: $CONFIG_FILE${NC}"
    fi
    
    echo ""
}

# 清理部署檔案
cleanup_deployment_files() {
    echo -e "${YELLOW}[5/5] 清理部署檔案...${NC}"
    
    # 詢問是否要保留備份
    echo -e "目前的部署檔案："
    ls -la | grep -E "publish|keys|docker-compose|Dockerfile|deploy.sh|manian-deploy.tar.gz"
    echo ""
    
    read -p "是否要刪除以上檔案？(y/N) " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        echo -e "刪除檔案..."
        
        # 刪除部署相關檔案
        rm -rf publish/ 2>/dev/null && echo "  ✅ 移除 publish/"
        rm -rf keys/ 2>/dev/null && echo "  ✅ 移除 keys/"
        rm -f docker-compose.yaml 2>/dev/null && echo "  ✅ 移除 docker-compose.yaml"
        rm -f Dockerfile 2>/dev/null && echo "  ✅ 移除 Dockerfile"
        rm -f deploy.sh 2>/dev/null && echo "  ✅ 移除 deploy.sh"
        rm -f manian-deploy.tar.gz 2>/dev/null && echo -e "  ✅ 移除 manian-deploy.tar.gz"
        
        echo -e "${GREEN}✅ 檔案清理完成${NC}"
    else
        echo -e "${YELLOW}⚠️  保留部署檔案${NC}"
    fi
    
    echo ""
}

# 顯示結果摘要
show_summary() {
    echo -e "${BLUE}========================================${NC}"
    echo -e "${GREEN}✅ Manian API 解除部署完成！${NC}"
    echo -e "${BLUE}========================================${NC}"
    echo -e "${YELLOW}已移除項目：${NC}"
    echo -e "  - Manian API 容器"
    echo -e "  - Manian API Docker 映像檔"
    echo -e "  - Nginx 設定檔 ($DOMAIN.conf)"
    echo ""
    echo -e "${YELLOW}備份位置：${NC} $BACKUP_DIR/"
    echo -e "${YELLOW}保留項目：${NC}"
    echo -e "  - SSL 憑證 (保留在 nginx-proxy/certbot/ 中)"
    echo -e "  - 備份檔案 (在 $BACKUP_DIR/ 中)"
    echo -e "${BLUE}========================================${NC}"
    echo -e "如果要完全移除 SSL 憑證，請手動執行："
    echo -e "  cd $NGINX_PROXY_DIR"
    echo -e "  rm -rf certbot/letsencrypt/live/$DOMAIN/"
    echo -e "${BLUE}========================================${NC}"
}

# ===== 主程式 =====
main() {
    START_TIME=$(date +%s)
    
    confirm_domain
    final_confirmation
    backup_current_deployment
    stop_and_remove_container
    remove_docker_image
    remove_nginx_config
    cleanup_deployment_files
    show_summary
    
    END_TIME=$(date +%s)
    DURATION=$((END_TIME - START_TIME))
    echo -e "${GREEN}✨ Manian API 解除部署花費: ${DURATION} 秒${NC}"
}

# 執行主程式
main