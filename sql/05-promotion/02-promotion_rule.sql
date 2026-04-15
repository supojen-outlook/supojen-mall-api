-- =============================================================================
-- promotion_rules：促銷規則表
-- 用途：定義促銷活動的具體規則（滿額折扣、贈品等）
-- 設計考量：一個活動可以有多條規則，根據 rule_type 不同只會有部分欄位有值
-- =============================================================================
CREATE TABLE promotion_rules (
    -- 基礎資訊欄位 (Basic Information)
    -- -------------------------------------------------------------------------
    id              INT NOT NULL, -- 規則唯一識別碼 (Unique rule ID)
    promotion_id    INT NOT NULL, -- 所屬促銷活動 ID (Parent promotion ID)
    
    -- 規則名稱 (Rule Name)
    -- -------------------------------------------------------------------------
    tab_name        VARCHAR(100) NOT NULL, -- 規則名稱/標籤，如：「滿千送百」、「雙11折扣」 (Rule display name)
    
    -- 規則類型 (Rule Type)
    -- -------------------------------------------------------------------------
    rule_type       VARCHAR(20) NOT NULL, -- 規則類型：full_reduction滿額減/discount折扣/gift贈品/free_shipping免運 (Rule type)
    
    -- 門檻條件 (Threshold Conditions)
    -- -------------------------------------------------------------------------
    threshold_amount DECIMAL(10,2), -- 滿額門檻，如：1000 表示滿 1000 元 (Amount threshold)
    threshold_quantity INT,         -- 滿件門檻，如：2 表示買 2 件 (Quantity threshold)
    
    -- 折扣內容 (Discount Content)
    -- -------------------------------------------------------------------------
    discount_amount DECIMAL(10,2),     -- 折抵金額（滿減規則專用）(Fixed discount amount)
    discount_rate   DECIMAL(5,2),      -- 折扣率（折扣規則專用），如：20.00 表示 20% off (Discount percentage)
    max_discount_amount DECIMAL(10,2), -- 最高折抵金額（防止折扣無上限）(Maximum discount amount)
    
    -- 贈品資訊 (Gift Information)
    -- -------------------------------------------------------------------------
    gift_item_id    INT,                    -- 贈品商品 ID (Gift product ID)
    gift_quantity   INT NOT NULL DEFAULT 1, -- 贈品數量，預設 1 個 (Gift quantity)
    
    -- 時間戳欄位 (Timestamp Fields)
    -- -------------------------------------------------------------------------
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(), -- 記錄建立時間 (Record creation time)

    -- 約束條件 (Constraints)
    -- -------------------------------------------------------------------------

    -- 主鍵約束：規則唯一識別碼 (Primary key: unique rule ID)
    CONSTRAINT pk_promotion_rules PRIMARY KEY (id),
    
    -- 外鍵約束：刪除促銷活動時自動刪除規則 (Foreign key: delete rules when promotion is deleted)
    CONSTRAINT fk_promotion_rules_promotion 
        FOREIGN KEY (promotion_id) 
        REFERENCES promotions(id) 
        ON DELETE CASCADE,
    
    -- 外鍵約束：刪除贈品商品時設為 NULL (Foreign key: set NULL when gift product is deleted)
    CONSTRAINT fk_promotion_rules_gift 
        FOREIGN KEY (gift_item_id) 
        REFERENCES products(id) 
        ON DELETE SET NULL,
    
    -- 檢查約束：規則類型必須為指定值 (Check: rule type must be valid)
    CONSTRAINT ck_promotion_rules_type CHECK 
        (rule_type IN ('full_reduction', 'discount', 'gift', 'free_shipping')),
    
    -- 規則類型專屬檢查 (Rule type specific checks)
    CONSTRAINT ck_promotion_rules_full_reduction CHECK (
        (rule_type = 'full_reduction' AND discount_amount IS NOT NULL) OR
        rule_type != 'full_reduction'
    ),                                                          -- 滿減規則必須有 discount_amount
    
    CONSTRAINT ck_promotion_rules_discount CHECK (
        (rule_type = 'discount' AND discount_rate IS NOT NULL) OR
        rule_type != 'discount'
    ),                                                          -- 折扣規則必須有 discount_rate
    
    CONSTRAINT ck_promotion_rules_gift CHECK (
        (rule_type = 'gift' AND gift_item_id IS NOT NULL) OR
        rule_type != 'gift'
    ),                                                          -- 贈品規則必須有 gift_item_id
    
    -- 數值範圍檢查 (Value range checks)
    CONSTRAINT ck_promotion_rules_threshold CHECK 
        ((threshold_amount IS NULL OR threshold_amount >= 0) AND
        (threshold_quantity IS NULL OR threshold_quantity > 0)),
    
    CONSTRAINT ck_promotion_rules_amount CHECK 
        (discount_amount IS NULL OR discount_amount >= 0),
    
    CONSTRAINT ck_promotion_rules_rate CHECK 
        (discount_rate IS NULL OR (discount_rate >= 0 AND discount_rate <= 100)),
    
    CONSTRAINT ck_promotion_rules_gift_quantity CHECK 
        (gift_quantity > 0)
);

