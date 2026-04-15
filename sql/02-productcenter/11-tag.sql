-- =============================================================================
-- tags：標籤表
-- 用途：儲存商品標籤，用於商品分類、行銷活動、搜尋篩選等
-- 設計考量：標籤為扁平化結構，不支援層級，用於靈活的商品標記
-- 注意：與商品為多對多關係，透過 product_tags 關聯表連接
-- =============================================================================
CREATE TABLE tags (
    -- 基礎資訊欄位 (Basic Information)
    -- -------------------------------------------------------------------------
    id          INT NOT NULL,                             -- 標籤唯一識別碼 (Unique tag ID)
    name        VARCHAR(100) NOT NULL,                    -- 標籤名稱，如：新品、熱銷、限時優惠 (Tag name)
    description TEXT,                                     -- 標籤詳細描述 (Tag description)
    
    -- 管理控制欄位 (Management Control)
    -- -------------------------------------------------------------------------
    sort_order  INT NOT NULL DEFAULT 0,                   -- 排序順序，數字越小越前面 (Display order)
    
    -- 時間戳欄位 (Timestamp Fields)
    -- -------------------------------------------------------------------------
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),       -- 標籤建立時間 (Tag creation time)

    -- 約束條件 (Constraints)
    -- -------------------------------------------------------------------------

    -- 主鍵約束：標籤唯一識別碼 (Primary key: unique tag identifier)
    CONSTRAINT pk_tags PRIMARY KEY (id),                  

    -- 唯一約束：標籤名稱不可重複 (Unique constraint: tag name must be unique)
    CONSTRAINT uk_tags_name UNIQUE (name),                 

    -- 檢查約束：排序值必須大於等於 0 (Check: sort order must be >= 0)
    CONSTRAINT ck_tags_sort_order CHECK 
        (sort_order >= 0)                                  
);

-- =============================================================================
-- 索引建立 (Indexes)
-- 用途：優化各類常見查詢的效能
-- 說明：PostgreSQL 會自動為 PRIMARY KEY 和 UNIQUE 約束建立索引，
--       以下為額外建立的索引
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 索引 1：排序查詢索引（建議保留）
-- 名稱：idx_tags_sort_order
-- 類型：B-tree
-- 欄位：sort_order
-- 用途：加速依排序順序顯示標籤
-- 場景：前台顯示標籤雲、後台管理標籤列表
-- 範例：SELECT * FROM tags ORDER BY sort_order, name;
-- 說明：如果標籤數量較多且經常依序顯示，可建立此索引
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_tags_sort_order 
    ON tags (sort_order);

-- =============================================================================
-- 範例資料 (Sample Data)
-- 用途：提供一組常用的商品標籤
-- 說明：created_at 使用 DEFAULT NOW() 自動產生，可省略
-- =============================================================================

/*
-- 常見的標籤類型：
-- 行銷標籤：新品、熱銷、限時優惠、限量
-- 屬性標籤：手工製作、環保材質、台灣製
-- 活動標籤：聖誕節、母親節、雙11
*/

INSERT INTO tags (id, name, description, sort_order) VALUES
    -- 行銷標籤 (Marketing Tags)
    (1, '新品', '最新上架商品', 100),
    (2, '熱銷', '暢銷熱賣商品', 200),
    (3, '限時優惠', '限時特價商品', 300),
    (4, '限量', '限量發售商品', 400),
    (5, '獨家', '平台獨家販售', 500),
    
    -- 屬性標籤 (Attribute Tags)
    (6, '手工製作', '職人手作商品', 600),
    (7, '環保材質', '使用環保材料', 700),
    (8, '台灣製', '台灣製造商品', 800),
    (9, '日本進口', '日本進口商品', 900),
    (10, '職人推薦', '職人精選推薦', 1000),
    
    -- 活動標籤 (Event Tags)
    (11, '聖誕節', '聖誕節限定商品', 1100),
    (12, '母親節', '母親節推薦商品', 1200),
    (13, '雙11', '雙11購物節商品', 1300),
    (14, '年貨', '年貨節商品', 1400),
    (15, '情人節', '情人節禮盒', 1500),
    
    -- 品質標籤 (Quality Tags)
    (16, '精品', '精品等級商品', 1600),
    (17, '入門', '入門款商品', 1700),
    (18, '專業級', '專業人士適用', 1800),
    (19, '禮盒', '禮盒包裝商品', 1900),
    (20, '客製化', '可客製化商品', 2000);

-- =============================================================================
-- 驗證範例 (Validation Queries)
-- =============================================================================

/*
-- 1. 查詢所有標籤，依序顯示
SELECT * FROM tags 
ORDER BY sort_order, name;

-- 2. 查詢特定類型的標籤（依名稱模糊搜尋）
SELECT * FROM tags 
WHERE name LIKE '%節%'  -- 查詢節日相關標籤
ORDER BY sort_order;

-- 3. 統計標籤使用次數（需搭配 product_tags 表）
SELECT 
    t.id,
    t.name,
    COUNT(pt.product_id) AS usage_count
FROM tags t
LEFT JOIN product_tags pt ON t.id = pt.tag_id
GROUP BY t.id, t.name
ORDER BY usage_count DESC, t.sort_order;
*/

-- =============================================================================
-- 註解說明 (Comments)
-- =============================================================================

COMMENT ON TABLE tags IS '標籤表，用於商品標記和分類';
COMMENT ON COLUMN tags.id IS '標籤唯一識別碼，主鍵';
COMMENT ON COLUMN tags.name IS '標籤名稱，如：新品、熱銷';
COMMENT ON COLUMN tags.description IS '標籤詳細描述';
COMMENT ON COLUMN tags.sort_order IS '排序順序，數字越小越前面';
COMMENT ON COLUMN tags.created_at IS '標籤建立時間';