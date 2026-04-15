-- =============================================================================
-- inventories：庫存主表
-- 用途：記錄每個 SKU 在各個儲位的即時庫存數量
-- 設計考量：一個 SKU 可以放在多個儲位，透過 (sku_id + location_id) 唯一識別
-- 注意：可銷售庫存 = quantity_on_hand - quantity_reserved，由觸發器自動維護
-- =============================================================================
CREATE TABLE inventories (
    -- 基礎資訊欄位 (Basic Information)
    -- -------------------------------------------------------------------------
    id              INT NOT NULL,                          -- 庫存記錄唯一識別碼 (Unique inventory record ID)
    sku_id          INT NOT NULL,                          -- SKU ID，關聯到 skus 表 (SKU ID)
    location_id     INT NOT NULL,                          -- 儲位 ID，關聯到 locations 表 (Storage bin location ID)
    
    -- 數量資訊 (Quantity Information)
    -- -------------------------------------------------------------------------
    quantity_on_hand   DECIMAL(19,2) NOT NULL DEFAULT 0,   -- 實際庫存數量 (Actual quantity on hand)
    quantity_reserved  DECIMAL(19,2) NOT NULL DEFAULT 0,   -- 預占庫存量（已訂未出）(Reserved quantity for orders)
    quantity_available DECIMAL(19,2) NOT NULL DEFAULT 0,   -- 可銷售庫存量 (Available quantity for sale)
    
    -- 狀態與控制欄位 (Status & Control Fields)
    -- -------------------------------------------------------------------------
    status          VARCHAR(20) NOT NULL DEFAULT 'active', -- 狀態：active啟用/inactive停用/quarantined隔離 (Inventory status)
    is_available    BOOLEAN NOT NULL DEFAULT TRUE,         -- 是否可用於銷售 (Whether available for sale)
    
    -- 時間戳欄位 (Timestamp Fields)
    -- -------------------------------------------------------------------------
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),    -- 庫存記錄建立時間 (Record creation time)

    -- 約束條件 (Constraints)
    -- -------------------------------------------------------------------------

    -- 主鍵約束：庫存記錄唯一識別碼 (Primary key: unique inventory record)
    CONSTRAINT pk_inventories PRIMARY KEY (id),               
    
    -- 唯一約束：同一個 SKU 在同一個儲位只能有一筆記錄 (Unique: one SKU per location)
    CONSTRAINT uk_inventories_sku_location 
        UNIQUE (sku_id, location_id),                          
    
    -- 外鍵約束：刪除 SKU 時自動刪除庫存記錄 (Foreign key: delete inventory when SKU is deleted)
    CONSTRAINT fk_inventories_sku 
        FOREIGN KEY (sku_id) 
        REFERENCES skus(id) 
        ON DELETE CASCADE,                                      
    
    -- 外鍵約束：有庫存的儲位不能被刪除 (Foreign key: cannot delete location with inventory)
    CONSTRAINT fk_inventories_location 
        FOREIGN KEY (location_id) 
        REFERENCES locations(id) 
        ON DELETE RESTRICT,                                    
    
    -- 檢查約束：實際庫存不能為負 (Check: on-hand quantity cannot be negative)
    CONSTRAINT ck_inventories_quantity_on_hand CHECK 
        (quantity_on_hand >= 0),                                
    
    -- 檢查約束：預占庫存不能為負 (Check: reserved quantity cannot be negative)
    CONSTRAINT ck_inventories_quantity_reserved CHECK 
        (quantity_reserved >= 0),                               
    
    -- 檢查約束：可銷售庫存不能為負 (Check: available quantity cannot be negative)
    CONSTRAINT ck_inventories_quantity_available CHECK 
        (quantity_available >= 0),                              
    
    -- 檢查約束：狀態必須為指定值 (Check: status must be active, inactive, or quarantined)
    CONSTRAINT ck_inventories_status CHECK 
        (status IN ('active', 'inactive', 'quarantined'))      
);

-- =============================================================================
-- 索引建立 (Indexes)
-- 用途：優化各類常見查詢的效能
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 索引 1：SKU 查詢索引（必要）
-- 名稱：idx_inventories_sku
-- 類型：B-tree
-- 欄位：sku_id
-- 用途：加速查詢某個 SKU 在所有儲位的庫存總量
-- 範例：SELECT sku_id, SUM(quantity_on_hand) as total_stock
--       FROM inventories 
--       WHERE sku_id = 1001001
--       GROUP BY sku_id;
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_inventories_sku 
    ON inventories (sku_id);

