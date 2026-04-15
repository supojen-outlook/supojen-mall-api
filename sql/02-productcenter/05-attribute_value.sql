-- =============================================================================
-- attribute_values：屬性值表
-- 用途：儲存屬性的具體可選值，如顏色屬性的"紅色"、"藍色"、"黑色"
-- 設計考量：與 attribute_keys 為多對一關係，每個屬性鍵可以有多個屬性值
-- 注意：同一屬性下不能有重複的值 (attribute_id + value 唯一)
-- =============================================================================
CREATE TABLE attribute_values (
    -- 基礎資訊欄位 (Basic Information)
    -- -------------------------------------------------------------------------
    id           INT NOT NULL,                          -- 屬性值唯一識別碼 (Unique attribute value ID)
    attribute_id INT NOT NULL,                           -- 所屬屬性鍵 ID (Parent attribute key ID)
    value        VARCHAR(100) NOT NULL,                  -- 屬性值內容，如：紅色、XL (Attribute value, e.g., red, XL)
    
    -- 輔助資訊欄位 (Auxiliary Information)
    -- -------------------------------------------------------------------------
    description  TEXT,                                    -- 屬性值詳細描述 (Value description)
    
    -- 管理控制欄位 (Management Control)
    -- -------------------------------------------------------------------------
    sort_order   INTEGER NOT NULL DEFAULT 0,              -- 排序順序，數字越小越前面 (Display order)
    
    -- 時間戳欄位 (Timestamp Fields)
    -- -------------------------------------------------------------------------
    created_at   TIMESTAMPTZ NOT NULL DEFAULT NOW(),      -- 屬性值建立時間 (Creation timestamp)

    -- 約束條件 (Constraints)
    -- -------------------------------------------------------------------------

    -- 主鍵約束：屬性值唯一識別碼 (Primary key: unique attribute value identifier)
    CONSTRAINT pk_attribute_values PRIMARY KEY (id),    

    -- 外鍵約束：當屬性鍵刪除時，其所有屬性值也一併刪除 (Foreign key: delete values when key is deleted)
    CONSTRAINT fk_attribute_values_attribute_key 
        FOREIGN KEY (attribute_id) 
        REFERENCES attribute_keys(id) 
        ON DELETE CASCADE,                               

    -- 唯一約束：同一屬性下不能有重複的值 (Unique: no duplicate values under the same attribute)  
    CONSTRAINT uk_attribute_id_value 
        UNIQUE (attribute_id, value)                     
);

-- =============================================================================
-- 索引建立 (Indexes)
-- 用途：優化各類常見查詢的效能
-- 說明：PostgreSQL 會自動為 PRIMARY KEY 和 UNIQUE 約束建立索引
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 索引 1：屬性 ID 查詢索引（建議保留）
-- 名稱：idx_attribute_values_attribute_id
-- 類型：B-tree
-- 欄位：attribute_id
-- 用途：加速查詢某個屬性下的所有值
-- 場景：前台顯示某個屬性的所有可選項時
-- 範例：SELECT * FROM attribute_values WHERE attribute_id = 1001 ORDER BY sort_order;
-- 說明：外鍵約束不會自動建立索引，建議手動建立以加速 JOIN 查詢
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_attribute_values_attribute_id 
    ON attribute_values (attribute_id);

-- -----------------------------------------------------------------------------
-- 索引 2：排序查詢索引（選擇性建立）
-- 名稱：idx_attribute_values_attribute_sort
-- 類型：B-tree
-- 欄位：(attribute_id, sort_order)
-- 用途：加速某個屬性下的值依序顯示
-- 場景：前台顯示屬性選項時需要按照 sort_order 排序
-- 範例：SELECT * FROM attribute_values 
--       WHERE attribute_id = 1001 ORDER BY sort_order;
-- 說明：如果這種查詢非常頻繁，可以用這個複合索引取代單一 attribute_id 索引
-- -----------------------------------------------------------------------------
-- CREATE INDEX IF NOT EXISTS idx_attribute_values_attribute_sort 
--     ON attribute_values (attribute_id, sort_order);

-- =============================================================================
-- 範例資料 (Sample Data)
-- 用途：對應 attribute_keys 的範例，提供各屬性的具體選項值
-- 說明：created_at 使用 DEFAULT NOW() 自動產生，可省略
-- =============================================================================

/*
-- 對應 attribute_keys 範例中的屬性：
-- 1001: 顏色 (color) - 銷售屬性
-- 1002: 尺寸 (size) - 銷售屬性
-- 1003: 容量 (capacity) - 銷售屬性
-- 2001: 材質 (material) - 非銷售屬性
-- 2002: 產地 (origin) - 非銷售屬性
-- 2005: 適用年齡 (age_range) - 非銷售屬性
-- 2006: 季節 (season) - 非銷售屬性
*/

