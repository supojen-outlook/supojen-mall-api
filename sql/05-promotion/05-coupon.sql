-- =============================================================================
-- coupons：優惠券表
-- 用途：提供給特定用戶的折扣工具，可指定適用商品、折扣方式
-- 設計考量：獨立於 promotion 系統，單純針對特定用戶或特定商品給予折扣
-- =============================================================================
CREATE TABLE coupons (
    -- 基礎資訊欄位 (Basic Information)
    -- -------------------------------------------------------------------------
    id              BIGINT NOT NULL,                            -- 優惠券唯一識別碼 (Unique coupon ID)
    coupon_code     VARCHAR(50) NOT NULL,                       -- 優惠券代碼，用戶輸入 (Coupon code)
    name            VARCHAR(100) NOT NULL,                      -- 優惠券名稱，如：VIP專屬85折 (Coupon name)
    description     VARCHAR(255),                               -- 優惠券描述 (Coupon description)
    
    -- 擁有者資訊 (Owner Information)
    -- -------------------------------------------------------------------------
    user_id         INT,                                        -- 指定給特定用戶，NULL 表示不指定 (Specific user ID, NULL for any user)
    
    -- 折扣內容 (Discount Content)
    -- -------------------------------------------------------------------------
    discount_amount DECIMAL(5,2) NOT NULL,                      -- 折扣金額，如：100 表示 100 元 (Discount amount)
    
    -- 適用範圍 (Applicable Scope)
    -- -------------------------------------------------------------------------
    scope_type      VARCHAR(20) NOT NULL DEFAULT 'all',         -- 適用範圍：all全部/product商品/category類別/brand品牌 (Scope type)
    scope_id        INT,                                        -- 根據 scope_type 對應到不同表的 ID (Scope identifier)
    
    -- 使用狀態 (Usage Status)
    -- -------------------------------------------------------------------------
    is_used         BOOLEAN NOT NULL DEFAULT FALSE,             -- 是否已使用 (Whether used)
    used_at         TIMESTAMPTZ,                                -- 使用時間 (Usage time)
    order_id        INT,                                        -- 使用的訂單 ID (Order ID)
    
    -- 有效期 (Validity Period)
    -- -------------------------------------------------------------------------
    valid_from      TIMESTAMPTZ NOT NULL DEFAULT NOW(),         -- 有效開始時間 (Valid from)
    valid_until     TIMESTAMPTZ,                                -- 有效截止時間，NULL 表示永久有效 (Valid until)
    
    -- 時間戳欄位 (Timestamp Fields)
    -- -------------------------------------------------------------------------
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),         -- 記錄建立時間 (Record creation time)

    -- 約束條件 (Constraints)
    -- -------------------------------------------------------------------------

    -- 主鍵約束：優惠券唯一識別碼 (Primary key: unique coupon ID)
    CONSTRAINT pk_coupons PRIMARY KEY (id),                     

    -- 唯一約束：優惠券代碼不可重複 (Unique: coupon code must be unique)
    CONSTRAINT uk_coupons_code UNIQUE (coupon_code),            
    
    -- 外鍵約束：刪除用戶時，優惠券的 user_id 設為 NULL
    CONSTRAINT fk_coupons_user 
        FOREIGN KEY (user_id) 
        REFERENCES users(id) 
        ON DELETE SET NULL,                                     
    
    -- 外鍵約束：刪除訂單時，優惠券的 order_id 設為 NULL
    CONSTRAINT fk_coupons_order 
        FOREIGN KEY (order_id) 
        REFERENCES orders(id) 
        ON DELETE SET NULL,                                      
        
    -- 檢查約束：scope_type 必須是有效值 (Check: scope_type must be valid)
    CONSTRAINT ck_coupons_scope_type CHECK 
        (scope_type IN ('all', 'product', 'category', 'brand')),
    
    -- 檢查約束：如果有指定範圍類型，就必須有範圍 ID (Check: scope_id required when scope_type is not 'all')
    CONSTRAINT ck_coupons_scope_id CHECK (
        (scope_type = 'all' AND scope_id IS NULL) OR
        (scope_type != 'all' AND scope_id IS NOT NULL)
    ),
    
    -- 檢查約束：已使用的優惠券必須有 used_at 和 order_id (Check: used coupon must have used_at and order_id)
    CONSTRAINT ck_coupons_used CHECK (
        (is_used = FALSE) OR
        (is_used = TRUE AND used_at IS NOT NULL AND order_id IS NOT NULL)
    ),
    
    -- 檢查約束：有效期必須合理 (Check: valid dates must be logical)
    CONSTRAINT ck_coupons_dates CHECK (
        valid_until IS NULL OR valid_until > valid_from
    )
);

