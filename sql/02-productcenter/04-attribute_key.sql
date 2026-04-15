-- =============================================================================
-- attribute_keys：屬性鍵表
-- 用途：定義產品的各種屬性，如顏色、尺寸、材質等，用於商品規格管理
-- 設計考量：支援銷售屬性（用於SKU生成）與非銷售屬性（用於商品描述），
--          並提供 input_type 控制前端顯示元件
-- 注意：屬性值會儲存在 attribute_values 表中，與此表為一對多關係
-- =============================================================================
CREATE TABLE attribute_keys (
    -- 基礎資訊欄位 (Basic Information)
    -- -------------------------------------------------------------------------
    id          INT NOT NULL,                            -- 屬性唯一識別碼 (Unique attribute key ID)
    name        VARCHAR(100) NOT NULL,                   -- 屬性顯示名稱，如：顏色、尺寸 (Display name, e.g., color, size)
    description TEXT,                                    -- 屬性詳細描述 (Attribute description)
    
    -- 屬性特性欄位 (Attribute Characteristics)
    -- -------------------------------------------------------------------------
    for_sales   BOOLEAN NOT NULL DEFAULT FALSE,          -- 是否為銷售屬性：TRUE用於SKU生成/FALSE僅為描述 (Sales attribute flag for SKU)
    input_type  VARCHAR(20) NOT NULL DEFAULT 'select',   -- 前端輸入類型：select下拉選單/text文字/number數字/checkbox複選框 (Frontend input type)
    is_required BOOLEAN NOT NULL DEFAULT FALSE,          -- 是否為必填屬性 (Whether required)
    unit        VARCHAR(30),                             -- 單位，如：cm(公分)、g(公克) (Unit of measurement)
    
    -- 管理控制欄位 (Management Control)
    -- -------------------------------------------------------------------------
    sort_order  INTEGER NOT NULL DEFAULT 0,              -- 同層級間的排序順序，數字越小越前面 (Display order)
    status      VARCHAR(20) NOT NULL DEFAULT 'active',   -- 狀態：active啟用/inactive停用 (Attribute status)
    
    -- 時間戳欄位 (Timestamp Fields)
    -- -------------------------------------------------------------------------
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),      -- 屬性建立時間 (Creation timestamp)

    -- 約束條件 (Constraints)
    -- -------------------------------------------------------------------------
    
    -- 主鍵約束 (Primary key constraint)
    CONSTRAINT pk_attribute_keys PRIMARY KEY (id),
    
    -- 檢查約束 (Check constraints)

    -- 輸入類型限制
    CONSTRAINT ck_attribute_keys_input_type CHECK 
        (input_type IN ('select', 'text', 'number', 'checkbox')),  

    -- 狀態只能為 active 或 inactive
    CONSTRAINT ck_attribute_keys_status CHECK 
        (status IN ('active', 'inactive')),

    -- 排序值必須大於等於 0
    CONSTRAINT ck_attribute_keys_sort_order CHECK 
        (sort_order >= 0)
);

-- =============================================================================
-- 索引建立 (Indexes)
-- 用途：優化各類常見查詢的效能
-- 說明：PostgreSQL 會自動為 PRIMARY KEY 和 UNIQUE 約束建立 B-tree 索引
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 索引 1：銷售屬性查詢索引（選擇性建立）
-- 名稱：idx_attribute_keys_for_sales
-- 類型：B-tree
-- 欄位：for_sales
-- 用途：加速過濾銷售屬性或非銷售屬性的查詢
-- 場景：生成SKU時只需要查詢 for_sales = TRUE 的屬性
-- 範例：SELECT * FROM attribute_keys WHERE for_sales = TRUE ORDER BY sort_order;
-- 說明：如果屬性數量很多且經常這樣查詢，才需要建立此索引
-- -----------------------------------------------------------------------------
-- CREATE INDEX IF NOT EXISTS idx_attribute_keys_for_sales 
--     ON attribute_keys (for_sales);

-- -----------------------------------------------------------------------------
-- 索引 2：狀態查詢索引（選擇性建立）
-- 名稱：idx_attribute_keys_status
-- 類型：B-tree
-- 欄位：status
-- 用途：加速過濾啟用/停用屬性的查詢
-- 場景：前台只顯示啟用的屬性，後台管理可能需要看停用的
-- 範例：SELECT * FROM attribute_keys WHERE status = 'active' ORDER BY sort_order;
-- 說明：如果屬性數量很多且經常這樣查詢，才需要建立此索引
-- -----------------------------------------------------------------------------
-- CREATE INDEX IF NOT EXISTS idx_attribute_keys_status 
--     ON attribute_keys (status);

