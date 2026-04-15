# ===== 變數設定 =====
DEPLOY_SCRIPT = ./deploy.sh
UNDEPLOY_SCRIPT = ./undeploy.sh
DOCKER_COMPOSE = docker compose

# 顏色定義
BLUE   = \033[0;34m
GREEN  = \033[0;32m
YELLOW = \033[1;33m
NC     = \033[0m

.PHONY: help deploy undeploy logs restart ps clean status

# ===== 預設指令：顯示幫助 =====
help:
	@echo -e "$(BLUE)========================================$(NC)"
	@echo -e "$(BLUE)    Manian API 管理工具 (Makefile)$(NC)"
	@echo -e "$(BLUE)========================================$(NC)"
	@echo -e "$(YELLOW)使用方式:$(NC) make [指令]"
	@echo ""
	@echo -e "$(GREEN)deploy$(NC)    - 執行 deploy.sh 進行完整部署"
	@echo -e "$(GREEN)undeploy$(NC)  - 執行 undeploy.sh 移除部署並備份"
	@echo -e "$(GREEN)logs$(NC)      - 查看 API 容器的即時日誌"
	@echo -e "$(GREEN)restart$(NC)   - 重啟 API 容器 (不更動 Nginx)"
	@echo -e "$(GREEN)ps$(NC)        - 檢查目前 API 容器狀態"
	@echo -e "$(GREEN)shell$(NC)     - 進入 API 容器內部的 bash"
	@echo -e "$(GREEN)status$(NC)    - 快速檢查 Nginx Proxy 狀態"
	@echo -e "$(BLUE)========================================$(NC)"

# ===== 主要流程 =====

# 部署
deploy:
	@chmod +x $(DEPLOY_SCRIPT)
	@$(DEPLOY_SCRIPT)

# 移除部署
undeploy:
	@chmod +x $(UNDEPLOY_SCRIPT)
	@$(UNDEPLOY_SCRIPT)

# ===== 輔助指令 =====

# 查看日誌
logs:
	@$(DOCKER_COMPOSE) logs -f

# 快速重啟
restart:
	@echo -e "$(YELLOW)正在重啟 Manian API...$(NC)"
	@$(DOCKER_COMPOSE) restart
	@echo -e "$(GREEN)✅ 重啟完成$(NC)"

# 查看容器狀態
ps:
	@$(DOCKER_COMPOSE) ps

# 進入容器
shell:
	@$(DOCKER_COMPOSE) exec api bash || $(DOCKER_COMPOSE) exec api sh

# 檢查 Nginx Proxy 狀態 (假設在上一層目錄)
status:
	@echo -e "$(YELLOW)檢查 Nginx Proxy 容器狀態...$(NC)"
	@docker ps --filter "name=nginx-proxy"
	@echo -e "\n$(YELLOW)檢查 Docker 網路狀態...$(NC)"
	@docker network ls | grep proxy || echo "⚠️  未發現 proxy 網路"

# 清理未使用的 Docker 映像檔
clean:
	@echo -e "$(YELLOW)清理懸浮映像檔與快取...$(NC)"
	@docker image prune -f
	@echo -e "$(GREEN)✅ 清理完成$(NC)"