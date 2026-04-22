-- =============================================================================
-- payments：付款資訊表
-- 用途：記錄訂單的付款方式、金額、狀態等資訊
-- 設計考量：一個訂單可能有多筆付款（如部分付款、分期），但通常為一對一
-- =============================================================================
CREATE TABLE payments (
    -- 基礎資訊欄位 (Basic Information)
    -- -------------------------------------------------------------------------
    id              INT NOT NULL, -- 付款記錄唯一識別碼 (Unique payment ID)
    order_id        INT NOT NULL, -- 訂單 ID (Order ID)
    
    -- 付款方式 (Payment Method)
    -- -------------------------------------------------------------------------
    method          VARCHAR(50) NOT NULL, -- 付款方式 (Payment method)
    
    -- 金流資訊 (Payment Gateway Information)
    -- -------------------------------------------------------------------------
    transaction_id  VARCHAR(100),           -- 金流平台交易編號 (Payment gateway transaction ID)
    amount          DECIMAL(19,2) NOT NULL, -- 付款金額 (Payment amount)
    
    -- 付款狀態 (Payment Status)
    -- -------------------------------------------------------------------------
    -- 狀態流程：pending(處理中) → paid(已付款) 
    --                               ↘ failed(失敗) / refunded(已退款)
    status          VARCHAR(30) NOT NULL DEFAULT 'pending',  -- 付款狀態 (Payment status)

    -- 銀行資訊 (Bank Information)
    -- -------------------------------------------------------------------------
    bank_code       VARCHAR(10), -- 銀行代碼 (Bank code)
    bank_account    VARCHAR(50), -- 銀行帳號 (Bank account)
    expired_at      DATE, -- 付款到期時間-非同步付款方式 (Expiration time)
    
    -- 時間戳欄位 (Timestamp Fields)
    -- -------------------------------------------------------------------------
    paid_at         TIMESTAMPTZ,                         -- 付款時間 (Payment time)
    refunded_at     TIMESTAMPTZ,                         -- 退款時間 (Refund time)
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),  -- 記錄建立時間 (Record creation time)
    
    -- 快照資訊 (Snapshot Information)
    -- -------------------------------------------------------------------------
    snapshot        JSONB,                                -- 金流平台回傳的完整交易資訊 (Payment gateway snapshot)

    -- 約束條件 (Constraints)
    -- -------------------------------------------------------------------------

    -- 主鍵約束：付款記錄唯一識別碼 (Primary key: unique payment ID)
    CONSTRAINT pk_payments PRIMARY KEY (id),      
    
    -- 外鍵約束：有付款記錄的訂單不能被刪除 (Foreign key: cannot delete order with payments)
    CONSTRAINT fk_payments_order 
        FOREIGN KEY (order_id) 
        REFERENCES orders(id) 
        ON DELETE CASCADE,
    
    -- 檢查約束：付款方式必須為指定值 (Check: payment method must be valid)
    CONSTRAINT ck_payments_method CHECK 
        (method IN (
            'credit_card_one_time', -- 1. 信用卡 (一次付清) : 2.8%
            'atm_virtual',          -- 2. 實體 ATM (VACC) : 1%
            'taiwan_pay',           -- 3. 台灣 Pay : 1.5%
            'cash',                 -- 4. 現金交易 (線下/面交) : 0%
            'cvs',                  -- 5. 超商代碼/條碼
            'other'                 -- 其他付款方式
        )),
    
    -- 檢查約束：付款狀態必須為指定值 (Check: payment status must be pending, paid, failed, or refunded)
    CONSTRAINT ck_payments_status CHECK 
        (status IN ('pending', 'paid', 'failed', 'refunded')),
    
    -- 檢查約束：付款金額必須大於 0 (Check: payment amount must be > 0)
    CONSTRAINT ck_payments_amount CHECK 
        (amount > 0),
    
    CONSTRAINT ck_payments_timeline CHECK (
        (paid_at IS NULL OR paid_at >= created_at) AND  -- 付款時間不能在建立時間之前
        (refunded_at IS NULL OR refunded_at >= paid_at) -- 退款時間不能在付款時間之前
    )
);

