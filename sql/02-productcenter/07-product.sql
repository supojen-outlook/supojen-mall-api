-- =============================================================================
-- products：商品表
-- 用途：儲存商品 SPU (Standard Product Unit) 層級的核心資訊，包含基本資料、價格、分類、多媒體等
-- 設計考量：與 SKU 表為一對多關係，一個商品可以有多個規格組合
-- 注意：price 為基礎價格，實際售價可由 SKU 覆蓋；specs 使用 JSONB 存儲彈性規格參數
-- =============================================================================
CREATE TABLE products (
    -- 基礎資訊欄位 (Basic Information)
    -- -------------------------------------------------------------------------
    id             INT NOT NULL,                         -- 商品唯一識別碼 (Unique product ID)
    spu_code       VARCHAR(50) NOT NULL,                 -- SPU 編碼，用於商品識別 (Unique SPU code for product identification)
    name           VARCHAR(200) NOT NULL,                -- 商品顯示名稱 (Product display name)
    description    TEXT,                                 -- 商品詳細描述 (Product detailed description)
    
    -- 價格與分類資訊 (Price & Category Information)
    -- -------------------------------------------------------------------------
    price          DECIMAL(10,2) NOT NULL,               -- 商品基礎價格，SKU 價格可覆蓋 (Base price, can be overridden by SKUs)
    category_id    INT,                                  -- 所屬類別 ID (Category ID)
    brand_id       INT,                                  -- 所屬品牌 ID (Brand ID)
    
    -- 多媒體資訊 (Media Information)
    -- -------------------------------------------------------------------------
    main_image_url VARCHAR(500),                         -- 商品主圖網址 (Main product image URL)
    detail_images  TEXT[] DEFAULT '{}'::TEXT[],          -- 商品詳情圖陣列，如：'{"url1.jpg","url2.jpg"}' (Detail images array)
    video_url      VARCHAR(500),                         -- 商品介紹影片網址 (Product video URL)
    
    -- 規格與標籤資訊 (Specs & Tags Information)
    -- -------------------------------------------------------------------------
    specs          JSONB DEFAULT '{}'::JSONB,            -- 商品規格參數，如：{"weight":"1kg","material":"棉"} (Product specifications in JSON format)
    tags           TEXT[] DEFAULT '{}'::TEXT[],          -- 商品標籤，如：["新品","熱銷"] 
    
    -- 狀態與時間戳欄位 (Status & Timestamp Fields)
    -- -------------------------------------------------------------------------
    status         VARCHAR(20) NOT NULL DEFAULT 'draft', -- 商品狀態：draft草稿/active上架/inactive下架/pending審核中 (Product status)
    created_at     TIMESTAMPTZ NOT NULL DEFAULT NOW(),   -- 商品建立時間 (Product creation time)

    -- 約束條件 (Constraints)
    -- -------------------------------------------------------------------------

    -- 主鍵約束：商品唯一識別碼 (Primary key: unique product identifier)
    CONSTRAINT pk_products PRIMARY KEY (id),                

    -- 唯一約束：SPU 編碼不可重複 (Unique constraint: SPU code must be unique)
    CONSTRAINT uk_products_spu_code UNIQUE (spu_code),      
    
    -- 外鍵約束：關聯到類別表，刪除類別時設為 NULL (Foreign key: references categories, set NULL when category deleted)
    CONSTRAINT fk_products_category 
        FOREIGN KEY (category_id) 
        REFERENCES categories(id) 
        ON DELETE SET NULL,                                  
    
    -- 外鍵約束：關聯到品牌表，刪除品牌時設為 NULL (Foreign key: references brands, set NULL when brand deleted)
    CONSTRAINT fk_products_brand 
        FOREIGN KEY (brand_id) 
        REFERENCES brands(id) 
        ON DELETE SET NULL,                                  
    
    -- 檢查約束：狀態必須為指定值 (Check: status must be draft, pending, active, or inactive)
    CONSTRAINT ck_products_status CHECK 
        (status IN ('draft', 'pending', 'active', 'inactive')),  
    
    -- 檢查約束：價格必須大於等於 0 (Check: price must be >= 0)
    CONSTRAINT ck_products_price CHECK 
        (price >= 0)                                        
);

