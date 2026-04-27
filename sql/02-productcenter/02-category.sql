-- =============================================================================
-- categories：產品類別表
-- 用途：儲存商品的多層級分類結構，支援無限層級的商品分類
-- 設計考量：使用 parent_id 建立自我參照的樹狀結構，並透過觸發器自動維護
--          path_cache 和 level 以優化樹狀結構的查詢效能
-- 注意：本表資料量預估 < 1000 筆，索引設計以精簡為原則
-- =============================================================================
CREATE TABLE categories (
    -- 基礎資訊欄位 (Basic Information)
    -- -------------------------------------------------------------------------
    id          INT NOT NULL,                           -- 類別唯一識別碼 (Unique category ID)
    name        VARCHAR(100) NOT NULL,                  -- 類別顯示名稱 (Display name of category)
    slug        TEXT,                                   -- URL 友好名稱，用於 SEO 優化 (URL-friendly name for SEO)
    
    -- 層級結構欄位 (Hierarchical Structure)
    -- -------------------------------------------------------------------------
    parent_id   INTEGER,                                -- 上層類別 ID，NULL 表示根類別 (Parent category ID, NULL for root)
    level       INTEGER DEFAULT 1,                      -- 所在層級：根類別為 1，子類別遞增 (Hierarchy level: 1 for root)
    is_leaf     BOOLEAN DEFAULT FALSE,                  -- 是否為葉節點（沒有子類別）(Flag indicating if this is a leaf node)
    path_cache  INTEGER[],                              -- 從根到目前節點的所有 ID 陣列，如：'{1,5,8}' (Array of IDs from root to current node)
    path_text   VARCHAR(500),                           -- 從根到目前節點的路徑文字，如：'/3C/手機/智慧型手機' (Text path from root to current node)
    
    -- 管理控制欄位 (Management Control)
    -- -------------------------------------------------------------------------
    sort_order  INTEGER DEFAULT 0,                      -- 同層級間的排序順序，數字越小越前面 (Display order within same level)
    status      VARCHAR(20) NOT NULL DEFAULT 'active',  -- 狀態：active啟用/inactive停用 (Category status)
    image_url   VARCHAR(500),                           -- 類別代表圖片網址 (Category representative image URL)
    
    -- 時間戳欄位 (Timestamp Fields)
    -- -------------------------------------------------------------------------
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),     -- 類別建立時間 (Category creation time)

    -- 約束條件 (Constraints)
    -- -------------------------------------------------------------------------

    -- 主鍵約束 (Primary key constraint)
    CONSTRAINT pk_categories PRIMARY KEY (id),            

    -- 外鍵約束：刪除父類別時，子類別的 parent_id 設為 NULL
    CONSTRAINT fk_categories_parent FOREIGN KEY (parent_id) 
        REFERENCES categories(id) ON DELETE SET NULL,     

    -- 狀態只能為 active 或 inactive
    CONSTRAINT ck_categories_status CHECK 
        (status IN ('active', 'inactive')),               

    -- 層級必須大於等於 1
    CONSTRAINT ck_categories_level CHECK 
        (level >= 1),                                      
    
    -- 排序值必須大於等於 0
    CONSTRAINT ck_categories_sort_order CHECK 
        (sort_order >= 0)                                  
);

-- =============================================================================
-- 索引建立 (Indexes)
-- 用途：優化各類常見查詢的效能，但考量本表資料量 < 1000 筆，只保留必要索引
-- 說明：PostgreSQL 會自動為 PRIMARY KEY 建立索引，以下為額外建立的索引
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 索引 1：父類別 ID 索引（必要）
-- 名稱：idx_categories_parent_id
-- 類型：B-tree
-- 欄位：parent_id
-- 用途：加速「查詢某個類別的下一層子類別」的操作
-- 場景：前台顯示類別樹的展開、後台管理選擇父類別時
-- 範例：SELECT * FROM categories WHERE parent_id = 5 ORDER BY sort_order;
--       查詢父類別 ID 為 5 的所有直接子類別
-- 說明：這是類別表最頻繁的查詢，即使資料量小也建議保留
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_categories_parent_id 
    ON categories (parent_id);

