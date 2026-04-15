-- =============================================================================
-- order_items：訂單項目表
-- 用途：記錄訂單中的每個商品明細，包含商品名稱、價格、數量等
-- 設計考量：一個訂單可以有多個項目，每個項目對應一個 SKU
-- 注意：product_name 和 unit_price 為快照，避免商品資訊變更影響歷史訂單
-- =============================================================================
CREATE TABLE order_items (
    -- 基礎資訊欄位 (Basic Information)
    -- -------------------------------------------------------------------------
    id              INT NOT NULL, -- 訂單項目唯一識別碼 (Unique order item ID)
    order_id        INT NOT NULL, -- 所屬訂單 ID (Parent order ID)
    product_id      INT NOT NULL, -- 商品 ID，關聯到 products 表 (Product ID)
    sku_id          INT,          -- SKU ID，關聯到 skus 表 (SKU ID)
    
    -- 商品快照資訊 (Product Snapshot Information)
    -- -------------------------------------------------------------------------
    product_name    VARCHAR(255) NOT NULL,    -- 下單時的商品名稱 (Product name at order time)
    product_image_url VARCHAR(512),           -- 下單時的商品圖片 URL (Product image URL at order time)
    unit_price      DECIMAL(19,2) NOT NULL,   -- 下單時的單價 (Unit price at order time)
    quantity        INT NOT NULL,             -- 購買數量 (Quantity)
    
    -- 退貨數量追蹤 (Return Tracking)
    -- -------------------------------------------------------------------------
    returned_quantity INT NOT NULL DEFAULT 0, -- 已退貨數量 (Total returned quantity)
    
    -- 項目狀態 (Item Status)
    -- -------------------------------------------------------------------------
    -- 狀態流程：pending(待處理) → shipped(已出貨) 
    --                               ↘ refunded(已退款) / cancelled(已取消)
    status          VARCHAR(30) NOT NULL DEFAULT 'pending',  -- 項目狀態 (Item status)
    
    -- 時間戳欄位 (Timestamp Fields)
    -- -------------------------------------------------------------------------
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(), -- 項目建立時間 (Item creation time)

    -- 約束條件 (Constraints)
    -- -------------------------------------------------------------------------
    
    -- 主鍵約束：訂單項目唯一識別碼 (Primary key: unique order item ID)
    CONSTRAINT pk_order_items PRIMARY KEY (id),
    
    -- 外鍵約束：刪除訂單時自動刪除項目 (Foreign key: delete items when order is deleted)
    CONSTRAINT fk_order_items_order 
        FOREIGN KEY (order_id) 
        REFERENCES orders(id) 
        ON DELETE CASCADE,
    
    -- 外鍵約束：有訂單的商品不能被刪除 (Foreign key: cannot delete product with orders)
    CONSTRAINT fk_order_items_product 
        FOREIGN KEY (product_id) 
        REFERENCES products(id) 
        ON DELETE RESTRICT,
    
    -- 外鍵約束：有訂單的 SKU 不能被刪除 (Foreign key: cannot delete SKU with orders)
    CONSTRAINT fk_order_items_sku 
        FOREIGN KEY (sku_id) 
        REFERENCES skus(id) 
        ON DELETE RESTRICT,
    
    -- 檢查約束：數量必須大於 0 (Check: quantity must be > 0)
    CONSTRAINT ck_order_items_quantity CHECK 
        (quantity > 0),
    
    -- 檢查約束：退貨數量不能超過購買數量 (Check: returned quantity cannot exceed purchased quantity)
    CONSTRAINT ck_order_items_returned CHECK 
        (returned_quantity <= quantity),
    
    -- 檢查約束：單價不能為負 (Check: unit price cannot be negative)
    CONSTRAINT ck_order_items_unit_price CHECK 
        (unit_price >= 0),
    
    -- 檢查約束：狀態必須為指定值 (Check: status must be pending, shipped, refunded, or cancelled)
    CONSTRAINT ck_order_items_status CHECK 
        (status IN ('pending', 'shipped', 'refunded', 'cancelled'))
);

