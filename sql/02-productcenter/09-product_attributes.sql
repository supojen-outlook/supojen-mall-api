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
-- 註解說明 (Comments)
-- =============================================================================

COMMENT ON TABLE product_attributes IS '商品屬性關聯表，定義商品擁有的非銷售屬性';
COMMENT ON COLUMN product_attributes.product_id IS '商品 ID，關聯到 products 表';
COMMENT ON COLUMN product_attributes.attribute_value_id IS '屬性值 ID，關聯到 attribute_values 表';
COMMENT ON COLUMN product_attributes.created_at IS '關聯建立時間';