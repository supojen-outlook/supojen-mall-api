-- =============================================================================
-- shipping_rules：運費規則表
-- 用途：定義訂單運費計算規則，支援按數量和按金額兩種計算方式
-- 設計考量：
--   - 與促銷規則系統保持一致的設計風格
--   - 支援多種運費計算方式（按數量、按金額）
--   - 使用 JSONB 儲存條件，保持彈性
--   - 支援優先級機制，確保規則按正確順序匹配
--   - 規則類型（ruleType）儲存在 conditions JSONB 欄位中，而非獨立欄位
-- 注意：
--   - 按數量計算時，只計算 unit_of_measure_id = 1 的商品數量
--   - 按金額計算時，基於訂單總金額（不含運費）
-- =============================================================================
CREATE TABLE shipping_rules (
    -- 基礎資訊欄位 (Basic Information)
    -- -------------------------------------------------------------------------
    id              INT NOT NULL,         -- 規則唯一識別碼 (Unique rule ID)
    name            VARCHAR(100) NOT NULL, -- 規則名稱 (Rule name)
    description     TEXT,                 -- 規則描述 (Rule description)
    
    -- 適用條件 (Applicable Conditions)
    -- -------------------------------------------------------------------------
    -- 按數量：{"ruleType": "quantity", "unitOfMeasureId": 1, "minQuantity": 5, "maxQuantity": 10}
    -- 按金額：{"ruleType": "amount", "minAmount": 1000, "maxAmount": 2000}
    -- 注意：ruleType 欄位已移除，規則類型現在儲存在 conditions JSONB 欄位中
    conditions      JSONB,                -- 適用條件 (Applicable conditions)
    
    -- 運費金額 (Shipping Fee)
    -- -------------------------------------------------------------------------
    shipping_fee    DECIMAL(10,2) NOT NULL, -- 運費金額 (Shipping fee)
    
    -- 規則狀態 (Rule Status)
    -- -------------------------------------------------------------------------
    is_active       BOOLEAN NOT NULL DEFAULT TRUE, -- 是否啟用 (Is active)
    
    -- 優先級 (Priority)
    -- -------------------------------------------------------------------------
    -- 數字越小優先級越高
    priority        INT NOT NULL DEFAULT 0, -- 優先級 (Priority)
    
    -- 時間戳欄位 (Timestamp Fields)
    -- -------------------------------------------------------------------------
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(), -- 記錄建立時間 (Creation time)

    -- 約束條件 (Constraints)
    -- -------------------------------------------------------------------------

    -- 主鍵約束：規則唯一識別碼 (Primary key: unique rule ID)
    CONSTRAINT pk_shipping_rules PRIMARY KEY (id),                    
    
    -- 檢查約束：運費金額不能為負 (Check: shipping fee cannot be negative)
    CONSTRAINT ck_shipping_rules_shipping_fee CHECK 
        (shipping_fee >= 0)
);

-- =============================================================================
-- 索引建立 (Indexes)
-- 用途：優化各類常見查詢的效能
-- 說明：PostgreSQL 會自動為 PRIMARY KEY 和 UNIQUE 約束建立索引，
--       以下為額外建立的索引
-- =============================================================================


-- -----------------------------------------------------------------------------
-- 索引 1：啟用狀態查詢索引（必要）
-- 名稱：idx_shipping_rules_is_active
-- 類型：B-tree
-- 欄位：is_active, priority
-- 用途：加速查詢啟用的規則，並按優先級排序
-- 場景：計算訂單運費時，只使用啟用的規則
-- 範例：SELECT * FROM shipping_rules 
--       WHERE is_active = TRUE 
--       ORDER BY priority;
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_shipping_rules_is_active 
    ON shipping_rules (is_active, priority);

-- -----------------------------------------------------------------------------
-- 索引 2：優先級查詢索引（必要）
-- 名稱：idx_shipping_rules_priority
-- 類型：B-tree
-- 欄位：priority
-- 用途：加速按優先級排序的規則查詢
-- 場景：計算訂單運費時，確保規則按正確順序匹配
-- 範例：SELECT * FROM shipping_rules 
--       WHERE is_active = TRUE 
--       ORDER BY priority;
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_shipping_rules_priority 
    ON shipping_rules (priority);

-- =============================================================================
-- 範例資料 (Sample Data)
-- =============================================================================

-- 規則1：按數量計算 - 買5件以下收60元
INSERT INTO shipping_rules (id, name, description, conditions, shipping_fee, is_active, priority) VALUES
    (1, '少量運費', '購買5件以下收60元運費', 
     '{"ruleType": "quantity", "minAmount": 0, "maxAmount": 5}', 
     60, TRUE, 1);

-- 規則2：按數量計算 - 買5-10件收100元
INSERT INTO shipping_rules (id, name, description, conditions, shipping_fee, is_active, priority) VALUES
    (2, '中等數量運費', '購買5-10件收100元運費', 
     '{"ruleType": "quantity", "minAmount": 5, "maxAmount": 10}', 
     100, TRUE, 2);

-- 規則3：按數量計算 - 買10件以上免運
INSERT INTO shipping_rules (id, name, description, conditions, shipping_fee, is_active, priority) VALUES
    (3, '大量免運', '購買10件以上免運費', 
     '{"ruleType": "quantity", "minAmount": 10}', 
     0, TRUE, 3);

-- 規則4：按金額計算 - 滿1000元免運
INSERT INTO shipping_rules (id, name, description, conditions, shipping_fee, is_active, priority) VALUES
    (4, '滿額免運', '滿1000元免運費', 
     '{"ruleType": "amount", "minAmount": 1000}', 
     0, TRUE, 4);

-- 規則5：按金額計算 - 未滿1000元收150元
INSERT INTO shipping_rules (id, name, description, conditions, shipping_fee, is_active, priority) VALUES
    (5, '基本運費', '未滿1000元收150元運費', 
     '{"ruleType": "amount", "maxAmount": 1000}', 
     150, TRUE, 5);

-- =============================================================================
-- 註解說明 (Comments)
-- =============================================================================

COMMENT ON TABLE shipping_rules IS '運費規則表，定義訂單運費計算規則';
COMMENT ON COLUMN shipping_rules.id IS '規則唯一識別碼，主鍵';
COMMENT ON COLUMN shipping_rules.name IS '規則名稱';
COMMENT ON COLUMN shipping_rules.description IS '規則描述';
COMMENT ON COLUMN shipping_rules.conditions IS '適用條件，JSON格式，包含規則類型（ruleType）和具體條件';
COMMENT ON COLUMN shipping_rules.shipping_fee IS '運費金額';
COMMENT ON COLUMN shipping_rules.is_active IS '是否啟用';
COMMENT ON COLUMN shipping_rules.priority IS '優先級，數字越小優先級越高';
COMMENT ON COLUMN shipping_rules.created_at IS '記錄建立時間';