-- -----------------------------------------------------------------------------
-- 索引 2：複合查詢索引（選擇性建立，依實際需求決定）
-- 名稱：idx_categories_parent_status_sort
-- 類型：B-tree
-- 欄位：(parent_id, status, sort_order)
-- 用途：加速前台顯示類別樹的典型查詢
-- 場景：前台顯示某個父類別下所有啟用的子類別，並照順序排列
-- 範例：SELECT * FROM categories 
--       WHERE parent_id = 5 AND status = 'active' 
--       ORDER BY sort_order;
-- 說明：如果這種查詢非常頻繁（佔總查詢 50% 以上），才需要建立此索引
--       否則單靠 parent_id 索引就已足夠
-- -----------------------------------------------------------------------------
-- CREATE INDEX IF NOT EXISTS idx_categories_parent_status_sort 
--     ON categories (parent_id, status, sort_order);

-- =============================================================================
-- 觸發器 (Triggers)
-- 用途：自動維護層級相關欄位，確保資料一致性
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 觸發器函數：更新類別層級資訊
-- 名稱：fn_update_category_hierarchy
-- 用途：當新增或修改類別時，自動計算並填入 level、path_cache、path_text
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_update_category_hierarchy()
RETURNS TRIGGER AS $$
DECLARE
    parent_path_cache INTEGER[];
    parent_path_text VARCHAR(500);
    parent_level INTEGER;
BEGIN
    -- 情況 1：根類別 (parent_id IS NULL)
    IF NEW.parent_id IS NULL THEN
        NEW.level := 1;
        NEW.path_cache := ARRAY[NEW.id];
        NEW.path_text := '/' || NEW.name;
    
    -- 情況 2：子類別 (有 parent_id)
    ELSE
        -- 從父類別繼承層級資訊
        SELECT 
            c.path_cache,
            c.path_text,
            c.level
        INTO 
            parent_path_cache,
            parent_path_text,
            parent_level
        FROM categories c
        WHERE c.id = NEW.parent_id;
        
        -- 如果父類別存在，組合新的路徑資訊
        IF parent_path_cache IS NOT NULL THEN
            NEW.level := parent_level + 1;
            NEW.path_cache := parent_path_cache || NEW.id;
            NEW.path_text := parent_path_text || '/' || NEW.name;
        ELSE
            -- 預防措施：如果父類別不存在（理論上不應發生）
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
-- 觸發器：在新增或更新類別時自動維護層級資訊
-- -----------------------------------------------------------------------------
CREATE TRIGGER trg_categories_before_insert_update
    BEFORE INSERT OR UPDATE OF parent_id ON categories
    FOR EACH ROW
    EXECUTE FUNCTION fn_update_category_hierarchy();

-- -----------------------------------------------------------------------------
-- 觸發器函數：更新葉節點標記
-- 名稱：fn_update_leaf_flag
-- 用途：當某個類別有了子類別時，自動將其 is_leaf 設為 FALSE
--       當刪除最後一個子類別時，自動將父類別 is_leaf 設為 TRUE
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_update_leaf_flag()
RETURNS TRIGGER AS $$
BEGIN
    -- 當新增子類別時，將父類別標記為非葉節點
    IF TG_OP = 'INSERT' AND NEW.parent_id IS NOT NULL THEN
        UPDATE categories 
        SET is_leaf = FALSE 
        WHERE id = NEW.parent_id;
    END IF;
    
    -- 當刪除子類別時，檢查父類別是否還有其他子類別
    IF TG_OP = 'DELETE' AND OLD.parent_id IS NOT NULL THEN
        IF NOT EXISTS (SELECT 1 FROM categories WHERE parent_id = OLD.parent_id) THEN
            UPDATE categories 
            SET is_leaf = TRUE 
            WHERE id = OLD.parent_id;
        END IF;
    END IF;
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- -----------------------------------------------------------------------------
-- 觸發器：在新增或刪除類別時更新父類別的葉節點標記
-- -----------------------------------------------------------------------------
CREATE TRIGGER trg_categories_after_insert_delete
    AFTER INSERT OR DELETE ON categories
    FOR EACH ROW
    EXECUTE FUNCTION fn_update_leaf_flag();


