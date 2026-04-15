-- =============================================================================
-- returns：退貨單表
-- 用途：記錄客戶退貨申請、處理流程、退款狀態等
-- 設計考量：一個訂單項目可以多次退貨（部分退貨），但通常一次退貨對應一個訂單項目
-- =============================================================================
CREATE TABLE returns (
    -- 基礎資訊欄位 (Basic Information)
    -- -------------------------------------------------------------------------
    id              INT NOT NULL,         -- 退貨單唯一識別碼 (Unique return ID)
    return_number   VARCHAR(50) NOT NULL, -- 退貨單編號 (Return number)
    order_item_id   INT NOT NULL,         -- 訂單項目 ID (Order item ID)
    
    -- 退貨資訊 (Return Information)
    -- -------------------------------------------------------------------------
    quantity        INT NOT NULL,         -- 退貨數量 (Return quantity)
    reason          VARCHAR(255),         -- 退貨原因 (Return reason)
    
    -- 退貨狀態 (Return Status)
    -- -------------------------------------------------------------------------
    -- 狀態流程：requested(申請) → approved(核准) → received(收到貨) → refunded(已退款)
    --                                  ↘ rejected(拒絕)
    status          VARCHAR(30) NOT NULL DEFAULT 'requested', -- 退貨狀態 (Return status)
    
    -- 退款資訊 (Refund Information)
    -- -------------------------------------------------------------------------
    refund_amount   DECIMAL(19,2), -- 退款金額 (Refund amount)
    refund_method   VARCHAR(20),   -- 退款方式：original原路退回/balance購物金/bank_transfer銀行轉帳 (Refund method)
    refunded_at     TIMESTAMPTZ,   -- 退款時間 (Refund time)
    
    -- 時間戳欄位 (Timestamp Fields)
    -- -------------------------------------------------------------------------
    requested_at    TIMESTAMPTZ NOT NULL DEFAULT NOW(), -- 申請時間 (Request time)
    approved_at     TIMESTAMPTZ,                        -- 核准時間 (Approval time)
    received_at     TIMESTAMPTZ,                        -- 收到退貨時間 (Receipt time)
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(), -- 記錄建立時間 (Record creation time)
    
    -- 備註 (Notes)
    -- -------------------------------------------------------------------------
    staff_notes     TEXT,  -- 客服/倉管備註 (Staff notes)
    customer_notes  TEXT,  -- 客戶備註 (Customer notes)

    -- 約束條件 (Constraints)
    -- -------------------------------------------------------------------------

    -- 主鍵約束：退貨單唯一識別碼 (Primary key: unique return ID)
    CONSTRAINT pk_returns PRIMARY KEY (id),              

    -- 唯一約束：退貨單編號不可重複 (Unique: return number must be unique)
    CONSTRAINT uk_returns_number UNIQUE (return_number),      
    
    -- 外鍵約束：有退貨的訂單項目不能被刪除 (Foreign key: cannot delete order item with returns)
    CONSTRAINT fk_returns_order_item 
        FOREIGN KEY (order_item_id) 
        REFERENCES order_items(id) 
        ON DELETE RESTRICT,                                  
    
    -- 檢查約束：退貨數量必須大於 0 (Check: return quantity must be > 0)
    CONSTRAINT ck_returns_quantity CHECK 
        (quantity > 0),                                
    
    -- 檢查約束：退貨狀態必須為指定值
    CONSTRAINT ck_returns_status CHECK 
        (status IN ('requested', 'approved', 'rejected', 'received', 'refunded')),  
    
    -- 檢查約束：退款方式必須為指定值
    CONSTRAINT ck_returns_refund_method CHECK 
        (refund_method IS NULL OR refund_method IN ('original', 'balance', 'bank_transfer')),  
    
    -- 檢查約束：退款金額不能為負 (Check: refund amount cannot be negative)
    CONSTRAINT ck_returns_refund_amount CHECK 
        (refund_amount IS NULL OR refund_amount >= 0),         
    
    CONSTRAINT ck_returns_timeline CHECK (
        (approved_at IS NULL OR approved_at >= requested_at) AND
        (received_at IS NULL OR received_at >= approved_at) AND
        (refunded_at IS NULL OR refunded_at >= received_at)
    )
);

-- =============================================================================
-- 索引建立 (Indexes)
-- 用途：優化各類常見查詢的效能
-- =============================================================================

