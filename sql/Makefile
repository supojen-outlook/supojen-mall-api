.PHONY: run-sql help

# 預設參數（可自行修改或透過命令列覆蓋）
HOST ?= localhost
DBNAME ?= mall_db
USER ?= postgres

help:
	@echo "════════════════════════════════════════════════════════════"
	@echo "  SQL 腳本批次執行 Makefile"
	@echo "════════════════════════════════════════════════════════════"
	@echo "可用指令:"
	@echo "  make run-sql              - 執行所有 SQL 腳本"
	@echo ""
	@echo "參數設定 (可選，有預設值):"
	@echo "  HOST=<host>               - 資料庫主機 (預設: localhost)"
	@echo "  DBNAME=<dbname>           - 資料庫名稱 (預設: mall_db)"
	@echo "  USER=<user>               - 資料庫使用者 (預設: postgres)"
	@echo ""
	@echo "範例:"
	@echo "  make run-sql HOST=localhost DBNAME=mall_db USER=postgres"
	@echo "  make run-sql HOST=mydb.example.com DBNAME=production USER=admin"
	@echo ""
	@echo "或使用預設值:"
	@echo "  make run-sql"
	@echo "════════════════════════════════════════════════════════════"

run-sql:
	@echo "════════════════════════════════════════════════════════════"
	@echo "  開始執行 SQL 腳本"
	@echo "════════════════════════════════════════════════════════════"
	@echo "  Host:     $(HOST)"
	@echo "  Database: $(DBNAME)"
	@echo "  User:     $(USER)"
	@echo "════════════════════════════════════════════════════════════"
	@./run-all.sh $(HOST) $(DBNAME) $(USER)
