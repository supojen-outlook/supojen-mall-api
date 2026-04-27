-- =============================================================================
-- brands：品牌表
-- 用途：儲存商品的多層級品牌分類結構，支援無限層級的品牌層級關係
-- 設計考量：使用 parent_id 建立自我參照的樹狀結構，並透過觸發器自動維護
--          path_cache 和 level 以優化樹狀結構的查詢效能
-- 注意：本表資料量預估 < 1000 筆，索引設計以精簡為原則
-- =============================================================================
CREATE TABLE brands (
    -- 基礎資訊欄位 (Basic Information)
    -- -------------------------------------------------------------------------
    id          INT NOT NULL,                         -- 品牌唯一識別碼 (Unique brand ID)
    name        VARCHAR(100) NOT NULL,                 -- 品牌顯示名稱 (Display name of brand)
    slug        TEXT,                                   -- URL 友好名稱，用於 SEO 優化 (URL-friendly name for SEO)
    
    -- 層級結構欄位 (Hierarchical Structure)
    -- -------------------------------------------------------------------------
    parent_id   INTEGER,                                -- 上層品牌 ID，NULL 表示根品牌 (Parent brand ID, NULL for root)
    level       INTEGER DEFAULT 1,                      -- 所在層級：根品牌為 1，子品牌遞增 (Hierarchy level: 1 for root)
    is_leaf     BOOLEAN DEFAULT TRUE,                   -- 是否為葉節點（沒有子品牌）(Flag indicating if this is a leaf node)
    path_cache  INTEGER[],                               -- 從根到目前節點的所有 ID 陣列，如：'{1,5,8}' (Array of IDs from root to current node)
    path_text   VARCHAR(500),                            -- 從根到目前節點的路徑文字，如：'/精品/時尚/服飾' (Text path from root to current node)
    
    -- 品牌資訊欄位 (Brand Information)
    -- -------------------------------------------------------------------------
    logo_url    VARCHAR(500),                            -- 品牌標誌圖片網址 (Brand logo URL)
    description TEXT,                                     -- 品牌詳細描述 (Brand description)
    
    -- 管理控制欄位 (Management Control)
    -- -------------------------------------------------------------------------
    sort_order  INTEGER DEFAULT 0,                       -- 同層級間的排序順序，數字越小越前面 (Display order within same level)
    status      VARCHAR(20) NOT NULL DEFAULT 'active',   -- 狀態：active啟用/inactive停用 (Brand status)
    
    -- 時間戳欄位 (Timestamp Fields)
    -- -------------------------------------------------------------------------
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(), -- 品牌建立時間 (Brand creation time)

    -- 約束條件 (Constraints)
    -- -------------------------------------------------------------------------

    -- 主鍵約束 (Primary key constraint)
    CONSTRAINT pk_brands PRIMARY KEY (id),                
    
    -- 外鍵約束：刪除父品牌時，子品牌的 parent_id 設為 NULL
    CONSTRAINT fk_brands_parent FOREIGN KEY (parent_id) 
        REFERENCES brands(id) ON DELETE SET NULL,         
    
    -- 狀態只能為 active 或 inactive
    CONSTRAINT ck_brands_status CHECK 
        (status IN ('active', 'inactive')),              
    
    -- 層級必須大於等於 1 
    CONSTRAINT ck_brands_level CHECK 
        (level >= 1),                                    
    
    -- 排序值必須大於等於 0
    CONSTRAINT ck_brands_sort_order CHECK 
        (sort_order >= 0)                                 
);

-- =============================================================================
-- 索引建立 (Indexes)
-- 用途：優化各類常見查詢的效能，但考量本表資料量 < 1000 筆，只保留必要索引
-- 說明：PostgreSQL 會自動為 PRIMARY KEY 建立索引，以下為額外建立的索引
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 索引 1：父類別 ID 索引（必要）
-- 名稱：idx_brands_parent_id
-- 類型：B-tree
-- 欄位：parent_id
-- 用途：加速「查詢某個品牌的下一層子品牌」的操作
-- 場景：前台顯示品牌樹的展開、後台管理選擇父品牌時
-- 範例：SELECT * FROM brands WHERE parent_id = 5 ORDER BY sort_order;
--       查詢父品牌 ID 為 5 的所有直接子品牌
-- 說明：這是品牌表最頻繁的查詢，即使資料量小也建議保留
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_brands_parent_id 
    ON brands (parent_id);