-- =============================================================================
-- 索引建立 (Indexes)
-- 用途：優化各類常見查詢的效能
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 索引 1：促銷活動查詢索引（必要）
-- 名稱：idx_promotion_rules_promotion
-- 類型：B-tree
-- 欄位：promotion_id
-- 用途：加速查詢某個促銷活動的所有規則
-- 場景：促銷詳情頁面、結帳時計算適用折扣
-- 範例：SELECT * FROM promotion_rules WHERE promotion_id = 1;
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_promotion_rules_promotion 
    ON promotion_rules (promotion_id);

-- -----------------------------------------------------------------------------
-- 索引 2：規則類型查詢索引（選擇性建立）
-- 名稱：idx_promotion_rules_type
-- 類型：B-tree
-- 欄位：rule_type
-- 用途：加速依規則類型篩選
-- 場景：後台只想看滿減規則、統計各類型規則數量
-- 範例：SELECT * FROM promotion_rules WHERE rule_type = 'gift';
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_promotion_rules_type 
    ON promotion_rules (rule_type);

-- =============================================================================
-- 範例資料 (Sample Data)
-- 用途：對應 promotions 範例，為每個活動加入規則
-- =============================================================================

/*
-- 對應 promotions 範例：
-- 1. 雙11全館88折
-- 2. 週年慶滿千送百
-- 3. VIP 免運專屬
*/

INSERT INTO promotion_rules (id, promotion_id, tab_name, rule_type, threshold_amount, discount_rate, discount_amount, gift_item_id) VALUES
    (1, 1, '全館88折', 'discount', NULL, 12.00, NULL, NULL),        -- 88折 = 折扣 12%
    (2, 2, '滿千送百', 'full_reduction', 1000, NULL, 100, NULL),     -- 滿1000 折 100
    (3, 3, 'VIP免運', 'free_shipping', NULL, NULL, NULL, NULL);       -- 免運

-- =============================================================================
-- 驗證範例 (Validation Queries)
-- =============================================================================

/*
-- 1. 查詢某個促銷活動的所有規則
SELECT * FROM promotion_rules WHERE promotion_id = 1;

-- 2. 查詢所有折扣規則
SELECT * FROM promotion_rules WHERE rule_type = 'discount';

-- 3. 查詢有贈品的規則
SELECT * FROM promotion_rules WHERE rule_type = 'gift' AND gift_item_id IS NOT NULL;
*/

-- =============================================================================
-- 註解說明 (Comments)
-- =============================================================================

COMMENT ON TABLE promotion_rules IS '促銷規則表，定義促銷活動的具體規則';
COMMENT ON COLUMN promotion_rules.id IS '規則唯一識別碼，主鍵';
COMMENT ON COLUMN promotion_rules.promotion_id IS '所屬促銷活動 ID';
COMMENT ON COLUMN promotion_rules.tab_name IS '規則名稱/標籤，用於前台顯示';
COMMENT ON COLUMN promotion_rules.rule_type IS '規則類型：full_reduction滿額減/discount折扣/gift贈品/free_shipping免運';
COMMENT ON COLUMN promotion_rules.threshold_amount IS '滿額門檻，達到此金額才適用';
COMMENT ON COLUMN promotion_rules.threshold_quantity IS '滿件門檻，達到此數量才適用';
COMMENT ON COLUMN promotion_rules.discount_amount IS '折抵金額（滿減規則專用）';
COMMENT ON COLUMN promotion_rules.discount_rate IS '折扣率（折扣規則專用），如 20 表示 20% off';
COMMENT ON COLUMN promotion_rules.max_discount_amount IS '最高折抵金額，防止折扣無上限';
COMMENT ON COLUMN promotion_rules.gift_item_id IS '贈品商品 ID';
COMMENT ON COLUMN promotion_rules.gift_quantity IS '贈品數量';
COMMENT ON COLUMN promotion_rules.created_at IS '記錄建立時間';