-- =============================================================================
-- 範例資料 (Sample Data)
-- 用途：提供一組完整的類別資料，展示如何正確使用此表
-- 說明：由於觸發器會自動產生 level、path_cache、path_text、is_leaf 等欄位，
--       因此 INSERT 時只需提供必要欄位即可
-- =============================================================================

INSERT INTO categories (id, name, parent_id, sort_order, status, slug) VALUES
    (1000000, '工藝品', NULL, 1000000, 'active', 'crafts'),
    (1001000, '陶瓷工藝品', 1000000, 1001000, 'active', 'ceramic_crafts'),
    (1002000, '玻璃工藝品', 1000000, 1002000, 'active', 'glass_crafts'),
    (1003000, '金屬工藝品', 1000000, 1003000, 'active', 'metal_crafts'),
    (1004000, '木製工藝品', 1000000, 1004000, 'active', 'wood_crafts'),
    (1005000, '竹製工藝品', 1000000, 1005000, 'active', 'bamboo_crafts'),
    (1006000, '藤編工藝品', 1000000, 1006000, 'active', 'rattan_crafts'),
    (1007000, '纖維紡織工藝品', 1000000, 1007000, 'active', 'fiber_crafts'),
    (1008000, '石料/礦石工藝品', 1000000, 1008000, 'active', 'stone_crafts'),
    (1009000, '樹脂/複合材料工藝品', 1000000, 1009000, 'active', 'resin_crafts'),
    (1010000, '紙藝與繪畫', 1000000, 1010000, 'active', 'paper_crafts'),
    (1011000, '混合材質工藝品', 1000000, 1011000, 'active', 'mixed_material_crafts');

-- =============================================================================
-- 使用範例 (Usage Examples)
-- =============================================================================

/*

-- 1. 新增根類別
INSERT INTO categories (id, name) VALUES (1, '3C');
-- 結果：level=1, path_cache={1}, path_text='/3C'

-- 2. 新增子類別
INSERT INTO categories (id, name, parent_id) VALUES (2, '手機', 1);
-- 結果：level=2, path_cache={1,2}, path_text='/3C/手機'

-- 3. 新增孫類別
INSERT INTO categories (id, name, parent_id) VALUES (3, '智慧型手機', 2);
-- 結果：level=3, path_cache={1,2,3}, path_text='/3C/手機/智慧型手機'

-- 4. 查詢某個類別的所有子類別（不限層級）
SELECT * FROM categories WHERE path_cache @> '{2}';

-- 5. 查詢某個類別的直接子類別（限下一層）
SELECT * FROM categories WHERE parent_id = 2 ORDER BY sort_order;

-- 6. 查詢所有根類別（第一層）
SELECT * FROM categories WHERE level = 1 AND status = 'active';

-- 7. 移動類別到不同父類別
UPDATE categories SET parent_id = 1 WHERE id = 3;
-- 觸發器會自動重新計算 path_cache 和 path_text

*/

-- =============================================================================
-- 註解說明 (Comments)
-- =============================================================================

COMMENT ON TABLE categories IS '產品類別表，支援多層級樹狀結構';
COMMENT ON COLUMN categories.id IS '類別唯一識別碼，主鍵';
COMMENT ON COLUMN categories.name IS '類別顯示名稱';
COMMENT ON COLUMN categories.slug IS 'URL 友好名稱，用於 SEO';
COMMENT ON COLUMN categories.parent_id IS '上層類別 ID，NULL 表示根類別';
COMMENT ON COLUMN categories.level IS '所在層級，根類別為 1，由觸發器自動維護';
COMMENT ON COLUMN categories.is_leaf IS '是否為葉節點（沒有子類別），由觸發器自動維護';
COMMENT ON COLUMN categories.path_cache IS '從根到目前節點的所有 ID 陣列，由觸發器自動維護';
COMMENT ON COLUMN categories.path_text IS '從根到目前節點的路徑文字，由觸發器自動維護';
COMMENT ON COLUMN categories.sort_order IS '同層級間的排序順序，數字越小越前面';
COMMENT ON COLUMN categories.status IS '狀態：active啟用，inactive停用';
COMMENT ON COLUMN categories.image_url IS '類別代表圖片網址';
COMMENT ON COLUMN categories.created_at IS '類別建立時間，預設為當前時間';