-- -----------------------------------------------------------------------------
-- 索引 2：複合查詢索引（選擇性建立，依實際需求決定）
-- 名稱：idx_brands_parent_status_sort
-- 類型：B-tree
-- 欄位：(parent_id, status, sort_order)
-- 用途：加速前台顯示品牌樹的典型查詢
-- 場景：前台顯示某個父品牌下所有啟用的子品牌，並照順序排列
-- 範例：SELECT * FROM brands 
--       WHERE parent_id = 5 AND status = 'active' 
--       ORDER BY sort_order;
-- 說明：如果這種查詢非常頻繁（佔總查詢 50% 以上），才需要建立此索引
--       否則單靠 parent_id 索引就已足夠
-- -----------------------------------------------------------------------------
-- CREATE INDEX IF NOT EXISTS idx_brands_parent_status_sort 
--     ON brands (parent_id, status, sort_order);

-- =============================================================================
-- 觸發器 (Triggers)
-- 用途：自動維護層級相關欄位，確保資料一致性
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 觸發器函數：更新品牌層級資訊
-- 名稱：fn_update_brand_hierarchy
-- 用途：當新增或修改品牌時，自動計算並填入 level、path_cache、path_text
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_update_brand_hierarchy()
RETURNS TRIGGER AS $$
DECLARE
    parent_path_cache INTEGER[];
    parent_path_text VARCHAR(500);
    parent_level INTEGER;
BEGIN
    -- 情況 1：根品牌 (parent_id IS NULL)
    IF NEW.parent_id IS NULL THEN
        NEW.level := 1;
        NEW.path_cache := ARRAY[NEW.id];
        NEW.path_text := '/' || NEW.name;
    
    -- 情況 2：子品牌 (有 parent_id)
    ELSE
        -- 從父品牌繼承層級資訊
        SELECT 
            b.path_cache,
            b.path_text,
            b.level
        INTO 
            parent_path_cache,
            parent_path_text,
            parent_level
        FROM brands b
        WHERE b.id = NEW.parent_id;
        
        -- 如果父品牌存在，組合新的路徑資訊
        IF parent_path_cache IS NOT NULL THEN
            NEW.level := parent_level + 1;
            NEW.path_cache := parent_path_cache || NEW.id;
            NEW.path_text := parent_path_text || '/' || NEW.name;
        ELSE
            -- 預防措施：如果父品牌不存在（理論上不應發生）
            NEW.level := 1;
            NEW.path_cache := ARRAY[NEW.id];
            NEW.path_text := '/' || NEW.name;
        END IF;
    END IF;
    
    -- 暫時設定為葉節點（另一個觸發器會更新）
    NEW.is_leaf := TRUE;
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- -----------------------------------------------------------------------------
-- 觸發器：在新增或更新品牌時自動維護層級資訊
-- -----------------------------------------------------------------------------
CREATE TRIGGER trg_brands_before_insert_update
    BEFORE INSERT OR UPDATE OF parent_id ON brands
    FOR EACH ROW
    EXECUTE FUNCTION fn_update_brand_hierarchy();

