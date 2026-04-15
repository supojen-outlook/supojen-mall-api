-- =============================================================================
-- skus：商品SKU表
-- 用途：儲存商品的 SKU (Stock Keeping Unit) 層級資訊，包含具體規格、價格、庫存
-- 設計考量：與 products 表為多對一關係，一個商品可以有多個 SKU 代表不同規格組合
-- 注意：可銷售庫存 = stock_quantity - reserved_stock；specs 需對應 attribute_values 的組合
-- =============================================================================
CREATE TABLE skus (
    -- 基礎資訊欄位 (Basic Information)
    -- -------------------------------------------------------------------------
    id             INT NOT NULL,                            -- SKU 唯一識別碼 (Unique SKU ID)
    sku_code       VARCHAR(50) NOT NULL,                    -- SKU 編碼，用於庫存管理 (Unique SKU code for inventory management)
    name           VARCHAR(200) NOT NULL,                   -- SKU 顯示名稱，如：iPhone 14 黑色 128G (SKU display name)
    product_id     INT NOT NULL,                            -- 所屬商品 ID (Parent product ID)
    
    -- 價格與庫存資訊 (Price & Stock Information)
    -- -------------------------------------------------------------------------
    price          DECIMAL(10,2) NOT NULL,                  -- SKU 銷售價格，覆蓋商品基礎價格 (Sales price, overrides product base price)
    stock_quantity INT NOT NULL DEFAULT 0,                  -- 實際庫存數量 (Actual stock quantity)
    reserved_stock INT NOT NULL DEFAULT 0,                  -- 預占庫存（已下單未付款）(Reserved stock for pending orders)
    
    -- 規格與多媒體資訊 (Specs & Media Information)
    -- -------------------------------------------------------------------------
    specs          JSONB NOT NULL DEFAULT '{}'::JSONB,      -- SKU 規格組合，如：{"顏色":"黑色","尺寸":"XL"} (SKU specifications, combination of attribute values)
    image_url      VARCHAR(500),                            -- SKU 專屬圖片 (SKU specific image)
    
    -- 單位資訊 (Unit Information)
    -- -------------------------------------------------------------------------
    unit_of_measure_id INT,                                -- 計量單位 ID (Unit of measure ID)
    
    -- 狀態與控制欄位 (Status & Control Fields)
    -- -------------------------------------------------------------------------
    is_default     BOOLEAN NOT NULL DEFAULT FALSE,          -- 是否為預設 SKU（用於商品頁面預先顯示）(Default SKU flag for product page)
    status         VARCHAR(20) NOT NULL DEFAULT 'active',   -- SKU 狀態：active啟用/inactive停用 (SKU status)
    
    -- 時間戳欄位 (Timestamp Fields)
    -- -------------------------------------------------------------------------
    created_at     TIMESTAMPTZ NOT NULL DEFAULT NOW(),      -- SKU 建立時間 (SKU creation time)

    -- 約束條件 (Constraints)
    -- -------------------------------------------------------------------------
    
    -- 主鍵約束：SKU 唯一識別碼 (Primary key: unique SKU identifier)
    CONSTRAINT pk_skus PRIMARY KEY (id),

    -- 唯一約束：SKU 編碼不可重複 (Unique constraint: SKU code must be unique)
    CONSTRAINT uk_skus_sku_code UNIQUE (sku_code),
    
     -- 外鍵約束：關聯到商品表，刪除商品時一併刪除 SKU (Foreign key: references products, delete SKUs when product is deleted)
    CONSTRAINT fk_skus_product 
        FOREIGN KEY (product_id) 
        REFERENCES products(id) 
        ON DELETE CASCADE,                                  
    
    -- 外鍵約束：關聯到計量單位表 (Foreign key: references unit of measures)
    CONSTRAINT fk_skus_unit_of_measure 
        FOREIGN KEY (unit_of_measure_id) 
        REFERENCES unit_of_measures(id),                     
    
    -- 檢查約束：價格必須大於等於 0 (Check: price must be >= 0)
    CONSTRAINT ck_skus_price CHECK 
        (price >= 0),                                        
    
    -- 檢查約束：庫存數量必須大於等於 0 (Check: stock quantity must be >= 0)
    CONSTRAINT ck_skus_stock CHECK 
        (stock_quantity >= 0),                               
    
    -- 檢查約束：預占庫存必須大於等於 0 (Check: reserved stock must be >= 0)
    CONSTRAINT ck_skus_reserved CHECK 
        (reserved_stock >= 0),                               
    
    -- 檢查約束：預占庫存不能超過實際庫存 (Check: reserved stock cannot exceed actual stock)
    CONSTRAINT ck_skus_available CHECK 
        (reserved_stock <= stock_quantity),                  
    
    -- 檢查約束：狀態只能為 active 或 inactive (Check: status must be active or inactive)
    CONSTRAINT ck_skus_status CHECK 
        (status IN ('active', 'inactive'))                   
);

