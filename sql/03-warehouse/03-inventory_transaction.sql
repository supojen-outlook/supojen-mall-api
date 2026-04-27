-- =============================================================================
-- inventory_transactions：庫存交易紀錄表
-- 用途：記錄所有庫存異動的明細，用於追蹤、稽核、對帳
-- 設計考量：精簡版只記錄核心資訊，足夠追蹤問題但不複雜
-- 注意：交易數量為正數表示入庫，負數表示出庫
-- =============================================================================
CREATE TABLE inventory_transactions (
    -- 基礎資訊欄位 (Basic Information)
    -- -------------------------------------------------------------------------
    id              INT NOT NULL,                       -- 交易記錄唯一識別碼 (Unique transaction ID)
    sku_id          INT NOT NULL,                       -- SKU ID，關聯到 skus 表 (SKU ID)
    
    -- 交易類型與數量 (Transaction Type & Quantity)
    -- -------------------------------------------------------------------------
    transaction_type VARCHAR(20) NOT NULL,              -- 交易類型：in入庫/out出庫/adjust調整 (Transaction type)
    quantity        INT NOT NULL,                       -- 交易數量：正數入庫，負數出庫 (Transaction quantity, positive for in, negative for out)
    
    -- 參考資訊 (Reference Information)
    -- -------------------------------------------------------------------------
    reference_type  VARCHAR(20),                        -- 來源文檔類型：ORDER訂單/PURCHASE採購/ADJUST調整單 (Reference document type)
    reference_id    INT,                                -- 來源文檔 ID (Reference document ID)
    note            VARCHAR(255),                       -- 交易備註 (Transaction note)
    
    -- 時間戳欄位 (Timestamp Fields)
    -- -------------------------------------------------------------------------
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(), -- 交易發生時間 (Transaction time)

    -- 約束條件 (Constraints)
    -- -------------------------------------------------------------------------
    
    -- 主鍵約束：交易記錄唯一識別碼 (Primary key: unique transaction ID)
    CONSTRAINT pk_inventory_transactions PRIMARY KEY (id),
    
    -- 外鍵約束：有交易記錄的 SKU 不能被刪除 (Foreign key: cannot delete SKU with transactions)
    CONSTRAINT fk_inventory_transactions_sku 
        FOREIGN KEY (sku_id) 
        REFERENCES skus(id) 
        ON DELETE RESTRICT,
    
    -- 檢查約束：交易類型必須為指定值 (Check: transaction type must be in, out, or adjust)
    CONSTRAINT ck_inventory_transactions_type CHECK 
        (transaction_type IN ('IN', 'OUT', 'ADJSUT')),
    
    -- 檢查約束：交易數量不能為 0 (Check: quantity cannot be zero)
    CONSTRAINT ck_inventory_transactions_quantity CHECK 
        (quantity != 0),
    
    -- 檢查約束：reference_type 必須為指定值 (Check: reference type must be ORDER, PURCHASE, or ADJUST)
    CONSTRAINT ck_inventory_transactions_ref_type CHECK 
        (reference_type IS NULL OR reference_type IN ('ORDER', 'PURCHASE', 'ADJUST')),
    
    -- 檢查約束：reference_type 和 reference_id 必須同時為 NULL 或同時有值
    CONSTRAINT ck_inventory_transactions_reference CHECK 
        ((reference_type IS NULL AND reference_id IS NULL) OR
         (reference_type IS NOT NULL AND reference_id IS NOT NULL)) 
);

-- =============================================================================
-- 索引建立 (Indexes)
-- 用途：優化各類常見查詢的效能
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 索引 1：SKU 時間查詢索引（必要）
-- 名稱：idx_inventory_transactions_sku_time
-- 類型：B-tree
-- 欄位：sku_id, created_at DESC
-- 用途：加速查詢某個 SKU 的最近交易紀錄
-- 場景：商品詳情頁顯示庫存異動歷史、問題排查
-- 範例：SELECT * FROM inventory_transactions 
--       WHERE sku_id = 1001001 
--       ORDER BY created_at DESC 
--       LIMIT 20;
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_inventory_transactions_sku_time 
    ON inventory_transactions (sku_id, created_at DESC);

-- -----------------------------------------------------------------------------
-- 索引 2：來源文檔查詢索引（建議保留）
-- 名稱：idx_inventory_transactions_reference
-- 類型：B-tree
-- 欄位：reference_type, reference_id
-- 用途：加速查詢某個訂單或採購單相關的所有庫存交易
-- 場景：對帳時想知道某筆訂單扣了哪些庫存
-- 範例：SELECT * FROM inventory_transactions 
--       WHERE reference_type = 'order' AND reference_id = 3001;
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_inventory_transactions_reference 
    ON inventory_transactions (reference_type, reference_id);

-- -----------------------------------------------------------------------------
-- 索引 3：時間範圍查詢索引（選擇性建立）
-- 名稱：idx_inventory_transactions_date
-- 類型：B-tree
-- 欄位：created_at DESC
-- 用途：加速時間範圍的查詢
-- 場景：月底對帳、報表生成
-- 範例：SELECT * FROM inventory_transactions 
--       WHERE created_at >= '2024-03-01' 
--         AND created_at < '2024-04-01';
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_inventory_transactions_date 
    ON inventory_transactions (created_at DESC);


-- =============================================================================
-- 註解說明 (Comments)
-- =============================================================================

COMMENT ON TABLE inventory_transactions IS '庫存交易紀錄表，用於追蹤所有庫存異動';
COMMENT ON COLUMN inventory_transactions.id IS '交易記錄唯一識別碼，主鍵';
COMMENT ON COLUMN inventory_transactions.sku_id IS 'SKU ID，關聯到 skus 表';
COMMENT ON COLUMN inventory_transactions.transaction_type IS '交易類型：in入庫/out出庫/adjust調整';
COMMENT ON COLUMN inventory_transactions.quantity IS '交易數量：正數入庫，負數出庫';
COMMENT ON COLUMN inventory_transactions.reference_type IS '來源文檔類型：order訂單/purchase採購/adjust調整單';
COMMENT ON COLUMN inventory_transactions.reference_id IS '來源文檔 ID';
COMMENT ON COLUMN inventory_transactions.note IS '交易備註';
COMMENT ON COLUMN inventory_transactions.created_at IS '交易發生時間';