-- -----------------------------------------------------------------------------
-- 觸發器函數：更新葉節點標記
-- 名稱：fn_update_brand_leaf_flag
-- 用途：當某個品牌有了子品牌時，自動將其 is_leaf 設為 FALSE
--       當刪除最後一個子品牌時，自動將父品牌 is_leaf 設為 TRUE
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_update_brand_leaf_flag()
RETURNS TRIGGER AS $$
BEGIN
    -- 當新增子品牌時，將父品牌標記為非葉節點
    IF TG_OP = 'INSERT' AND NEW.parent_id IS NOT NULL THEN
        UPDATE brands 
        SET is_leaf = FALSE 
        WHERE id = NEW.parent_id;
    END IF;
    
    -- 當刪除子品牌時，檢查父品牌是否還有其他子品牌
    IF TG_OP = 'DELETE' AND OLD.parent_id IS NOT NULL THEN
        IF NOT EXISTS (SELECT 1 FROM brands WHERE parent_id = OLD.parent_id) THEN
            UPDATE brands 
            SET is_leaf = TRUE 
            WHERE id = OLD.parent_id;
        END IF;
    END IF;
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- -----------------------------------------------------------------------------
-- 觸發器：在新增或刪除品牌時更新父品牌的葉節點標記
-- -----------------------------------------------------------------------------
CREATE TRIGGER trg_brands_after_insert_delete
    AFTER INSERT OR DELETE ON brands
    FOR EACH ROW
    EXECUTE FUNCTION fn_update_brand_leaf_flag();

-- =============================================================================
-- 範例資料 (Sample Data)
-- 用途：提供一組完整的品牌資料，展示如何正確使用此表
-- 說明：由於觸發器會自動產生 level、path_cache、path_text、is_leaf 等欄位，
--       因此 INSERT 時只需提供必要欄位即可
-- =============================================================================

/*
-- 根品牌：汽車集團及其子品牌
-- 注意：created_at 使用 DEFAULT NOW()，可省略不寫
--      level、path_cache、path_text、is_leaf 均由觸發器自動計算
*/
INSERT INTO brands (id, name, parent_id, sort_order, status, slug, description) VALUES
    -- 根品牌：德國汽車集團
    (1000, 'Volkswagen Group', NULL, 1000, 'active', 'volkswagen-group', '德國最大的汽車集團，擁有眾多知名品牌'),
    
    -- 第一層子品牌：VW 集團旗下主流品牌
    (1100, 'Volkswagen', 1000, 1100, 'active', 'volkswagen', '德國大眾汽車，國民車代表'),
    (1200, 'Audi', 1000, 1200, 'active', 'audi', '德國豪華汽車品牌，科技領先'),
    (1300, 'Porsche', 1000, 1300, 'active', 'porsche', '德國高性能跑車品牌'),
    (1400, 'Bentley', 1000, 1400, 'active', 'bentley', '英國頂級豪華房車品牌'),
    (1500, 'Lamborghini', 1000, 1500, 'active', 'lamborghini', '義大利超級跑車品牌'),
    
    -- 根品牌：日本汽車集團
    (2000, 'Toyota Group', NULL, 2000, 'active', 'toyota-group', '日本最大的汽車集團'),
    
    -- 第一層子品牌：Toyota 集團旗下品牌
    (2100, 'Toyota', 2000, 2100, 'active', 'toyota', '日本國民汽車品牌，以可靠耐用著稱'),
    (2200, 'Lexus', 2000, 2200, 'active', 'lexus', '日本豪華汽車品牌'),
    (2300, 'Daihatsu', 2000, 2300, 'active', 'daihatsu', '日本輕型車專家'),
    
    -- 根品牌：美國汽車集團
    (3000, 'General Motors', NULL, 3000, 'active', 'general-motors', '美國最大的汽車集團'),
    
    -- 第一層子品牌：GM 集團旗下品牌
    (3100, 'Chevrolet', 3000, 3100, 'active', 'chevrolet', '美國國民汽車品牌'),
    (3200, 'Cadillac', 3000, 3200, 'active', 'cadillac', '美國豪華汽車品牌'),
    (3300, 'GMC', 3000, 3300, 'active', 'gmc', '美國專業卡車與SUV品牌'),
    
    -- 第二層子品牌：Audi 的產品線
    (1210, 'Audi Sport', 1200, 1210, 'active', 'audi-sport', 'Audi 高性能部門'),
    (1220, 'Audi e-tron', 1200, 1220, 'active', 'audi-etron', 'Audi 電動車系列'),
    
    -- 第二層子品牌：Porsche 的產品線
    (1310, 'Porsche 911', 1300, 1310, 'active', 'porsche-911', 'Porsche 經典跑車系列'),
    (1320, 'Porsche Taycan', 1300, 1320, 'active', 'porsche-taycan', 'Porsche 電動跑車系列'),
    
    -- 第二層子品牌：Lexus 的產品線
    (2210, 'Lexus F Sport', 2200, 2210, 'active', 'lexus-f-sport', 'Lexus 性能運動系列'),
    (2220, 'Lexus Electrified', 2200, 2220, 'active', 'lexus-electrified', 'Lexus 電動化系列');