-- =============================================================================
-- 索引建立 (Indexes)
-- 用途：優化各類常見查詢的效能
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 索引 1：優惠券代碼查詢索引（必要）
-- 名稱：idx_coupons_code
-- 類型：B-tree
-- 欄位：coupon_code
-- 用途：加速用戶輸入優惠券代碼時的查詢
-- 場景：結帳時輸入優惠券代碼
-- 範例：SELECT * FROM coupons WHERE coupon_code = 'VIP85' AND is_used = FALSE;
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_coupons_code 
    ON coupons (coupon_code);

-- -----------------------------------------------------------------------------
-- 索引 2：用戶優惠券查詢索引（必要）
-- 名稱：idx_coupons_user
-- 類型：B-tree
-- 欄位：user_id, is_used, valid_until
-- 用途：加速查詢某個用戶的可使用優惠券
-- 場景：會員中心顯示可用優惠券
-- 範例：SELECT * FROM coupons 
--       WHERE user_id = 1001 
--         AND is_used = FALSE 
--         AND valid_from <= NOW() 
--         AND (valid_until IS NULL OR valid_until > NOW());
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_coupons_user 
    ON coupons (user_id, is_used, valid_until) 
    WHERE user_id IS NOT NULL;

-- -----------------------------------------------------------------------------
-- 索引 3：適用範圍查詢索引（必要）
-- 名稱：idx_coupons_scope
-- 類型：B-tree
-- 欄位：scope_type, scope_id
-- 用途：加速查詢某個商品有哪些可用的優惠券
-- 場景：商品詳情頁顯示可用的優惠券
-- 範例：SELECT * FROM coupons 
--       WHERE (scope_type = 'all' OR (scope_type = 'product' AND scope_id = 1001))
--         AND is_used = FALSE 
--         AND valid_from <= NOW() 
--         AND (valid_until IS NULL OR valid_until > NOW());
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_coupons_scope 
    ON coupons (scope_type, scope_id) 
    WHERE is_used = FALSE;

-- -----------------------------------------------------------------------------
-- 索引 4：過期優惠券查詢索引（選擇性建立）
-- 名稱：idx_coupons_expired
-- 類型：B-tree
-- 欄位：valid_until
-- 用途：加速清理過期優惠券的定時任務
-- 場景：每天凌晨執行任務，將過期優惠券標記為無效
-- 範例：SELECT * FROM coupons 
--       WHERE valid_until < NOW() AND is_used = FALSE;
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_coupons_expired 
    ON coupons (valid_until) 
    WHERE valid_until IS NOT NULL AND is_used = FALSE;

-- =============================================================================
-- 範例資料 (Sample Data)
-- 用途：展示各種優惠券的設定方式
-- =============================================================================

/*
-- 對應 users 範例：
-- 1001: 王小明
-- 1002: 李小華
-- 1003: 張阿美

-- 對應 products 範例：
-- 10001: 青花瓷茶具組
-- 20001: 手工玻璃花器
*/

INSERT INTO coupons (id, coupon_code, name, description, user_id, discount_rate, scope_type, scope_id, valid_from, valid_until) VALUES
    -- 指定用戶的全館折扣券
    (1, 'VIP85', 'VIP 專屬85折', 'VIP會員感謝祭，全館85折', 1001, 15.00, 'all', NULL, '2024-01-01 00:00:00+00', '2024-12-31 23:59:59+00'),
    
    -- 指定用戶的商品折扣券
    (2, 'TEA20', '茶具好友價', '青花瓷茶具組專用8折', 1001, 20.00, 'product', 10001, '2024-02-01 00:00:00+00', '2024-03-31 23:59:59+00'),
    
    -- 未指定用戶的類別折扣券（可公開領取）
    (3, 'GLASS15', '玻璃工藝85折', '所有玻璃工藝品85折', NULL, 15.00, 'category', 1002000, '2024-03-01 00:00:00+00', '2024-05-31 23:59:59+00'),
    
    -- 指定 Email 的新人禮
    (4, 'WELCOME10', '新朋友見面禮', '首次購物9折', NULL, 10.00, 'all', NULL, '2024-01-01 00:00:00+00', '2024-12-31 23:59:59+00'),
    
    -- 品牌專屬折扣券
    (5, 'AUDI10', 'Audi 粉絲價', 'Audi 精品9折', 1002, 10.00, 'brand', 1100, '2024-04-01 00:00:00+00', '2024-06-30 23:59:59+00');