-- =============================================================================
-- 索引建立 (Indexes)
-- 用途：優化各類常見查詢的效能
-- 說明：PostgreSQL 會自動為 PRIMARY KEY 建立索引，以下為額外建立的索引
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 索引 1：訂單查詢索引（必要）
-- 名稱：idx_order_items_order
-- 類型：B-tree
-- 欄位：order_id
-- 用途：加速查詢某個訂單的所有項目
-- 場景：訂單詳情頁面顯示商品明細、訂單出貨處理
-- 範例：SELECT * FROM order_items WHERE order_id = 1001 ORDER BY id;
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_order_items_order 
    ON order_items (order_id);

-- -----------------------------------------------------------------------------
-- 索引 2：SKU 銷售查詢索引（必要）
-- 名稱：idx_order_items_sku
-- 類型：B-tree
-- 欄位：sku_id, created_at DESC
-- 用途：加速查詢某個 SKU 的銷售歷史，用於銷售分析
-- 場景：商品銷售報表、暢銷排行、庫存需求預測
-- 範例：SELECT sku_id, SUM(quantity) FROM order_items 
--       WHERE sku_id = 1001001 AND created_at >= '2024-01-01'
--       GROUP BY sku_id;
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_order_items_sku 
    ON order_items (sku_id, created_at DESC);

-- -----------------------------------------------------------------------------
-- 索引 3：項目狀態查詢索引（選擇性建立）
-- 名稱：idx_order_items_status
-- 類型：B-tree
-- 欄位：status
-- 用途：加速依狀態篩選訂單項目
-- 場景：後台查詢待出貨項目、退貨處理、問題訂單追蹤
-- 範例：SELECT * FROM order_items WHERE status = 'pending' ORDER BY created_at;
-- 說明：如果訂單項目數量很大且經常依狀態查詢，才需要此索引
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_order_items_status 
    ON order_items (status);

-- =============================================================================
-- 範例資料 (Sample Data)
-- =============================================================================

INSERT INTO order_items (id, order_id, product_id, sku_id, product_name, unit_price, quantity, status) VALUES
    (1, 1, 10001, 1001001, '青花瓷茶具組 - 藍色', 2990, 1, 'pending'),
    (2, 2, 10002, NULL, '柴燒陶罐', 5800, 1, 'pending'),
    (3, 3, 30001, 3001001, 'Audi RS 電動遙控車 - 銀色', 3200, 1, 'shipped'),
    (4, 3, 20001, 2001001, '手工玻璃花器 - 透明', 3800, 2, 'shipped');

-- =============================================================================
-- 註解說明 (Comments)
-- =============================================================================

COMMENT ON TABLE order_items IS '訂單項目表，記錄訂單中的每個商品明細';
COMMENT ON COLUMN order_items.id IS '訂單項目唯一識別碼，主鍵';
COMMENT ON COLUMN order_items.order_id IS '所屬訂單 ID，關聯到 orders 表';
COMMENT ON COLUMN order_items.product_id IS '商品 ID，關聯到 products 表';
COMMENT ON COLUMN order_items.sku_id IS 'SKU ID，關聯到 skus 表';
COMMENT ON COLUMN order_items.product_name IS '下單時的商品名稱（快照）';
COMMENT ON COLUMN order_items.product_image_url IS '下單時的商品圖片網址（快照）'; 
COMMENT ON COLUMN order_items.unit_price IS '下單時的單價（快照）';
COMMENT ON COLUMN order_items.quantity IS '購買數量';
COMMENT ON COLUMN order_items.returned_quantity IS '已退貨數量';
COMMENT ON COLUMN order_items.status IS '項目狀態：pending待處理/shipped已出貨/refunded已退款/cancelled已取消';
COMMENT ON COLUMN order_items.created_at IS '項目建立時間';