-- =============================================================================
-- 索引建立 (Indexes)
-- 用途：優化各類常見查詢的效能
-- 說明：PostgreSQL 會自動為 PRIMARY KEY 和 UNIQUE 約束建立索引，
--       以下為額外建立的索引
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 索引 1：商品 ID 查詢索引（必要）
-- 名稱：idx_skus_product_id
-- 類型：B-tree
-- 欄位：product_id
-- 用途：加速查詢某個商品下的所有 SKU
-- 場景：商品詳情頁面顯示所有規格選項時
-- 範例：SELECT * FROM skus WHERE product_id = 10001 AND status = 'active' ORDER BY is_default DESC;
-- 說明：這是 SKU 表最頻繁的查詢，一定要建立索引
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_skus_product_id 
    ON skus (product_id);

-- -----------------------------------------------------------------------------
-- 索引 2：狀態查詢索引（選擇性建立）
-- 名稱：idx_skus_status
-- 類型：B-tree
-- 欄位：status
-- 用途：加速按狀態過濾 SKU 的查詢
-- 場景：後台管理只顯示啟用或停用的 SKU
-- 範例：SELECT * FROM skus WHERE status = 'active';
-- 說明：如果 SKU 數量很多且經常這樣查詢，才需要建立此索引
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_skus_status 
    ON skus (status);

-- -----------------------------------------------------------------------------
-- 索引 3：複合查詢索引（選擇性建立）
-- 名稱：idx_skus_product_status_default
-- 類型：B-tree
-- 欄位：(product_id, status, is_default, sort_order?) 
-- 用途：加速商品頁面最常用的 SKU 顯示查詢
-- 場景：商品詳情頁需要顯示所有啟用 SKU，並將預設 SKU 排在前面
-- 範例：SELECT * FROM skus 
--       WHERE product_id = 10001 AND status = 'active' 
--       ORDER BY is_default DESC, id;
-- 說明：如果這種查詢非常頻繁，可以用這個複合索引取代單一 product_id 索引
-- -----------------------------------------------------------------------------
-- CREATE INDEX IF NOT EXISTS idx_skus_product_status_default 
--     ON skus (product_id, status, is_default DESC);

-- -----------------------------------------------------------------------------
-- 索引 4：規格 GIN 索引（選擇性建立）
-- 名稱：idx_skus_specs_gin
-- 類型：GIN
-- 欄位：specs (JSONB)
-- 用途：加速依規格組合搜尋 SKU
-- 場景：後台需要找出所有「黑色 XL」的 SKU
-- 範例：SELECT * FROM skus WHERE specs @> '{"顏色":"黑色","尺寸":"XL"}';
-- 說明：如果經常需要依規格組合搜尋 SKU，才建立此索引
-- -----------------------------------------------------------------------------
-- CREATE INDEX IF NOT EXISTS idx_skus_specs_gin 
--     ON skus USING GIN (specs);


-- =============================================================================
-- 註解說明 (Comments)
-- =============================================================================

COMMENT ON TABLE skus IS '商品 SKU 表，儲存具體規格和庫存資訊';
COMMENT ON COLUMN skus.id IS 'SKU 唯一識別碼，主鍵';
COMMENT ON COLUMN skus.sku_code IS 'SKU 編碼，唯一識別庫存單位';
COMMENT ON COLUMN skus.name IS 'SKU 顯示名稱，通常包含規格資訊';
COMMENT ON COLUMN skus.product_id IS '所屬商品 ID，關聯到 products 表';
COMMENT ON COLUMN skus.price IS 'SKU 銷售價格，可與商品基礎價格不同';
COMMENT ON COLUMN skus.stock_quantity IS '實際庫存數量';
COMMENT ON COLUMN skus.reserved_stock IS '預占庫存（已訂未付）';
COMMENT ON COLUMN skus.specs IS 'SKU 規格組合，JSON 格式';
COMMENT ON COLUMN skus.image_url IS 'SKU 專屬圖片';
COMMENT ON COLUMN skus.unit_of_measure_id IS '計量單位 ID';
COMMENT ON COLUMN skus.is_default IS '是否為預設 SKU';
COMMENT ON COLUMN skus.status IS 'SKU 狀態：active啟用，inactive停用';
COMMENT ON COLUMN skus.created_at IS 'SKU 建立時間';