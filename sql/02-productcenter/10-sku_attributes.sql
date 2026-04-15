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
-- 範例資料 (Sample Data)
-- 用途：對應 skus 和 attribute_values 的範例，為每個 SKU 定義屬性組合
-- 說明：這裡關聯的通常是銷售屬性（for_sales = TRUE 的屬性值）
-- =============================================================================

/*
-- 對應 skus 範例：
-- 1001001: 青花瓷茶具組 - 藍色
-- 1001002: 青花瓷茶具組 - 白色
-- 2001001: 手工玻璃花器 - 透明
-- 2001002: 手工玻璃花器 - 藍色
-- 3001001: Audi RS 電動遙控車 - 銀色
-- 3001002: Audi RS 電動遙控車 - 黑色
-- 3001003: Audi RS 電動遙控車 - 紅色

-- 對應 attribute_values 範例中的銷售屬性：
-- 顏色: 100101(紅色), 100102(藍色), 100103(黑色), 100104(白色), 100105(銀色), 100106(金色)
-- 尺寸: 100201(XS), 100202(S), 100203(M), 100204(L), 100205(XL), 100206(XXL)
-- 容量: 100301(100ml), 100302(200ml), 100303(500ml), 100304(1L), 100305(2L)
*/

INSERT INTO sku_attributes (sku_id, attribute_value_id) VALUES
    -- 青花瓷茶具組 - 藍色 (1001001) 的屬性
    (1001001, 100102),  -- 顏色：藍色
    
    -- 青花瓷茶具組 - 白色 (1001002) 的屬性
    (1001002, 100104),  -- 顏色：白色
    
    -- 手工玻璃花器 - 透明 (2001001) 的屬性
    (2001001, 100104),  -- 顏色：白色（透明歸類為白色）
    
    -- 手工玻璃花器 - 藍色 (2001002) 的屬性
    (2001002, 100102),  -- 顏色：藍色
    
    -- 手工玻璃花器 - 綠色 (2001003) 雖然是 inactive，但仍有關聯
    (2001003, 100102),  -- 顏色：藍色（綠色沒有對應值，暫用藍色）
    
    -- Audi RS 電動遙控車 - 銀色 (3001001) 的屬性
    (3001001, 100105),  -- 顏色：銀色
    
    -- Audi RS 電動遙控車 - 黑色 (3001002) 的屬性
    (3001002, 100103),  -- 顏色：黑色
    
    -- Audi RS 電動遙控車 - 紅色 (3001003) 的屬性
    (3001003, 100101),  -- 顏色：紅色
    
    -- 假設有些 SKU 有多個屬性（如顏色 + 尺寸）
    -- 這裡示範如果茶具組有尺寸選項的話
    (1001001, 100203),  -- 藍色 + M 尺寸
    (1001002, 100204);  -- 白色 + L 尺寸

-- =============================================================================
-- 驗證範例 (Validation Queries)
-- =============================================================================

/*
-- 1. 查詢某個 SKU 的所有屬性
SELECT 
    ak.name AS attribute_name,
    av.value AS attribute_value,
    av.slug,
    ak.input_type
FROM sku_attributes sa
JOIN attribute_values av ON sa.attribute_value_id = av.id
JOIN attribute_keys ak ON av.attribute_id = ak.id
WHERE sa.sku_id = 1001001  -- 青花瓷茶具組 - 藍色
ORDER BY ak.sort_order, av.sort_order;

-- 2. 查詢某個屬性值被哪些 SKU 使用（包含商品資訊）
SELECT 
    av.value AS attribute_value,
    p.name AS product_name,
    s.sku_code,
    s.price,
    s.stock_quantity
FROM sku_attributes sa
JOIN attribute_values av ON sa.attribute_value_id = av.id
JOIN skus s ON sa.sku_id = s.id
JOIN products p ON s.product_id = p.id
WHERE sa.attribute_value_id = 100101  -- 紅色
  AND s.status = 'active';

-- 3. 找出所有「藍色」的 SKU
SELECT s.*
FROM sku_attributes sa
JOIN skus s ON sa.sku_id = s.id
WHERE sa.attribute_value_id = 100102  -- 藍色
  AND s.status = 'active';

-- 4. 統計每個顏色的 SKU 數量
SELECT 
    av.value AS color,
    COUNT(sa.sku_id) AS sku_count,
    SUM(s.stock_quantity) AS total_stock
FROM attribute_values av
LEFT JOIN sku_attributes sa ON av.id = sa.attribute_value_id
LEFT JOIN skus s ON sa.sku_id = s.id AND s.status = 'active'
WHERE av.attribute_id = (SELECT id FROM attribute_keys WHERE code = 'color')
GROUP BY av.id, av.value
ORDER BY sku_count DESC;

-- 5. 找出符合多個屬性的 SKU（如：藍色 + L 尺寸）
SELECT s.*
FROM skus s
WHERE EXISTS (
    SELECT 1 FROM sku_attributes sa1
    WHERE sa1.sku_id = s.id
      AND sa1.attribute_value_id = 100102  -- 藍色
)
AND EXISTS (
    SELECT 1 FROM sku_attributes sa2
    WHERE sa2.sku_id = s.id
      AND sa2.attribute_value_id = 100204  -- L 尺寸
)
AND s.status = 'active';

-- 6. 驗證 skus.specs JSON 與 sku_attributes 的一致性
SELECT 
    s.id,
    s.sku_code,
    s.specs AS json_specs,
    jsonb_agg(jsonb_build_object(
        'attribute', ak.code,
        'value', av.value
    )) AS table_specs
FROM skus s
LEFT JOIN sku_attributes sa ON s.id = sa.sku_id
LEFT JOIN attribute_values av ON sa.attribute_value_id = av.id
LEFT JOIN attribute_keys ak ON av.attribute_id = ak.id
WHERE s.id = 1001001
GROUP BY s.id, s.sku_code, s.specs;
*/

-- =============================================================================
-- 註解說明 (Comments)
-- =============================================================================

COMMENT ON TABLE sku_attributes IS 'SKU 屬性值關聯表，定義 SKU 擁有的銷售屬性組合';
COMMENT ON COLUMN sku_attributes.sku_id IS 'SKU ID，關聯到 skus 表';
COMMENT ON COLUMN sku_attributes.attribute_value_id IS '屬性值 ID，關聯到 attribute_values 表';
COMMENT ON COLUMN sku_attributes.created_at IS '關聯建立時間';