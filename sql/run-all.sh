#!/bin/bash

# PostgreSQL SQL 批次執行腳本
# 使用方式: ./run-all.sh <host> <dbname> <user>
# 範例: ./run-all.sh localhost mall_db postgres

set -e

if [ $# -ne 3 ]; then
    echo "用法: $0 <host> <dbname> <user>"
    echo "範例: $0 localhost mall_db postgres"
    exit 1
fi

HOST=$1
DBNAME=$2
USER=$3

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# 建立臨時合併檔案
MERGED_FILE=$(mktemp)
trap "rm -f $MERGED_FILE" EXIT

echo ""
echo "準備合併 SQL 檔案..."
echo ""

# 取得所有數字前綴的子目錄並排序
SUBDIRS=$(find "$SCRIPT_DIR" -maxdepth 1 -type d -name '[0-9]*-*' | sort)

if [ -z "$SUBDIRS" ]; then
    echo "找不到任何數字前綴的子目錄"
    exit 1
fi

FILE_COUNT=0

# 遍歷每個子目錄
for SUBDIR in $SUBDIRS; do
    DIR_NAME=$(basename "$SUBDIR")
    
    # 取得該目錄下所有數字前綴的 sql 檔案並排序
    SQL_FILES=$(find "$SUBDIR" -maxdepth 1 -type f -name '[0-9]*-*.sql' | sort)
    
    if [ -z "$SQL_FILES" ]; then
        continue
    fi
    
    echo "📁 $DIR_NAME"
    
    # 將該目錄的 SQL 檔案加入合併檔案
    for SQL_FILE in $SQL_FILES; do
        FILE_NAME=$(basename "$SQL_FILE")
        echo "  📄 $FILE_NAME"
        
        # 加入檔案分隔註解和 echo 命令（讓 psql 執行時顯示檔案名稱）
        echo "" >> "$MERGED_FILE"
        echo "\echo '========================================'" >> "$MERGED_FILE"
        echo "\echo '正在執行: $DIR_NAME/$FILE_NAME'" >> "$MERGED_FILE"
        echo "\echo '========================================'" >> "$MERGED_FILE"
        echo "" >> "$MERGED_FILE"
        
        cat "$SQL_FILE" >> "$MERGED_FILE"
        ((FILE_COUNT++)) || true
    done
done

echo ""
echo "════════════════════════════════════════════════════════════"
echo "  即將執行 $FILE_COUNT 個 SQL 檔案"
echo "════════════════════════════════════════════════════════════"
echo ""

# 執行合併後的 SQL 檔案（只需輸入一次密碼）
echo ""
echo "⚠️  注意：如果出現錯誤，請查看上面的 '正在執行: xxx' 提示"
echo "   即可知道是哪個檔案出問題"
echo ""

if ! psql "host=$HOST dbname=$DBNAME user=$USER" -f "$MERGED_FILE" 2>&1; then
    echo ""
    echo "❌ SQL 執行失敗"
    echo ""
    echo "════════════════════════════════════════════════════════════"
    echo "  除錯資訊："
    echo "  合併檔案位置: $MERGED_FILE"
    echo "  （此檔案在腳本結束後會自動刪除，若要保留請按 Ctrl+C 中斷）"
    echo "════════════════════════════════════════════════════════════"
    exit 1
fi

echo ""
echo "════════════════════════════════════════════════════════════"
echo "  ✅ 所有 SQL 腳本執行完成 ($FILE_COUNT 個檔案)"
echo "════════════════════════════════════════════════════════════"