-- -----------------------------------------------------------------------------
-- 索引 2：儲位查詢索引（必要）
-- 名稱：idx_inventories_location
-- 類型：B-tree
-- 欄位：location_id
-- 用途：加速查詢某個儲位上有哪些 SKU
-- 範例：SELECT * FROM inventories WHERE location_id = 101;
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_inventories_location 
    ON inventories (location_id);

-- -----------------------------------------------------------------------------
-- 索引 3：覆蓋索引（選擇性建立）
-- 名稱：idx_inventories_sku_covering
-- 類型：B-tree 包含索引
-- 欄位：sku_id
-- 包含：quantity_on_hand, quantity_available
-- 用途：加速 SKU 庫存總量的統計查詢，可直接從索引取得資料，不用回表
-- 說明：如果庫存查詢非常頻繁（如每秒數十次），建議建立此索引
-- -----------------------------------------------------------------------------
-- CREATE INDEX IF NOT EXISTS idx_inventories_sku_covering 
--     ON inventories (sku_id) 
--     INCLUDE (quantity_on_hand, quantity_available)
--     WHERE is_available = TRUE;

-- =============================================================================
-- 觸發器 (Triggers)
-- 用途：自動維護 quantity_available 計算
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 觸發器函數：更新庫存可用數量
-- 名稱：fn_update_inventory_available
-- 用途：當 quantity_on_hand 或 quantity_reserved 變動時，自動重新計算 quantity_available
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_update_inventory_available()
RETURNS TRIGGER AS $$
BEGIN
    NEW.quantity_available = NEW.quantity_on_hand - NEW.quantity_reserved;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- -----------------------------------------------------------------------------
-- 觸發器：在新增或更新庫存時自動維護可用數量
-- -----------------------------------------------------------------------------
CREATE TRIGGER trg_inventories_before_insert_update
    BEFORE INSERT OR UPDATE OF quantity_on_hand, quantity_reserved ON inventories
    FOR EACH ROW
    EXECUTE FUNCTION fn_update_inventory_available();

-- =============================================================================
-- 範例資料 (Sample Data)
-- 用途：展示如何記錄庫存
-- =============================================================================

/*
-- 假設 locations 表有儲位：
-- A01-01 (id=101), A01-02 (id=102), B01-01 (id=201)

-- 假設 skus 表有 SKU：
-- 藍色茶具組 (id=1001001), 白色茶具組 (id=1001002)
*/

INSERT INTO inventories (id, sku_id, location_id, quantity_on_hand) VALUES
    (1, 1001001, 101, 50),   -- 藍色茶具組 50 個在 A01-01
    (2, 1001001, 102, 30),   -- 藍色茶具組 30 個在 A01-02
    (3, 1001002, 101, 20);   -- 白色茶具組 20 個在 A01-01

-- =============================================================================
-- 驗證範例 (Validation Queries)
-- =============================================================================

/*
-- 1. 查詢某個 SKU 的總庫存
SELECT 
    sku_id,
    SUM(quantity_on_hand) AS total_stock,
    SUM(quantity_reserved) AS total_reserved,
    SUM(quantity_available) AS total_available
FROM inventories
WHERE sku_id = 1001001
GROUP BY sku_id;

-- 2. 查詢某個儲位的庫存明細
SELECT 
    l.bin_code,
    s.sku_code,
    s.name,
    i.quantity_on_hand,
    i.quantity_available
FROM inventories i
JOIN skus s ON i.sku_id = s.id
JOIN locations l ON i.location_id = l.id
WHERE i.location_id = 101;

-- 3. 查詢庫存不足的 SKU (可銷售庫存 < 10)
SELECT 
    sku_id,
    SUM(quantity_available) AS total_available
FROM inventories
GROUP BY sku_id
HAVING SUM(quantity_available) < 10;
*/

-- =============================================================================
-- 註解說明 (Comments)
-- =============================================================================

COMMENT ON TABLE inventories IS '庫存主表，記錄每個 SKU 在各儲位的即時庫存';
COMMENT ON COLUMN inventories.id IS '庫存記錄唯一識別碼，主鍵';
COMMENT ON COLUMN inventories.sku_id IS 'SKU ID，關聯到 skus 表';
COMMENT ON COLUMN inventories.location_id IS '儲位 ID，關聯到 locations 表';
COMMENT ON COLUMN inventories.quantity_on_hand IS '實際庫存數量';
COMMENT ON COLUMN inventories.quantity_reserved IS '預占庫存量（已訂未出）';
COMMENT ON COLUMN inventories.quantity_available IS '可銷售庫存量，由觸發器自動計算';
COMMENT ON COLUMN inventories.status IS '庫存狀態：active正常/inactive停用/quarantined隔離';
COMMENT ON COLUMN inventories.is_available IS '是否可用於銷售';
COMMENT ON COLUMN inventories.created_at IS '記錄建立時間';