-- =============================================================================
-- 驗證範例 (Validation Queries)
-- =============================================================================

/*
-- 1. 查看所有品牌，確認觸發器自動填入的欄位
SELECT 
    id, 
    name, 
    level, 
    path_cache, 
    path_text, 
    is_leaf,
    created_at
FROM brands 
ORDER BY path_cache;

-- 預期結果：
-- 根品牌 (1000)：level=1, path_cache={1000}, path_text='/Volkswagen Group', is_leaf=FALSE
-- 子品牌 (1100)：level=2, path_cache={1000,1100}, path_text='/Volkswagen Group/Volkswagen', is_leaf=FALSE
-- 孫品牌 (1210)：level=3, path_cache={1000,1200,1210}, path_text='/Volkswagen Group/Audi/Audi Sport', is_leaf=TRUE

-- 2. 查詢某個品牌的所有子品牌（不限層級）
SELECT * FROM brands WHERE path_cache @> '{1200}' AND id != 1200;
-- 查詢 Audi 的所有子品牌（Audi Sport, Audi e-tron）

-- 3. 查詢某個品牌的下一層子品牌
SELECT * FROM brands WHERE parent_id = 1000 ORDER BY sort_order;
-- 查詢 Volkswagen Group 的直接子品牌

-- 4. 查詢所有葉節點品牌（最底層品牌）
SELECT * FROM brands WHERE is_leaf = TRUE;
-- 結果：Audi Sport, Audi e-tron, Porsche 911, Porsche Taycan, Lexus F Sport, Lexus Electrified 等

-- 5. 查詢某個層級的所有品牌
SELECT * FROM brands WHERE level = 2 ORDER BY sort_order;
-- 查詢所有第二層品牌（各集團的主要子品牌）

-- 6. 查詢特定國家的品牌（透過 path_text）
SELECT * FROM brands WHERE path_text LIKE '%德國%';
-- 查詢所有德國相關品牌

-- 7. 查詢電動車相關品牌
SELECT * FROM brands 
WHERE name ILIKE '%e-tron%' OR name ILIKE '%taycan%' OR name ILIKE '%electrified%';
*/

-- =============================================================================
-- 註解說明 (Comments)
-- =============================================================================

COMMENT ON TABLE brands IS '品牌表，支援多層級樹狀結構';
COMMENT ON COLUMN brands.id IS '品牌唯一識別碼，主鍵';
COMMENT ON COLUMN brands.name IS '品牌顯示名稱';
COMMENT ON COLUMN brands.slug IS 'URL 友好名稱，用於 SEO';
COMMENT ON COLUMN brands.parent_id IS '上層品牌 ID，NULL 表示根品牌';
COMMENT ON COLUMN brands.level IS '所在層級，根品牌為 1，由觸發器自動維護';
COMMENT ON COLUMN brands.is_leaf IS '是否為葉節點（沒有子品牌），由觸發器自動維護';
COMMENT ON COLUMN brands.path_cache IS '從根到目前節點的所有 ID 陣列，由觸發器自動維護';
COMMENT ON COLUMN brands.path_text IS '從根到目前節點的路徑文字，由觸發器自動維護';
COMMENT ON COLUMN brands.logo_url IS '品牌標誌圖片網址';
COMMENT ON COLUMN brands.description IS '品牌詳細描述';
COMMENT ON COLUMN brands.sort_order IS '同層級間的排序順序，數字越小越前面';
COMMENT ON COLUMN brands.status IS '狀態：active啟用，inactive停用';
COMMENT ON COLUMN brands.created_at IS '品牌建立時間，預設為當前時間';