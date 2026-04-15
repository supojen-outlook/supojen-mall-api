-- =============================================================================
-- product_attributes：商品屬性關聯表
-- 用途：建立商品與屬性值之間的多對多關聯關係，定義商品擁有哪些非銷售屬性
-- 設計考量：用於商品描述性屬性（如材質、產地、風格等），不參與 SKU 規格組合
-- 注意：銷售屬性（用於 SKU 的顏色、尺寸等）應放在 skus.specs 欄位中
-- =============================================================================
CREATE TABLE product_attributes (
    -- 關聯欄位 (Association Fields)
    -- -------------------------------------------------------------------------
    product_id         INT NOT NULL,                        -- 商品 ID (Product ID)
    attribute_value_id INT NOT NULL,                        -- 屬性值 ID (Attribute value ID)

    -- 時間戳欄位 (Timestamp Fields)
    -- -------------------------------------------------------------------------
    created_at         TIMESTAMPTZ NOT NULL DEFAULT NOW(),  -- 關聯建立時間 (Association creation time)

    -- 約束條件 (Constraints)
    -- -------------------------------------------------------------------------

    -- 複合主鍵：確保同一商品不會重複關聯同一屬性值 (Composite primary key: prevents duplicate attribute value associations for a product)
    CONSTRAINT pk_product_attributes 
        PRIMARY KEY (product_id, attribute_value_id), 
    
    -- 外鍵約束：刪除商品時自動刪除關聯 (Foreign key: delete relations when product is deleted)
    CONSTRAINT fk_product_attributes_product 
        FOREIGN KEY (product_id) 
        REFERENCES products(id) 
        ON DELETE CASCADE,                             
    
    -- 外鍵約束：刪除屬性值時自動刪除關聯 (Foreign key: delete relations when attribute value is deleted)
    CONSTRAINT fk_product_attributes_attribute_value 
        FOREIGN KEY (attribute_value_id) 
        REFERENCES attribute_values(id) 
        ON DELETE CASCADE                             
);

-- =============================================================================
-- 索引建立 (Indexes)
-- 用途：優化各類常見查詢的效能
-- 說明：複合主鍵 (product_id, attribute_value_id) 已自動建立複合索引，
--       以下為額外建立的索引用於反向查詢
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 索引 1：商品查詢索引（必要）
-- 名稱：idx_product_attributes_product
-- 類型：B-tree
-- 欄位：product_id
-- 用途：加速查詢某個商品的所有屬性
-- 場景：商品詳情頁面顯示商品的描述性屬性（材質、產地等）
-- 範例：SELECT av.* 
--       FROM product_attributes pa
--       JOIN attribute_values av ON pa.attribute_value_id = av.id
--       WHERE pa.product_id = 10001
--       ORDER BY av.sort_order;
-- 說明：雖然複合主鍵已包含 product_id，但單獨的 product_id 索引可加速特定查詢
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_product_attributes_product 
    ON product_attributes (product_id);

-- -----------------------------------------------------------------------------
-- 索引 2：屬性值反向查詢索引（建議保留）
-- 名稱：idx_product_attributes_attribute_value
-- 類型：B-tree
-- 欄位：attribute_value_id
-- 用途：加速查詢某個屬性值被哪些商品使用
-- 場景：前台想找出所有「棉質」的商品、後台分析特定屬性的商品分佈
-- 範例：SELECT p.* 
--       FROM product_attributes pa
--       JOIN products p ON pa.product_id = p.id
--       WHERE pa.attribute_value_id = 200101  -- 棉質屬性值 ID
--         AND p.status = 'active';
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_product_attributes_attribute_value 
    ON product_attributes (attribute_value_id);

-- =============================================================================
-- 範例資料 (Sample Data)
-- 用途：對應 products 和 attribute_values 的範例，為商品添加描述性屬性
-- 說明：這裡只關聯非銷售屬性（for_sales = FALSE 的屬性值）
-- =============================================================================

