-- =============================================================================
-- sku_attributes：SKU屬性值關聯表
-- 用途：建立SKU與屬性值之間的多對多關聯關係，定義SKU擁有哪些銷售屬性組合
-- 設計考量：用於SKU的規格組合（如顏色、尺寸的具體值），與 skus.specs JSON 欄位互補
-- 注意：此表確保資料正規化，skus.specs 可作為快取加速顯示
-- =============================================================================
CREATE TABLE sku_attributes (
    -- 關聯欄位 (Association Fields)
    -- -------------------------------------------------------------------------
    sku_id             INT NOT NULL,                        -- SKU ID (SKU ID)
    attribute_value_id INT NOT NULL,                        -- 屬性值 ID (Attribute value ID)

    -- 時間戳欄位 (Timestamp Fields)
    -- -------------------------------------------------------------------------
    created_at         TIMESTAMPTZ NOT NULL DEFAULT NOW(),  -- 關聯建立時間 (Association creation time)

    -- 約束條件 (Constraints)
    -- -------------------------------------------------------------------------

    -- 複合主鍵：確保同一SKU不會重複關聯同一屬性值 (Composite primary key: prevents duplicate attribute value associations for a SKU)
    CONSTRAINT pk_sku_attributes 
        PRIMARY KEY (sku_id, attribute_value_id),  
    
    -- 外鍵約束：刪除SKU時自動刪除關聯 (Foreign key: delete relations when SKU is deleted)
    CONSTRAINT fk_sku_attributes_sku 
        FOREIGN KEY (sku_id) 
        REFERENCES skus(id) 
        ON DELETE CASCADE,                         
    
    -- 外鍵約束：刪除屬性值時自動刪除關聯 (Foreign key: delete relations when attribute value is deleted)
    CONSTRAINT fk_sku_attributes_attribute_value 
        FOREIGN KEY (attribute_value_id) 
        REFERENCES attribute_values(id) 
        ON DELETE CASCADE                            
);

-- =============================================================================
-- 索引建立 (Indexes)
-- 用途：優化各類常見查詢的效能
-- 說明：複合主鍵 (sku_id, attribute_value_id) 已自動建立複合索引，
--       以下為額外建立的索引用於反向查詢
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 索引 1：SKU 查詢索引（必要）
-- 名稱：idx_sku_attributes_sku
-- 類型：B-tree
-- 欄位：sku_id
-- 用途：加速查詢某個 SKU 的所有屬性
-- 場景：SKU 詳情頁面顯示規格組合、確認 SKU 屬性是否正確
-- 範例：SELECT av.* 
--       FROM sku_attributes sa
--       JOIN attribute_values av ON sa.attribute_value_id = av.id
--       JOIN attribute_keys ak ON av.attribute_id = ak.id
--       WHERE sa.sku_id = 1001001
--       ORDER BY ak.sort_order, av.sort_order;
-- 說明：雖然複合主鍵已包含 sku_id，但單獨的 sku_id 索引可加速特定查詢
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_sku_attributes_sku 
    ON sku_attributes (sku_id);

-- -----------------------------------------------------------------------------
-- 索引 2：屬性值反向查詢索引（建議保留）
-- 名稱：idx_sku_attributes_attribute_value
-- 類型：B-tree
-- 欄位：attribute_value_id
-- 用途：加速查詢某個屬性值被哪些 SKU 使用
-- 場景：想知道某個顏色有多少 SKU、庫存統計、商品篩選
-- 範例：SELECT s.* 
--       FROM sku_attributes sa
--       JOIN skus s ON sa.sku_id = s.id
--       WHERE sa.attribute_value_id = 100101  -- 紅色屬性值 ID
--         AND s.status = 'active';
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_sku_attributes_attribute_value 
    ON sku_attributes (attribute_value_id);

-- -----------------------------------------------------------------------------
-- 索引 3：複合查詢索引（選擇性建立）
-- 名稱：idx_sku_attributes_composite
-- 類型：B-tree
-- 欄位：(attribute_value_id, sku_id)
-- 用途：加速特定屬性組合的查詢
-- 場景：找出同時有「紅色」和「XL」兩個屬性的 SKU
-- 範例：SELECT sa1.sku_id
--       FROM sku_attributes sa1
--       JOIN sku_attributes sa2 ON sa1.sku_id = sa2.sku_id
--       WHERE sa1.attribute_value_id = 100101  -- 紅色
--         AND sa2.attribute_value_id = 100202  -- XL
-- 說明：這種查詢較少見，且可用 EXISTS 替代，通常不需要此索引
-- -----------------------------------------------------------------------------
-- CREATE INDEX IF NOT EXISTS idx_sku_attributes_composite 
--     ON sku_attributes (attribute_value_id, sku_id);



-- =============================================================================
-- 註解說明 (Comments)
-- =============================================================================

COMMENT ON TABLE sku_attributes IS 'SKU 屬性值關聯表，定義 SKU 擁有的銷售屬性組合';
COMMENT ON COLUMN sku_attributes.sku_id IS 'SKU ID，關聯到 skus 表';
COMMENT ON COLUMN sku_attributes.attribute_value_id IS '屬性值 ID，關聯到 attribute_values 表';
COMMENT ON COLUMN sku_attributes.created_at IS '關聯建立時間';