-- =============================================================================
-- 索引建立 (Indexes)
-- 用途：優化各類常見查詢的效能
-- 說明：PostgreSQL 會自動為 PRIMARY KEY 建立索引，以下為額外建立的索引
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 索引 1：訂單查詢索引（必要）
-- 名稱：idx_payments_order
-- 類型：B-tree
-- 欄位：order_id
-- 用途：加速查詢某個訂單的所有付款記錄
-- 場景：訂單詳情頁面顯示付款資訊、訂單對帳
-- 範例：SELECT * FROM payments WHERE order_id = 1001 ORDER BY created_at;
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_payments_order 
    ON payments (order_id);

-- -----------------------------------------------------------------------------
-- 索引 2：交易編號查詢索引（必要）
-- 名稱：idx_payments_transaction
-- 類型：B-tree
-- 欄位：transaction_id
-- 用途：加速金流交易編號查詢，用於對帳和退款處理
-- 場景：金流平台對帳、客服查詢交易、退款查詢
-- 範例：SELECT * FROM payments WHERE transaction_id = 'GATEWAY123456';
-- 說明：部分索引只對有交易編號的記錄建立，節省空間
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_payments_transaction 
    ON payments (transaction_id) 
    WHERE transaction_id IS NOT NULL;

-- -----------------------------------------------------------------------------
-- 索引 3：付款狀態查詢索引（選擇性建立）
-- 名稱：idx_payments_status
-- 類型：B-tree
-- 欄位：status, created_at DESC
-- 用途：加速依付款狀態篩選記錄，用於財務對帳
-- 場景：財務查詢待付款訂單、失敗交易重試、退款處理
-- 範例：SELECT * FROM payments WHERE status = 'pending' ORDER BY created_at;
-- 說明：如果付款記錄數量很大且經常依狀態查詢，才需要此索引
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_payments_status 
    ON payments (status, created_at DESC);

-- =============================================================================
-- 範例資料 (Sample Data)
-- =============================================================================

INSERT INTO payments (id, order_id, method, transaction_id, amount, status, paid_at, snapshot) VALUES
    (1, 2, 'credit_card_one_time', 'GATEWAY123456', 5800, 'paid', NOW(), 
     '{"card_last4": "1234", "auth_code": "A12345"}'::JSONB),
    (2, 3, 'atm_virtual', 'VIRTUAL123456', 3200, 'paid', NOW(),
     '{"bank_code": "007", "account": "12345678901234"}'::JSONB);

-- =============================================================================
-- 註解說明 (Comments)
-- =============================================================================

COMMENT ON TABLE payments IS '付款資訊表，記錄訂單的付款方式、金額、狀態等';
COMMENT ON COLUMN payments.id IS '付款記錄唯一識別碼，主鍵';
COMMENT ON COLUMN payments.order_id IS '訂單 ID，關聯到 orders 表';
COMMENT ON COLUMN payments.method IS '付款方式：credit_card_one_time信用卡一次付清/credit_card_installment信用卡分期/credit_card_foreign國外卡/mobile_payment行動支付/atm_virtual虛擬帳號/webatm_taiwanpay網路ATM/cvs_code超商代碼/cvs_barcode超商條碼';
COMMENT ON COLUMN payments.bank_code IS '付款銀行代碼';
COMMENT ON COLUMN payments.code_no IS '付款代碼';
COMMENT ON COLUMN payments.expired_at IS '付款代碼有效期';
COMMENT ON COLUMN payments.transaction_id IS '金流平台交易編號';
COMMENT ON COLUMN payments.amount IS '付款金額';
COMMENT ON COLUMN payments.status IS '付款狀態：pending處理中/paid已付款/failed失敗/refunded已退款';
COMMENT ON COLUMN payments.paid_at IS '付款時間';
COMMENT ON COLUMN payments.refunded_at IS '退款時間';
COMMENT ON COLUMN payments.created_at IS '記錄建立時間';
COMMENT ON COLUMN payments.snapshot IS '金流平台回傳的完整交易資訊';