-- -----------------------------------------------------------------------------
-- 索引 3：複合查詢索引（選擇性建立）
-- 名稱：idx_attribute_keys_sales_status_sort
-- 類型：B-tree
-- 欄位：(for_sales, status, sort_order)
-- 用途：加速最常用的前台顯示查詢
-- 場景：前台顯示所有啟用的銷售屬性，並照順序排列
-- 範例：SELECT * FROM attribute_keys 
--       WHERE for_sales = TRUE AND status = 'active' 
--       ORDER BY sort_order;
-- 說明：如果這種查詢非常頻繁，才需要建立此索引
-- -----------------------------------------------------------------------------
-- CREATE INDEX IF NOT EXISTS idx_attribute_keys_sales_status_sort 
--     ON attribute_keys (for_sales, status, sort_order);

-- =============================================================================
-- 範例資料 (Sample Data)
-- 用途：提供一組完整的屬性鍵資料，展示常見的屬性類型
-- 說明：created_at 使用 DEFAULT NOW() 自動產生，可省略
-- =============================================================================

/*
-- 銷售屬性（用於SKU生成）
*/
INSERT INTO attribute_keys (id, name, description, for_sales, input_type, is_required, unit, sort_order, status) VALUES
    -- 顏色相關
    (1001, '顏色', '商品的顏色選項', TRUE, 'select', TRUE, NULL, 1000, 'active'),
    (1002, '尺寸', '商品的尺寸規格', TRUE, 'select', TRUE, 'cm', 1100, 'active'),
    (1003, '容量', '商品的容量規格', TRUE, 'select', TRUE, 'ml', 1200, 'active'),
    (1004, '重量', '商品的重量', TRUE, 'number', FALSE, 'g', 1300, 'active'),
    
    -- 非銷售屬性（用於商品描述）
    (2001, '材質', '商品的材質成分', FALSE, 'select', FALSE, NULL, 2000, 'active'),
    (2002, '產地', '商品的製造地點', FALSE, 'select', FALSE, NULL, 2100, 'active'),
    (2003, '品牌', '商品的品牌名稱', FALSE, 'text', FALSE, NULL, 2200, 'active'),
    (2004, '保固期限', '商品的保固時間', FALSE, 'number', FALSE, 'month', 2300, 'active'),
    (2005, '適用年齡', '建議的使用年齡層', FALSE, 'select', FALSE, NULL, 2400, 'active'),
    (2006, '季節', '適用的季節', FALSE, 'select', FALSE, NULL, 2500, 'active'),
    
    -- 特殊輸入類型範例
    (3001, '規格備註', '額外的規格說明', FALSE, 'text', FALSE, NULL, 3000, 'active'),
    (3002, '是否現貨', '商品是否有現貨', FALSE, 'checkbox', FALSE, NULL, 3100, 'active'),
    (3003, '庫存單位', '庫存計算單位', FALSE, 'select', TRUE, NULL, 3200, 'active');

-- =============================================================================
-- 驗證範例 (Validation Queries)
-- =============================================================================

/*
-- 1. 查詢所有銷售屬性（用於SKU生成）
SELECT * FROM attribute_keys 
WHERE for_sales = TRUE 
ORDER BY sort_order;

-- 2. 查詢所有啟用的屬性
SELECT * FROM attribute_keys 
WHERE status = 'active' 
ORDER BY for_sales DESC, sort_order;

-- 3. 依輸入類型分組統計
SELECT 
    input_type,
    COUNT(*) as count,
    ARRAY_AGG(name) as examples
FROM attribute_keys 
GROUP BY input_type 
ORDER BY input_type;

-- 4. 查詢特定類型的屬性
SELECT * FROM attribute_keys 
WHERE input_type = 'select' AND status = 'active'
ORDER BY sort_order;

-- 5. 查詢必填屬性
SELECT * FROM attribute_keys 
WHERE is_required = TRUE 
ORDER BY for_sales DESC, sort_order;
*/

-- =============================================================================
-- 註解說明 (Comments)
-- =============================================================================

COMMENT ON TABLE attribute_keys IS '屬性鍵表，定義商品的所有可配置屬性';
COMMENT ON COLUMN attribute_keys.id IS '屬性唯一識別碼，主鍵';
COMMENT ON COLUMN attribute_keys.name IS '屬性顯示名稱，如：顏色、尺寸';
COMMENT ON COLUMN attribute_keys.description IS '屬性詳細描述，說明屬性的用途';
COMMENT ON COLUMN attribute_keys.for_sales IS '是否為銷售屬性：true用於SKU生成，false僅為商品描述';
COMMENT ON COLUMN attribute_keys.input_type IS '前端輸入類型：select(下拉選單)、text(文字)、number(數字)、checkbox(複選框)';
COMMENT ON COLUMN attribute_keys.is_required IS '是否為必填屬性';
COMMENT ON COLUMN attribute_keys.unit IS '單位，如：cm(公分)、g(公克)、ml(毫升)';
COMMENT ON COLUMN attribute_keys.sort_order IS '排序順序，數字越小越前面';
COMMENT ON COLUMN attribute_keys.status IS '狀態：active啟用，inactive停用';
COMMENT ON COLUMN attribute_keys.created_at IS '屬性建立時間';