-- =============================================================================
-- 索引建立 (Indexes)
-- 用途：優化各類常見查詢的效能
-- 說明：PostgreSQL 會自動為 PRIMARY KEY 和 UNIQUE 約束建立索引，
--       以下為額外建立的索引
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 索引 1：標籤 GIN 索引
-- 名稱：idx_products_tags_gin
-- 類型：GIN (Generalized Inverted Index)
-- 欄位：tags (JSONB)
-- 用途：加速標籤查詢，如尋找所有「新品」或「熱銷」商品
-- 場景：前台依標籤篩選商品、後台行銷活動選品
-- 範例：SELECT * FROM products WHERE tags @> '["新品"]';
--       查詢所有包含「新品」標籤的商品
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_products_tags_gin 
    ON products USING GIN (tags);

-- -----------------------------------------------------------------------------
-- 索引 2：規格 GIN 索引（選擇性建立）
-- 名稱：idx_products_specs_gin
-- 類型：GIN
-- 欄位：specs (JSONB)
-- 用途：加速規格參數的查詢，如尋找特定規格的商品
-- 場景：前台篩選「重量=1kg」的商品
-- 範例：SELECT * FROM products WHERE specs @> '{"weight":"1kg"}';
-- 說明：如果經常需要依特定規格篩選商品，才建立此索引
-- -----------------------------------------------------------------------------
-- CREATE INDEX IF NOT EXISTS idx_products_specs_gin 
--     ON products USING GIN (specs);

-- -----------------------------------------------------------------------------
-- 索引 3：活躍商品索引（建議保留）
-- 名稱：idx_products_active
-- 類型：B-tree 部分索引
-- 欄位：(created_at)
-- 用途：加速前台顯示最新上架的商品
-- 場景：首頁顯示最新上架商品、商品列表頁依時間排序
-- 範例：SELECT * FROM products 
--       WHERE status = 'active' 
--       ORDER BY created_at DESC 
--       LIMIT 20;
-- 說明：部分索引只對活躍商品建立，節省空間且加速查詢
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_products_active 
    ON products (created_at DESC) 
    WHERE status = 'active';

-- -----------------------------------------------------------------------------
-- 索引 4：類別查詢索引（建議保留）
-- 名稱：idx_products_category
-- 類型：B-tree
-- 欄位：category_id
-- 用途：加速依類別篩選商品的查詢
-- 場景：商品列表頁選擇特定類別時
-- 範例：SELECT * FROM products 
--       WHERE category_id = 1001000 AND status = 'active';
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_products_category 
    ON products (category_id);

-- -----------------------------------------------------------------------------
-- 索引 5：品牌查詢索引（建議保留）
-- 名稱：idx_products_brand
-- 類型：B-tree
-- 欄位：brand_id
-- 用途：加速依品牌篩選商品的查詢
-- 場景：商品列表頁選擇特定品牌時
-- 範例：SELECT * FROM products 
--       WHERE brand_id = 1100 AND status = 'active';
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_products_brand 
    ON products (brand_id);

-- =============================================================================
-- 註解說明 (Comments)
-- =============================================================================

COMMENT ON TABLE products IS '商品 SPU 表，儲存商品核心資訊';
COMMENT ON COLUMN products.id IS '商品唯一識別碼，主鍵';
COMMENT ON COLUMN products.spu_code IS 'SPU 編碼，唯一識別商品';
COMMENT ON COLUMN products.name IS '商品顯示名稱';
COMMENT ON COLUMN products.description IS '商品詳細描述';
COMMENT ON COLUMN products.price IS '商品基礎價格，SKU 價格可覆蓋';
COMMENT ON COLUMN products.category_id IS '所屬類別 ID，關聯到 categories 表';
COMMENT ON COLUMN products.brand_id IS '所屬品牌 ID，關聯到 brands 表';
COMMENT ON COLUMN products.main_image_url IS '商品主圖網址';
COMMENT ON COLUMN products.detail_images IS '商品詳情圖陣列';
COMMENT ON COLUMN products.video_url IS '商品介紹影片網址';
COMMENT ON COLUMN products.specs IS '商品規格參數，JSON 格式';
COMMENT ON COLUMN products.tags IS '商品標籤，JSON 陣列格式';
COMMENT ON COLUMN products.status IS '商品狀態：draft草稿、pending審核中、active上架、inactive下架';
COMMENT ON COLUMN products.created_at IS '商品建立時間';