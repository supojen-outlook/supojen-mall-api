#!/bin/bash

# build.sh - 本地建置腳本（完整打包）
set -e

# 顏色輸出
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${YELLOW}開始建置 Manian API 專案...${NC}"

# 建立部署目錄
echo -e "建立部署目錄..."
DEPLOY_DIR="./deploy"
rm -rf $DEPLOY_DIR
mkdir -p $DEPLOY_DIR

# 還原專案
echo -e "還原 NuGet 套件..."
dotnet restore Manian.sln

# 建置專案
echo -e "建置專案..."
dotnet build Manian.sln -c Release --no-restore

# 發行專案
echo -e "發行專案到 publish 目錄..."
dotnet publish src/Manian.Presentation/Manian.Presentation.csproj \
    -c Release \
    -o $DEPLOY_DIR/publish \
    --no-build

# 複製 appsettings 檔案
echo -e "複製設定檔..."
cp src/Manian.Presentation/appsettings.json $DEPLOY_DIR/publish/ 2>/dev/null || :
cp src/Manian.Presentation/appsettings.Production.json $DEPLOY_DIR/publish/ 2>/dev/null || :

# 建立 keys 目錄並複製金鑰
# === 建立 keys 目錄並「生成」金鑰檔案 ===
echo -e "${YELLOW}正在利用 keygen-tool 自動生成全新金鑰...${NC}"
mkdir -p $DEPLOY_DIR/keys

# 確保工具可以執行
chmod +x ./keygen-tool

# 1. 生成 AES 金鑰並搬移
echo -e "正在生成 id_aes..."
./keygen-tool --aes
mv id_aes $DEPLOY_DIR/keys/

# 2. 生成 RSA v1 並搬移
echo -e "正在生成 id_rsa.v1..."
./keygen-tool --rsa --output id_rsa.v1
mv id_rsa.v1 $DEPLOY_DIR/keys/

# 3. 生成 RSA v2 並搬移
echo -e "正在生成 id_rsa.v2..."
./keygen-tool --rsa --output id_rsa.v2
mv id_rsa.v2 $DEPLOY_DIR/keys/

echo -e "${GREEN}✅ 所有金鑰已生成並存放到 $DEPLOY_DIR/keys/${NC}"

# 複製 Dockerfile 和 docker-compose
echo -e "複製 Dockerfile 和 docker-compose.yaml..."
cp Dockerfile $DEPLOY_DIR/ 2>/dev/null || echo -e "${RED}警告：找不到 Dockerfile${NC}"
cp docker-compose.yaml $DEPLOY_DIR/ 2>/dev/null || echo -e "${RED}警告：找不到 docker-compose.yaml${NC}"

# === 複製同級目錄的 deploy.sh ===
echo -e "複製部署腳本 deploy.sh..."
if [ -f "./deploy.sh" ]; then
    cp ./deploy.sh $DEPLOY_DIR/
    chmod +x $DEPLOY_DIR/deploy.sh
    echo -e "${GREEN}✅ 已複製 deploy.sh${NC}"
else
    echo -e "${RED}⚠️ 警告：找不到同級目錄的 deploy.sh${NC}"
fi

# 複製解除部署腳本 undeploy.sh
echo -e "複製解除部署腳本 undeploy.sh..."
if [ -f "./undeploy.sh" ]; then
    cp ./undeploy.sh $DEPLOY_DIR/
    chmod +x $DEPLOY_DIR/undeploy.sh
    echo -e "${GREEN}✅ 已複製 undeploy.sh${NC}"
else
    echo -e "${RED}⚠️ 警告：找不到同級目錄的 undeploy.sh${NC}"
fi

# 複製 Makefile
echo -e "複製 Makefile..."
if [ -f "./Makefile" ]; then
    cp ./Makefile $DEPLOY_DIR/
    echo -e "${GREEN}✅ 已複製 Makefile${NC}"
else
    echo -e "${RED}⚠️ 警告：找不到同級目錄的 Makefile${NC}"
fi

# 建立一個簡單的部署說明
echo -e "建立部署說明..."
cat > $DEPLOY_DIR/README.md << EOF
# Manian API 部署包

## 部署步驟
1. 上傳此壓縮檔到伺服器
2. 解壓縮：\`tar -xzf api-deploy.tar.gz\`
3. 進入目錄：\`cd deploy\`
4. 執行部署腳本：\`./deploy.sh\`

## 其他指令
- 查看 logs：\`docker-compose logs -f\`
- 停止服務：\`docker-compose down\`
- 重新啟動：\`docker-compose restart\`
EOF

# 打包整個部署目錄
echo -e "建立最終壓縮檔 manian-deploy.tar.gz..."
tar -czf manian-deploy.tar.gz -C $DEPLOY_DIR .

# 刪除部署目錄（只保留壓縮檔）
echo -e "清理暫存目錄..."
rm -rf $DEPLOY_DIR

echo -e "${GREEN}================================${NC}"
echo -e "${GREEN}部署包建置完成！${NC}"
echo -e "${GREEN}================================${NC}"
echo -e "產生的檔案："
echo -e "  - ${YELLOW}manian-deploy.tar.gz${NC}（要上傳到伺服器）"
echo -e ""
echo -e "上傳到伺服器："
echo -e "  ${YELLOW}scp manian-deploy.tar.gz user@your-server:/app/${NC}"
echo -e ""
echo -e "在伺服器上執行："
echo -e "  ${YELLOW}cd /app${NC}"
echo -e "  ${YELLOW}tar -xzf manian-deploy.tar.gz${NC}"
echo -e "  ${YELLOW}ls -la${NC}  # 會看到 publish/ keys/ Dockerfile docker-compose.yaml deploy.sh"
echo -e "  ${YELLOW}./deploy.sh${NC}"
echo -e "${GREEN}================================${NC}"