-- =============================================================================
-- 驗證範例 (Validation Queries)
-- =============================================================================

/*
-- 1. 查詢某個用戶的可用優惠券
SELECT * FROM coupons 
WHERE user_id = 1001 
  AND is_used = FALSE 
  AND valid_from <= NOW() 
  AND (valid_until IS NULL OR valid_until > NOW())
ORDER BY valid_until;

-- 2. 查詢某個商品可用的公開優惠券
SELECT * FROM coupons 
WHERE user_id IS NULL 
  AND is_used = FALSE 
  AND valid_from <= NOW() 
  AND (valid_until IS NULL OR valid_until > NOW())
  AND (
    scope_type = 'all' 
    OR (scope_type = 'product' AND scope_id = 10001)
    OR (scope_type = 'category' AND scope_id IN (
        SELECT category_id FROM products WHERE id = 10001
    ))
    OR (scope_type = 'brand' AND scope_id IN (
        SELECT brand_id FROM products WHERE id = 10001
    ))
  )
ORDER BY discount_rate DESC;

-- 3. 驗證優惠券代碼是否可用
SELECT * FROM coupons 
WHERE coupon_code = 'VIP85' 
  AND is_used = FALSE 
  AND valid_from <= NOW() 
  AND (valid_until IS NULL OR valid_until > NOW());

-- 4. 查詢即將過期的優惠券
SELECT * FROM coupons 
WHERE valid_until BETWEEN NOW() AND NOW() + INTERVAL '7 days'
  AND is_used = FALSE
ORDER BY valid_until;

-- 5. 查詢特定商品的優惠券（完整檢查）
WITH target_product AS (
    SELECT 10001 AS product_id, 1001000 AS category_id, 1100 AS brand_id
)
SELECT c.*
FROM coupons c
CROSS JOIN target_product tp
WHERE c.is_used = FALSE
  AND c.valid_from <= NOW()
  AND (c.valid_until IS NULL OR c.valid_until > NOW())
  AND (
    c.scope_type = 'all'
    OR (c.scope_type = 'product' AND c.scope_id = tp.product_id)
    OR (c.scope_type = 'category' AND c.scope_id = tp.category_id)
    OR (c.scope_type = 'brand' AND c.scope_id = tp.brand_id)
  )
ORDER BY c.discount_rate DESC;
*/

-- =============================================================================
-- 註解說明 (Comments)
-- =============================================================================

COMMENT ON TABLE coupons IS '優惠券表，提供給特定用戶的折扣工具';
COMMENT ON COLUMN coupons.id IS '優惠券唯一識別碼，主鍵';
COMMENT ON COLUMN coupons.coupon_code IS '優惠券代碼，用戶輸入';
COMMENT ON COLUMN coupons.name IS '優惠券名稱，如：VIP專屬85折';
COMMENT ON COLUMN coupons.description IS '優惠券描述';
COMMENT ON COLUMN coupons.user_id IS '指定給特定用戶，NULL 表示不指定';
COMMENT ON COLUMN coupons.discount_amount IS '折扣金額，單位為元';
COMMENT ON COLUMN coupons.scope_type IS '適用範圍：all全部/product商品/category類別/brand品牌';
COMMENT ON COLUMN coupons.scope_id IS '範圍 ID，根據 scope_type 對應到不同表的 ID';
COMMENT ON COLUMN coupons.is_used IS '是否已使用';
COMMENT ON COLUMN coupons.used_at IS '使用時間';
COMMENT ON COLUMN coupons.order_id IS '使用的訂單 ID';
COMMENT ON COLUMN coupons.valid_from IS '有效開始時間';
COMMENT ON COLUMN coupons.valid_until IS '有效截止時間，NULL 表示永久有效';
COMMENT ON COLUMN coupons.created_at IS '記錄建立時間';