CREATE INDEX IF NOT EXISTS idx_returns_order_item ON returns (order_item_id);
CREATE INDEX IF NOT EXISTS idx_returns_status ON returns (status, requested_at DESC);
CREATE INDEX IF NOT EXISTS idx_returns_requested_at ON returns (requested_at DESC);

-- =============================================================================
-- 索引建立 (Indexes)
-- 用途：優化各類常見查詢的效能
-- 說明：PostgreSQL 會自動為 PRIMARY KEY 和 UNIQUE 約束建立索引，
--       以下為額外建立的索引
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 索引 1：訂單項目查詢索引（必要）
-- 名稱：idx_returns_order_item
-- 類型：B-tree
-- 欄位：order_item_id
-- 用途：加速查詢某個訂單項目的所有退貨記錄
-- 場景：訂單詳情頁面顯示退貨進度、客服查詢退貨歷史
-- 範例：SELECT * FROM returns WHERE order_item_id = 1001 ORDER BY requested_at;
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_returns_order_item 
    ON returns (order_item_id);

-- -----------------------------------------------------------------------------
-- 索引 2：退貨狀態查詢索引（必要）
-- 名稱：idx_returns_status
-- 類型：B-tree
-- 欄位：status, requested_at DESC
-- 用途：加速依狀態篩選退貨單，用於退貨流程管理
-- 場景：客服處理退貨申請、倉庫收到退貨、財務退款作業
-- 範例：SELECT * FROM returns WHERE status = 'requested' ORDER BY requested_at;
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_returns_status 
    ON returns (status, requested_at DESC);

-- -----------------------------------------------------------------------------
-- 索引 3：申請時間查詢索引（選擇性建立）
-- 名稱：idx_returns_requested_at
-- 類型：B-tree
-- 欄位：requested_at DESC
-- 用途：加速時間範圍的退貨查詢，用於退貨報表統計
-- 場景：每月退貨率統計、退貨原因分析、客服績效考核
-- 範例：SELECT DATE(requested_at), COUNT(*) FROM returns 
--       WHERE requested_at >= '2024-01-01' AND requested_at < '2024-02-01'
--       GROUP BY DATE(requested_at);
-- 說明：如果經常需要統計退貨趨勢或跑報表，才需要此索引
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_returns_requested_at 
    ON returns (requested_at DESC);

-- =============================================================================
-- 驗證範例 (Validation Queries)
-- =============================================================================

/*
-- 1. 查詢待處理退貨申請
SELECT 
    r.return_number,
    o.order_number,
    oi.product_name,
    r.quantity,
    r.reason,
    r.requested_at
FROM returns r
JOIN order_items oi ON r.order_item_id = oi.id
JOIN orders o ON oi.order_id = o.id
WHERE r.status = 'requested'
ORDER BY r.requested_at;

-- 2. 查詢某個訂單的所有退貨
SELECT 
    o.order_number,
    oi.product_name,
    r.return_number,
    r.quantity,
    r.status,
    r.refund_amount
FROM returns r
JOIN order_items oi ON r.order_item_id = oi.id
JOIN orders o ON oi.order_id = o.id
WHERE o.id = 3;
*/

-- =============================================================================
-- 註解說明 (Comments)
-- =============================================================================

COMMENT ON TABLE returns IS '退貨單表，記錄客戶退貨申請、處理流程、退款狀態等';
COMMENT ON COLUMN returns.id IS '退貨單唯一識別碼，主鍵';
COMMENT ON COLUMN returns.return_number IS '退貨單編號';
COMMENT ON COLUMN returns.order_item_id IS '訂單項目 ID，關聯到 order_items 表';
COMMENT ON COLUMN returns.quantity IS '退貨數量';
COMMENT ON COLUMN returns.reason IS '退貨原因';
COMMENT ON COLUMN returns.status IS '退貨狀態：requested申請/approved核准/rejected拒絕/received收到貨/refunded已退款';
COMMENT ON COLUMN returns.refund_amount IS '退款金額';
COMMENT ON COLUMN returns.refund_method IS '退款方式：original原路退回/balance購物金/bank_transfer銀行轉帳';
COMMENT ON COLUMN returns.refunded_at IS '退款時間';
COMMENT ON COLUMN returns.requested_at IS '申請時間';
COMMENT ON COLUMN returns.approved_at IS '核准時間';
COMMENT ON COLUMN returns.received_at IS '收到退貨時間';
COMMENT ON COLUMN returns.created_at IS '記錄建立時間';
COMMENT ON COLUMN returns.staff_notes IS '客服/倉管備註';
COMMENT ON COLUMN returns.customer_notes IS '客戶備註';