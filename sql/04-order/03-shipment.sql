-- =============================================================================
-- shipments：物流單表
-- 用途：記錄訂單項目的出貨資訊，包含物流方式、追蹤編號等
-- 設計考量：一個訂單項目可以分批出貨，所以一個 order_item 可對應多個 shipment
-- =============================================================================
CREATE TABLE shipments (
    -- 基礎資訊欄位 (Basic Information)
    -- -------------------------------------------------------------------------
    id              INT NOT NULL,                       -- 物流單唯一識別碼 (Unique shipment ID)
    order_id        INT NOT NULL,                       -- 訂單 ID (Order ID)
    
    -- 物流資訊 (Shipping Information)
    -- -------------------------------------------------------------------------
    method          VARCHAR(20),                        -- 物流方式 (Shipping method)
    tracking_number VARCHAR(100),                       -- 物流追蹤編號 (Tracking number)
    shipping_address VARCHAR(255),                      -- 寄送地址 (Shipping address)
    recipient_name VARCHAR(100),                        -- 收件人姓名 (Recipient name)
    recipient_phone VARCHAR(20),                        -- 收件人電話 (Recipient phone)
    
    -- 時間戳欄位 (Timestamp Fields)
    -- -------------------------------------------------------------------------
    ship_date       TIMESTAMPTZ,                        -- 出貨日期 (Shipping date)
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(), -- 記錄建立時間 (Record creation time)

    -- 約束條件 (Constraints)
    -- -------------------------------------------------------------------------

    -- 主鍵約束：物流單唯一識別碼 (Primary key: unique shipment ID)
    CONSTRAINT pk_shipments PRIMARY KEY (id),
    
    -- 外鍵約束：訂單 ID (Foreign key: order ID)
    CONSTRAINT fk_shipments_order 
            FOREIGN KEY (order_id) 
            REFERENCES orders(id) 
            ON DELETE CASCADE,
    
    -- 檢查約束：物流方式必須為指定值（若有的話）(Check: shipping method must be valid if provided)
    CONSTRAINT ck_shipments_method CHECK 
        (method IS NULL OR method IN (
            'post', 'seven', 'family', 'hilife', 'ok', 
            'tcat', 'ecam'
        ))                                                    
);

-- =============================================================================
-- 索引建立 (Indexes)
-- 用途：優化各類常見查詢的效能
-- 說明：PostgreSQL 會自動為 PRIMARY KEY 建立索引，以下為額外建立的索引
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 索引 1：訂單查詢索引（必要）
-- 名稱：idx_shipments_order
-- 類型：B-tree
-- 欄位：order_id
-- 用途：加速依訂單查詢物流單，用於訂單物流狀態更新
-- 場景：訂單物流狀態更新、客服查詢物流狀態、物流報表
-- 範例：SELECT * FROM shipments WHERE order_id = 123;
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_shipments_order
    ON shipments (order_id);

-- -----------------------------------------------------------------------------
-- 索引 2：追蹤編號查詢索引（必要）
-- 名稱：idx_shipments_tracking
-- 類型：B-tree
-- 欄位：tracking_number
-- 用途：加速物流追蹤編號查詢，用於物流狀態更新
-- 場景：物流串接查詢、客服查詢包裹狀態、消費者追蹤貨態
-- 範例：SELECT * FROM shipments WHERE tracking_number = 'TCAT123456789';
-- 說明：部分索引只對有追蹤編號的記錄建立，節省空間
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_shipments_tracking 
    ON shipments (tracking_number) 
    WHERE tracking_number IS NOT NULL;

-- -----------------------------------------------------------------------------
-- 索引 3：出貨日期查詢索引（選擇性建立）
-- 名稱：idx_shipments_ship_date
-- 類型：B-tree
-- 欄位：ship_date DESC
-- 用途：加速依出貨日期範圍查詢，用於物流報表
-- 場景：每日出貨統計、物流對帳、運費結算
-- 範例：SELECT COUNT(*), method FROM shipments 
--       WHERE ship_date >= '2024-01-01' AND ship_date < '2024-02-01'
--       GROUP BY method;
-- 說明：如果經常需要統計出貨量或跑物流報表，才需要此索引
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_shipments_ship_date 
    ON shipments (ship_date DESC) 
    WHERE ship_date IS NOT NULL;


-- =============================================================================
-- 註解說明 (Comments)
-- =============================================================================

COMMENT ON TABLE shipments IS '物流單表，記錄訂單項目的出貨資訊';
COMMENT ON COLUMN shipments.id IS '物流單唯一識別碼，主鍵';
COMMENT ON COLUMN shipments.order_id IS '訂單項目 ID，關聯到 order_items 表';
COMMENT ON COLUMN shipments.method IS '物流方式：post中華郵政/seven-11/family全家/hilife萊爾富/ok Ok Mart/tcat黑貓/ecam宅配通';
COMMENT ON COLUMN shipments.tracking_number IS '物流追蹤編號';
COMMENT ON COLUMN shipments.recipient_name IS '收件人姓名';
COMMENT ON COLUMN shipments.recipient_phone IS '收件人電話';
COMMENT ON COLUMN shipments.shipping_address IS '寄送地址';
COMMENT ON COLUMN shipments.ship_date IS '出貨日期';
COMMENT ON COLUMN shipments.created_at IS '記錄建立時間';