/*
-- 對應 products 範例：
-- 10001: 青花瓷茶具組
-- 20001: 手工玻璃花器
-- 30001: Audi RS 電動遙控車

-- 對應 attribute_values 範例中的非銷售屬性：
-- 材質: 200101(棉), 200102(麻), 200103(絲), 200104(羊毛), 200105(聚酯纖維), 200106(尼龍)
-- 產地: 200201(中國), 200202(台灣), 200203(日本), 200204(韓國), 200205(越南), 200206(泰國), 200207(義大利)
-- 適用年齡: 200501(0-3歲), 200502(4-6歲), 200503(7-12歲), 200504(13-18歲), 200505(成人), 200506(銀髮族)
-- 季節: 200601(春季), 200602(夏季), 200603(秋季), 200604(冬季), 200605(四季通用)
*/

INSERT INTO product_attributes (product_id, attribute_value_id) VALUES
    -- 青花瓷茶具組 (10001) 的屬性
    (10001, 200101),  -- 材質：棉
    (10001, 200201),  -- 產地：中國
    (10001, 200505),  -- 適用年齡：成人
    (10001, 200605),  -- 季節：四季通用
    
    -- 手工玻璃花器 (20001) 的屬性
    (20001, 200106),  -- 材質：尼龍（包裝材質）
    (20001, 200207),  -- 產地：義大利
    (20001, 200505),  -- 適用年齡：成人
    (20001, 200605),  -- 季節：四季通用
    
    -- Audi RS 電動遙控車 (30001) 的屬性
    (30001, 200105),  -- 材質：聚酯纖維（車身）
    (30001, 200106),  -- 材質：尼龍（輪胎）
    (30001, 200201),  -- 產地：中國
    (30001, 200503),  -- 適用年齡：7-12歲
    (30001, 200504),  -- 適用年齡：13-18歲
    (30001, 200505),  -- 適用年齡：成人（收藏用）
    (30001, 200605);  -- 季節：四季通用

-- =============================================================================
-- 驗證範例 (Validation Queries)
-- =============================================================================

/*
-- 1. 查詢某個商品的所有屬性（依屬性鍵分組）
SELECT 
    ak.name AS attribute_name,
    av.value AS attribute_value,
    av.description
FROM product_attributes pa
JOIN attribute_values av ON pa.attribute_value_id = av.id
JOIN attribute_keys ak ON av.attribute_id = ak.id
WHERE pa.product_id = 30001  -- Audi RS 電動遙控車
ORDER BY ak.sort_order, av.sort_order;

-- 2. 查詢某個屬性值被哪些商品使用
SELECT 
    p.id,
    p.name AS product_name,
    p.price,
    p.status
FROM product_attributes pa
JOIN products p ON pa.product_id = p.id
WHERE pa.attribute_value_id = 200201  -- 產地：中國
  AND p.status = 'active';

-- 3. 統計每個產地的商品數量
SELECT 
    av.value AS origin,
    COUNT(pa.product_id) AS product_count
FROM attribute_values av
LEFT JOIN product_attributes pa ON av.id = pa.attribute_value_id
WHERE av.attribute_id = (SELECT id FROM attribute_keys WHERE code = 'origin')
GROUP BY av.id, av.value
ORDER BY product_count DESC;

-- 4. 找出符合多個屬性的商品（如：棉質 + 台灣製）
SELECT p.*
FROM products p
WHERE EXISTS (
    SELECT 1 FROM product_attributes pa1
    WHERE pa1.product_id = p.id
      AND pa1.attribute_value_id = 200101  -- 棉質
)
AND EXISTS (
    SELECT 1 FROM product_attributes pa2
    WHERE pa2.product_id = p.id
      AND pa2.attribute_value_id = 200202  -- 台灣
)
AND p.status = 'active';

-- 5. 統計每個商品有多少屬性
SELECT 
    p.id,
    p.name,
    COUNT(pa.attribute_value_id) AS attribute_count
FROM products p
LEFT JOIN product_attributes pa ON p.id = pa.product_id
GROUP BY p.id, p.name
ORDER BY attribute_count DESC;
*/

-- =============================================================================
-- 註解說明 (Comments)
-- =============================================================================

COMMENT ON TABLE product_attributes IS '商品屬性關聯表，定義商品擁有的非銷售屬性';
COMMENT ON COLUMN product_attributes.product_id IS '商品 ID，關聯到 products 表';
COMMENT ON COLUMN product_attributes.attribute_value_id IS '屬性值 ID，關聯到 attribute_values 表';
COMMENT ON COLUMN product_attributes.created_at IS '關聯建立時間';