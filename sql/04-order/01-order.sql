-- =============================================================================
-- orders：主訂單表
-- 用途：記錄訂單交易層級的核心資訊，包含金額、狀態、時間戳等
-- 設計考量：一個訂單對應多個訂單項目 (order_items)，總金額由各項目金額加總而得
-- 注意：snapshot 欄位用於儲存訂單建立時的快照，避免商品資訊變更影響歷史訂單
-- =============================================================================
CREATE TABLE orders (
    -- 基礎資訊欄位 (Basic Information)
    -- -------------------------------------------------------------------------
    id              INT NOT NULL,         -- 訂單唯一識別碼 (Unique order ID)
    order_number    VARCHAR(50) NOT NULL, -- 訂單編號，用於查詢和物流 (Order number)
    user_id         INT NOT NULL,         -- 顧客 ID，關聯到 users 表 (Customer ID)
    
    -- 訂單狀態 (Order Status)
    -- -------------------------------------------------------------------------
    -- 狀態流程：created(已建立) → paid(已付款) → shipped(已出貨) → completed(已完成)
    --                                         ↘ closed(已關閉/取消)
    status          VARCHAR(20) NOT NULL DEFAULT 'created', -- 訂單狀態 (Order status)
    
    -- 金額資訊 (Amount Information)
    -- -------------------------------------------------------------------------
    total_amount    DECIMAL(19,2) NOT NULL,           -- 訂單總金額 (Total order amount)
    discount_amount DECIMAL(19,2) NOT NULL DEFAULT 0, -- 折扣金額 (Discount amount)
    tax_amount      DECIMAL(19,2) NOT NULL DEFAULT 0, -- 稅金金額 (Tax amount)
    shipping_amount DECIMAL(19,2) NOT NULL DEFAULT 0, -- 運費金額 (Shipping amount)
    
    -- 時間戳欄位 (Timestamp Fields)
    -- -------------------------------------------------------------------------
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(), -- 訂單建立時間 (Order creation time)
    paid_at         TIMESTAMPTZ,                        -- 付款時間 (Payment time)
    shipped_at      TIMESTAMPTZ,                        -- 出貨時間 (Shipping time)
    completed_at    TIMESTAMPTZ,                        -- 完成時間 (Completion time)
    
    -- 快照資訊 (Snapshot Information)
    -- -------------------------------------------------------------------------
    snapshot        JSONB,                              -- 訂單快照，記錄當時的商品資訊、價格等 (Order snapshot)

    -- 約束條件 (Constraints)
    -- -------------------------------------------------------------------------

    -- 主鍵約束：訂單唯一識別碼 (Primary key: unique order ID)
    CONSTRAINT pk_orders PRIMARY KEY (id),                    

    -- 唯一約束：訂單編號不可重複 (Unique: order number must be unique)
    CONSTRAINT uk_orders_order_number UNIQUE (order_number),  
    
    -- 外鍵約束：有訂單的用戶不能被刪除 (Foreign key: cannot delete user with orders)
    CONSTRAINT fk_orders_user 
        FOREIGN KEY (user_id) 
        REFERENCES users(id) 
        ON DELETE RESTRICT,                                    
    
    -- 檢查約束：狀態必須為指定值 (Check: status must be created, paid, shipped, completed, or closed)
    CONSTRAINT ck_orders_status CHECK 
        (status IN ('created', 'paid', 'shipped', 'completed', 'closed')), 
    
    -- 檢查約束：金額不能為負 (Check: amounts cannot be negative)
    CONSTRAINT ck_orders_amounts CHECK 
        (total_amount >= 0 AND discount_amount >= 0 AND tax_amount >= 0 AND shipping_amount >= 0), 
    
    CONSTRAINT ck_orders_timeline CHECK (
        (paid_at IS NULL OR paid_at >= created_at) AND       -- 付款時間不能在建立時間之前
        (shipped_at IS NULL OR shipped_at >= paid_at) AND    -- 出貨時間不能在付款時間之前
        (completed_at IS NULL OR completed_at >= shipped_at) -- 完成時間不能在出貨時間之前
    )
);

-- =============================================================================
-- 索引建立 (Indexes)
-- 用途：優化各類常見查詢的效能
-- 說明：PostgreSQL 會自動為 PRIMARY KEY 和 UNIQUE 約束建立索引，
--       以下為額外建立的索引
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 索引 1：用戶訂單查詢索引（必要）
-- 名稱：idx_orders_user
-- 類型：B-tree
-- 欄位：user_id, created_at DESC
-- 用途：加速查詢某個用戶的歷史訂單，並按時間倒序排列
-- 場景：會員中心訂單列表、用戶訂單查詢
-- 範例：SELECT * FROM orders WHERE user_id = 1001 ORDER BY created_at DESC LIMIT 20;
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_orders_user 
    ON orders (user_id, created_at DESC);

-- -----------------------------------------------------------------------------
-- 索引 2：訂單狀態查詢索引（必要）
-- 名稱：idx_orders_status
-- 類型：B-tree
-- 欄位：status, created_at DESC
-- 用途：加速後台依狀態篩選訂單，並按時間倒序排列
-- 場景：訂單管理後台（待付款、待出貨、已完成等分頁）
-- 範例：SELECT * FROM orders WHERE status = 'paid' ORDER BY created_at DESC;
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_orders_status 
    ON orders (status, created_at DESC);

-- -----------------------------------------------------------------------------
-- 索引 3：時間範圍查詢索引（選擇性建立）
-- 名稱：idx_orders_created_at
-- 類型：B-tree
-- 欄位：created_at DESC
-- 用途：加速時間範圍的訂單查詢，用於報表統計
-- 場景：每日/每月訂單報表、銷售統計分析
-- 範例：SELECT DATE(created_at), COUNT(*) FROM orders 
--       WHERE created_at >= '2024-01-01' AND created_at < '2024-02-01'
--       GROUP BY DATE(created_at);
-- 說明：如果經常需要跑時間區間的報表，建議建立此索引
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_orders_created_at 
    ON orders (created_at DESC);

-- =============================================================================
-- 註解說明 (Comments)
-- =============================================================================

COMMENT ON TABLE orders IS '主訂單表，記錄訂單交易層級的核心資訊';
COMMENT ON COLUMN orders.id IS '訂單唯一識別碼，主鍵';
COMMENT ON COLUMN orders.order_number IS '訂單編號，用於查詢和物流';
COMMENT ON COLUMN orders.user_id IS '顧客 ID，關聯到 users 表';
COMMENT ON COLUMN orders.status IS '訂單狀態：created已建立/paid已付款/shipped已出貨/completed已完成/closed已關閉';
COMMENT ON COLUMN orders.total_amount IS '訂單總金額';
COMMENT ON COLUMN orders.discount_amount IS '折扣金額';
COMMENT ON COLUMN orders.tax_amount IS '稅金金額';
COMMENT ON COLUMN orders.shipping_amount IS '運費金額';
COMMENT ON COLUMN orders.created_at IS '訂單建立時間';
COMMENT ON COLUMN orders.paid_at IS '付款時間';
COMMENT ON COLUMN orders.shipped_at IS '出貨時間';
COMMENT ON COLUMN orders.completed_at IS '完成時間';
COMMENT ON COLUMN orders.snapshot IS '訂單快照，記錄當時的商品資訊、價格等';