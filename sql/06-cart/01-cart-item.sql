-- =============================================================================
-- cart_items：購物車項目表
-- 用途：記錄使用者的購物車項目，包含商品、SKU、數量等資訊
-- 設計考量：
--   - 直接透過 user_id 關聯使用者，不需額外的 carts 表
--   - 支援購物車(shopping)和願望清單(wishlist)兩種類型
--   - 快照商品資訊以確保歷史一致性
--   - 支援多使用者裝置（透過 session_id）
-- =============================================================================
CREATE TABLE cart_items (
    -- 基礎資訊欄位 (Basic Information)
    -- -------------------------------------------------------------------------
    id              INT NOT NULL,               -- 購物車項目唯一識別碼
    user_id         INT NOT NULL,               -- 使用者 ID
    
    -- 類型欄位 (Type Field)
    -- -------------------------------------------------------------------------
    cart_type       VARCHAR(20) NOT NULL DEFAULT 'shopping', -- 購物車類型：shopping/wishlist
    
    -- 商品資訊 (Product Information)
    -- -------------------------------------------------------------------------
    product_id      INT NOT NULL,               -- 商品 ID
    sku_id          INT NOT NULL,               -- SKU ID
    
    -- 商品快照資訊 (Product Snapshot)
    -- -------------------------------------------------------------------------
    product_name    VARCHAR(255) NOT NULL,         -- 商品名稱（快照）
    sku_attributes  JSONB NOT NULL,                -- SKU 屬性（快照，如：{"顏色":"黑色","尺寸":"XL"}）
    unit_price      DECIMAL(12,2) NOT NULL,        -- 單價（快照，來自 SKU.price）
    currency        VARCHAR(3) NOT NULL DEFAULT 'NTD', -- 貨幣代碼
    quantity        INTEGER NOT NULL DEFAULT 1,    -- 數量
    product_image   VARCHAR(500) NOT NULL,        -- 商品圖片（快照，優先使用 SKU 圖片）
    
    -- 時間戳欄位 (Timestamp Fields)
    -- -------------------------------------------------------------------------
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(), -- 記錄建立時間
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(), -- 記錄更新時間

    -- 約束條件 (Constraints)
    -- -------------------------------------------------------------------------

    -- 主鍵約束：購物車項目唯一識別碼
    CONSTRAINT pk_cart_items PRIMARY KEY (id),
    
    -- 外鍵約束：刪除使用者時自動刪除購物車項目
    CONSTRAINT fk_cart_items_user 
        FOREIGN KEY (user_id) 
        REFERENCES users(id) 
        ON DELETE CASCADE,
    
    -- 外鍵約束：刪除商品時保留購物車項目（僅標記商品不存在）
    CONSTRAINT fk_cart_items_product 
        FOREIGN KEY (product_id) 
        REFERENCES products(id) 
        ON DELETE SET NULL,
    
    -- 外鍵約束：刪除 SKU 時保留購物車項目（僅標記 SKU 不存在）
    CONSTRAINT fk_cart_items_sku 
        FOREIGN KEY (sku_id) 
        REFERENCES skus(id) 
        ON DELETE SET NULL,
    
    -- 檢查約束：購物車類型必須為指定值
    CONSTRAINT ck_cart_items_type CHECK 
        (cart_type IN ('shopping', 'wishlist')),
    
    -- 檢查約束：數量必須大於 0
    CONSTRAINT ck_cart_items_quantity CHECK 
        (quantity > 0),
    
    -- 檢查約束：單價必須大於 0
    CONSTRAINT ck_cart_items_price CHECK 
        (unit_price > 0),
    
    -- 唯一約束：同一使用者的同一 SKU 在同一類型的購物車中只能有一筆記錄
    CONSTRAINT uq_cart_items_user_sku_type UNIQUE (user_id, sku_id, cart_type)
);

-- =============================================================================
-- 索引建立 (Indexes)
-- 用途：優化各類常見查詢的效能
-- =============================================================================

-- 複合索引：優化查詢使用者的購物車項目
-- 查詢範例：WHERE user_id = ? AND cart_type = 'shopping'
CREATE INDEX idx_cart_items_user_type 
    ON cart_items (user_id, cart_type);

-- 觸發器 (Triggers)
-- -------------------------------------------------------------------------

-- 觸發器：自動更新 updated_at 欄位
CREATE OR REPLACE FUNCTION update_cart_items_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER tr_cart_items_updated_at
    BEFORE UPDATE ON cart_items
    FOR EACH ROW
    EXECUTE FUNCTION update_cart_items_updated_at();