INSERT INTO attribute_values (id, attribute_id, value, description, sort_order) VALUES
    -- 顏色屬性 (attribute_id = 1001) 的值
    (100101, 1001, '紅色', '熱情活力的紅色', 100),
    (100102, 1001, '藍色', '沉穩寧靜的藍色', 200),
    (100103, 1001, '黑色', '經典百搭的黑色', 300),
    (100104, 1001, '白色', '純淨簡約的白色', 400),
    (100105, 1001, '銀色', '科技時尚的銀色', 500),
    (100106, 1001, '金色', '奢華高貴的金色', 600),
    
    -- 尺寸屬性 (attribute_id = 1002) 的值
    (100201, 1002, 'XS', 'Extra Small 超小號', 100),
    (100202, 1002, 'S', 'Small 小號', 200),
    (100203, 1002, 'M', 'Medium 中號', 300),
    (100204, 1002, 'L', 'Large 大號', 400),
    (100205, 1002, 'XL', 'Extra Large 加大號', 500),
    (100206, 1002, 'XXL', 'Double Extra Large 雙加大號', 600),
    
    -- 容量屬性 (attribute_id = 1003) 的值
    (100301, 1003, '100ml', '100 毫升容量', 100),
    (100302, 1003, '200ml', '200 毫升容量', 200),
    (100303, 1003, '500ml', '500 毫升容量', 300),
    (100304, 1003, '1L', '1 公升容量', 400),
    (100305, 1003, '2L', '2 公升容量', 500),
    
    -- 材質屬性 (attribute_id = 2001) 的值
    (200101, 2001, '棉', '純棉材質，透氣舒適', 100),
    (200102, 2001, '麻', '亞麻材質，涼爽透氣', 200),
    (200103, 2001, '絲', '蠶絲材質，柔軟光滑', 300),
    (200104, 2001, '羊毛', '羊毛材質，保暖禦寒', 400),
    (200105, 2001, '聚酯纖維', '人造纖維，耐用抗皺', 500),
    (200106, 2001, '尼龍', '尼龍材質，耐磨耐用', 600),
    
    -- 產地屬性 (attribute_id = 2002) 的值
    (200201, 2002, '中國', '中國製造', 100),
    (200202, 2002, '台灣', '台灣製造', 200),
    (200203, 2002, '日本', '日本製造', 300),
    (200204, 2002, '韓國', '韓國製造', 400),
    (200205, 2002, '越南', '越南製造', 500),
    (200206, 2002, '泰國', '泰國製造', 600),
    (200207, 2002, '義大利', '義大利製造', 700),
    
    -- 適用年齡屬性 (attribute_id = 2005) 的值
    (200501, 2005, '0-3歲', '嬰幼兒階段', 100),
    (200502, 2005, '4-6歲', '學齡前兒童', 200),
    (200503, 2005, '7-12歲', '國小學童', 300),
    (200504, 2005, '13-18歲', '青少年', 400),
    (200505, 2005, '成人', '成年人', 500),
    (200506, 2005, '銀髮族', '老年人', 600),
    
    -- 季節屬性 (attribute_id = 2006) 的值
    (200601, 2006, '春季', '適合春季使用', 100),
    (200602, 2006, '夏季', '適合夏季使用', 200),
    (200603, 2006, '秋季', '適合秋季使用', 300),
    (200604, 2006, '冬季', '適合冬季使用', 400),
    (200605, 2006, '四季通用', '全年皆可使用', 500);

-- =============================================================================
-- 驗證範例 (Validation Queries)
-- =============================================================================

/*
-- 1. 查詢某個屬性的所有值（依序顯示）
SELECT v.*, k.name as attribute_name
FROM attribute_values v
JOIN attribute_keys k ON v.attribute_id = k.id
WHERE v.attribute_id = 1001
ORDER BY v.sort_order;

-- 2. 查詢所有銷售屬性的值
SELECT k.name as attribute_name, v.value, v.sort_order
FROM attribute_values v
JOIN attribute_keys k ON v.attribute_id = k.id
WHERE k.for_sales = TRUE
ORDER BY k.sort_order, v.sort_order;

-- 3. 統計每個屬性有多少個值
SELECT 
    k.name as attribute_name,
    k.code,
    COUNT(v.id) as value_count
FROM attribute_keys k
LEFT JOIN attribute_values v ON k.id = v.attribute_id
GROUP BY k.id, k.name, k.code
ORDER BY k.sort_order;

-- 4. 查詢特定值的詳細資訊
SELECT v.*, k.name as attribute_name
FROM attribute_values v
JOIN attribute_keys k ON v.attribute_id = k.id
WHERE v.value = '紅色' OR v.slug = 'red';
*/

-- =============================================================================
-- 註解說明 (Comments)
-- =============================================================================

COMMENT ON TABLE attribute_values IS '屬性值表，儲存屬性的具體可選值';
COMMENT ON COLUMN attribute_values.id IS '屬性值唯一識別碼，主鍵';
COMMENT ON COLUMN attribute_values.attribute_id IS '所屬屬性鍵 ID，關聯到 attribute_keys 表';
COMMENT ON COLUMN attribute_values.value IS '屬性值內容，如：紅色、XL';
COMMENT ON COLUMN attribute_values.slug IS 'URL 友好名稱，用於 SEO';
COMMENT ON COLUMN attribute_values.description IS '屬性值詳細描述';
COMMENT ON COLUMN attribute_values.sort_order IS '排序順序，數字越小越前面';
COMMENT ON COLUMN attribute_values.created_at IS '屬性值建立時間';