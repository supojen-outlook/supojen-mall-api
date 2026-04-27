-- =============================================================================
-- pick_items：揀貨明細表
-- 用途：記錄訂單項目應從哪個具體儲位（Location/Bin）提取多少數量的指引
-- 設計考量：
--   1. 支援「拆單揀貨」：一個 order_item 可對應多個 pick_items (若庫存分散在不同儲位)
--   2. 連結實體庫存：直接關聯到 locations，讓倉管或客戶明確知道去哪裡取貨
--   3. 狀態追蹤：紀錄從「已分配(allocated)」到「已揀貨(picked)」的過程
-- =============================================================================

CREATE TABLE pick_items (
    -- 基礎資訊欄位 (Basic Information)
    -- -------------------------------------------------------------------------
    id              INT NOT NULL,           -- 揀貨項目唯一識別碼 (Unique pick item ID)
    order_id        INT NOT NULL,           -- 所屬訂單 ID (Redundant for performance)
    order_item_id   INT NOT NULL,           -- 關聯的訂單項目 ID (Reference to order_items)
    inventory_id    INT NOT NULL,           -- 關聯到庫存記錄 ID (Reference to inventories)
    location_id     INT NOT NULL,           -- 指向具體的儲位/貨架 ID (Pick from this location)
    product_image_url VARCHAR(512),         -- 商品圖片 URL (Optional, for display purpose)
    
    -- 數量資訊 (Quantity Information)
    -- -------------------------------------------------------------------------
    quantity_to_pick INT NOT NULL,          -- 應揀貨數量 (Planned quantity to pick)
    quantity_picked  INT NOT NULL DEFAULT 0, -- 實際已揀貨數量 (Actual quantity picked)
    
    -- 狀態與控制欄位 (Status & Control Fields)
    -- -------------------------------------------------------------------------
    -- 狀態流程：allocated(已分配/待揀貨) → picked(已完成揀貨) → cancelled(已取消)
    status          VARCHAR(30) NOT NULL DEFAULT 'allocated', -- 揀貨狀態 (Pick status)
    
    -- 時間戳欄位 (Timestamp Fields)
    -- -------------------------------------------------------------------------
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),       -- 記錄建立時間 (Allocated time)
    picked_at       TIMESTAMPTZ,                              -- 實際完成揀貨的時間 (Completion time)

    -- 約束條件 (Constraints)
    -- -------------------------------------------------------------------------
    
    -- 主鍵約束 (Primary Key)
    CONSTRAINT pk_pick_items PRIMARY KEY (id),
    
    -- 外鍵約束：關聯到主訂單
    CONSTRAINT fk_pick_items_order 
        FOREIGN KEY (order_id) 
        REFERENCES orders(id) 
        ON DELETE CASCADE,

    -- 外鍵約束：關聯到訂單項目，刪除項目時同步刪除揀貨清單
    CONSTRAINT fk_pick_items_order_item 
        FOREIGN KEY (order_item_id) 
        REFERENCES order_items(id) 
        ON DELETE CASCADE,

    -- 外鍵約束：關聯到庫存記錄，確保揀貨項目對應有效的庫存
    CONSTRAINT fk_pick_items_inventory 
        FOREIGN KEY (inventory_id) 
        REFERENCES inventories(id) 
        ON DELETE RESTRICT,        
        
    -- 外鍵約束：關聯到儲位，確保取貨地點存在
    CONSTRAINT fk_pick_items_location 
        FOREIGN KEY (location_id) 
        REFERENCES locations(id) 
        ON DELETE RESTRICT,
    
    -- 檢查約束：應揀數量必須大於 0
    CONSTRAINT ck_pick_items_quantity_to_pick CHECK 
        (quantity_to_pick > 0),
    
    -- 檢查約束：實揀數量不能為負，且通常不應超過應揀數量
    CONSTRAINT ck_pick_items_quantity_picked CHECK 
        (quantity_picked >= 0),
    
    -- 檢查約束：狀態必須為指定值
    CONSTRAINT ck_pick_items_status CHECK 
        (status IN ('allocated', 'picked', 'cancelled'))
);

-- =============================================================================
-- 索引建立 (Indexes)
-- =============================================================================

-- 索引 1：加速整筆訂單的揀貨查詢（最常用於後台列印出貨單）
CREATE INDEX IF NOT EXISTS idx_pick_items_order 
    ON pick_items (order_id);

-- 索引 2：加速單一項目的揀貨明細查詢
CREATE INDEX IF NOT EXISTS idx_pick_items_order_item 
    ON pick_items (order_item_id);

-- 索引 3：加速單一儲位的揀貨明細查詢
CREATE INDEX IF NOT EXISTS idx_pick_items_inventory 
    ON pick_items (inventory_id);


-- =============================================================================
-- 註解說明 (Comments)
-- =============================================================================

COMMENT ON TABLE pick_items IS '揀貨明細表，指引訂單商品應從哪個儲位選取';
COMMENT ON COLUMN pick_items.order_id IS '所屬訂單 ID，方便直接按訂單查詢所有揀貨路徑';
COMMENT ON COLUMN pick_items.order_item_id IS '關聯到 order_items，代表這筆揀貨是為了滿足哪個訂單項目';
COMMENT ON COLUMN pick_items.inventory_id IS '關聯到 inventories，代表這筆揀貨從哪個庫存記錄扣除';
COMMENT ON COLUMN pick_items.location_id IS '關聯到 locations，指示具體的取貨貨架/儲位';
COMMENT ON COLUMN pick_items.product_image_url IS '商品圖片網址，用於輔助揀貨辨識';
COMMENT ON COLUMN pick_items.quantity_to_pick IS '系統計算出的應取數量';
COMMENT ON COLUMN pick_items.quantity_picked IS '實際操作後確認取得的數量';
COMMENT ON COLUMN pick_items.status IS '揀貨狀態：allocated已分配/picked已完成/cancelled已取消';