-- =============================================================================
-- category_attributes：類別屬性關聯表
-- 用途：建立產品類別與屬性鍵之間的多對多關聯關係
-- 設計考量：定義每個類別下可以使用哪些屬性，用於商品發布時的屬性選擇
-- 注意：複合主鍵確保同一類別不會重複關聯同一屬性
-- =============================================================================
CREATE TABLE category_attributes (
    -- 關聯欄位 (Association Fields)
    -- -------------------------------------------------------------------------
    category_id      INT NOT NULL,               -- 類別 ID (Category ID)
    attribute_key_id INT NOT NULL,               -- 屬性鍵 ID (Attribute key ID)

    -- 約束條件 (Constraints)
    -- -------------------------------------------------------------------------

    -- 複合主鍵：確保同一類別下不重複關聯同一屬性 (Composite primary key: prevents duplicate attribute associations for a category)
    CONSTRAINT pk_category_attributes 
        PRIMARY KEY (category_id, attribute_key_id),  
    
    -- 外鍵約束：刪除類別時自動刪除關聯 (Foreign key: delete relations when category is deleted)
    CONSTRAINT fk_category_attributes_category 
        FOREIGN KEY (category_id) 
        REFERENCES categories(id) 
        ON DELETE CASCADE,                            
    
    -- 外鍵約束：刪除屬性鍵時自動刪除關聯 (Foreign key: delete relations when attribute key is deleted)
    CONSTRAINT fk_category_attributes_attribute_key 
        FOREIGN KEY (attribute_key_id) 
        REFERENCES attribute_keys(id) 
        ON DELETE CASCADE                             
);

-- =============================================================================
-- 索引建立 (Indexes)
-- 用途：優化反向查詢的效能
-- 說明：複合主鍵 (category_id, attribute_key_id) 已自動建立複合索引，
--       以下為額外建立的索引用於反向查詢
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 索引 1：屬性鍵反向查詢索引（建議保留）
-- 名稱：idx_category_attributes_attribute_key
-- 類型：B-tree
-- 欄位：attribute_key_id
-- 用途：加速查詢某個屬性被哪些類別使用
-- 場景：後台管理想要了解某個屬性關聯到哪些類別
-- 範例：SELECT * FROM category_attributes WHERE attribute_key_id = 1001;
-- 說明：因為複合主鍵是 (category_id, attribute_key_id)，無法直接加速 attribute_key_id 的查詢
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_category_attributes_attribute_key 
    ON category_attributes (attribute_key_id);

-- =============================================================================
-- 範例資料 (Sample Data)
-- 用途：對應 categories 和 attribute_keys 的範例，建立類別與屬性的關聯
-- =============================================================================

/*
-- 對應 categories 範例：
-- 1000000: 工藝品 (根類別)
-- 1001000: 陶瓷工藝品
-- 1002000: 玻璃工藝品
-- 1003000: 金屬工藝品

-- 對應 attribute_keys 範例：
-- 1001: 顏色 (銷售屬性)
-- 1002: 尺寸 (銷售屬性)
-- 2001: 材質 (非銷售屬性)
-- 2002: 產地 (非銷售屬性)
-- 2006: 季節 (非銷售屬性)
*/

INSERT INTO category_attributes (category_id, attribute_key_id) VALUES
    -- 工藝品根類別適用的通用屬性
    (1000000, 2001),  -- 材質
    (1000000, 2002),  -- 產地
    (1000000, 2006),  -- 季節
    
    -- 陶瓷工藝品適用的屬性
    (1001000, 1001),  -- 顏色
    (1001000, 1002),  -- 尺寸
    (1001000, 2001),  -- 材質
    (1001000, 2002),  -- 產地
    
    -- 玻璃工藝品適用的屬性
    (1002000, 1001),  -- 顏色
    (1002000, 1002),  -- 尺寸
    (1002000, 2001),  -- 材質
    (1002000, 2002),  -- 產地
    
    -- 金屬工藝品適用的屬性
    (1003000, 1001),  -- 顏色
    (1003000, 1002),  -- 尺寸
    (1003000, 2001),  -- 材質
    (1003000, 2002);  -- 產地

-- =============================================================================
-- 驗證範例 (Validation Queries)
-- =============================================================================

/*
-- 1. 查詢某個類別下有哪些屬性
SELECT 
    c.name AS category_name,
    k.name AS attribute_name,
    k.code,
    k.for_sales,
    k.input_type
FROM category_attributes ca
JOIN categories c ON ca.category_id = c.id
JOIN attribute_keys k ON ca.attribute_key_id = k.id
WHERE ca.category_id = 1001000  -- 陶瓷工藝品
ORDER BY k.sort_order;

-- 2. 查詢某個屬性被哪些類別使用
SELECT 
    k.name AS attribute_name,
    c.name AS category_name,
    c.level
FROM category_attributes ca
JOIN attribute_keys k ON ca.attribute_key_id = k.id
JOIN categories c ON ca.category_id = c.id
WHERE ca.attribute_key_id = 2001  -- 材質屬性
ORDER BY c.path_cache;

-- 3. 統計每個類別有多少屬性
SELECT 
    c.name AS category_name,
    COUNT(ca.attribute_key_id) AS attribute_count
FROM categories c
LEFT JOIN category_attributes ca ON c.id = ca.category_id
GROUP BY c.id, c.name
ORDER BY c.sort_order;

-- 4. 查詢某個類別下的銷售屬性
SELECT 
    c.name AS category_name,
    k.name AS attribute_name,
    k.code
FROM category_attributes ca
JOIN categories c ON ca.category_id = c.id
JOIN attribute_keys k ON ca.attribute_key_id = k.id
WHERE ca.category_id = 1001000 
  AND k.for_sales = TRUE
ORDER BY k.sort_order;
*/

-- =============================================================================
-- 註解說明 (Comments)
-- =============================================================================

COMMENT ON TABLE category_attributes IS '類別屬性關聯表，定義每個類別可用的屬性';
COMMENT ON COLUMN category_attributes.category_id IS '類別 ID，關聯到 categories 表';
COMMENT ON COLUMN category_attributes.attribute_key_id IS '屬性鍵 ID，關聯到 attribute_keys 表';