-- =============================================================================
-- putaway_profiles：商品上架策略設定表
-- 用途：定義每個 SKU 的預設儲位，簡化版只做「同一個 SKU 放一起」
-- 設計考量：直接為 SKU 指定首選儲位，系統上架時優先分配到該儲位
-- =============================================================================
CREATE TABLE putaway_profiles (
    -- 關聯欄位 (Association Fields)
    -- -------------------------------------------------------------------------
    id               INT NOT NULL,                       -- 設定唯一識別碼 (Unique profile ID)
    sku_id           INT NOT NULL,                       -- SKU ID (SKU ID)
    preferred_bin_id INT,                                -- 首選儲位 ID (Preferred storage bin ID)

    -- 時間戳欄位 (Timestamp Fields)
    -- -------------------------------------------------------------------------
    created_at       TIMESTAMPTZ NOT NULL DEFAULT NOW(), -- 設定建立時間 (Profile creation time)

    -- 約束條件 (Constraints)
    -- -------------------------------------------------------------------------

    -- 主鍵約束：設定唯一識別碼 (Primary key: unique profile identifier)
    CONSTRAINT pk_putaway_profiles PRIMARY KEY (id),
    
    -- 唯一約束：一個 SKU 只能有一個設定 (Unique: one SKU, one profile)
    CONSTRAINT uk_putaway_profiles_sku 
        UNIQUE (sku_id),                                      
    
    -- 外鍵約束：刪除 SKU 時自動刪除設定 (Foreign key: delete profile when SKU is deleted)
    CONSTRAINT fk_putaway_profiles_sku 
        FOREIGN KEY (sku_id) 
        REFERENCES skus(id) 
        ON DELETE CASCADE,                                     
    
    -- 外鍵約束：刪除儲位時設為 NULL (Foreign key: set NULL when bin is deleted)
    CONSTRAINT fk_putaway_profiles_bin 
        FOREIGN KEY (preferred_bin_id) 
        REFERENCES locations(id) 
        ON DELETE SET NULL                                    
);

-- =============================================================================
-- 索引建立 (Indexes)
-- 用途：優化各類常見查詢的效能
-- 說明：資料量 < 1000 筆，只保留必要索引
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 索引 1：SKU 查詢索引（必要）
-- 名稱：idx_putaway_profiles_sku
-- 類型：B-tree
-- 欄位：sku_id
-- 用途：加速查詢某個 SKU 的首選儲位
-- 說明：雖然有唯一約束，但單獨索引可加速 JOIN 查詢
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_putaway_profiles_sku 
    ON putaway_profiles (sku_id);

-- -----------------------------------------------------------------------------
-- 索引 2：儲位查詢索引（選擇性建立）
-- 名稱：idx_putaway_profiles_bin
-- 類型：B-tree
-- 欄位：preferred_bin_id
-- 用途：加速查詢某個儲位被哪些 SKU 設為首選
-- 範例：SELECT * FROM putaway_profiles WHERE preferred_bin_id = 101;
-- 說明：如果經常需要反向查詢儲位使用情況，才建立此索引
-- -----------------------------------------------------------------------------
-- CREATE INDEX IF NOT EXISTS idx_putaway_profiles_bin 
--     ON putaway_profiles (preferred_bin_id);

-- =============================================================================
-- 註解說明 (Comments)
-- =============================================================================

COMMENT ON TABLE putaway_profiles IS '商品上架策略設定表，定義 SKU 的首選儲位';
COMMENT ON COLUMN putaway_profiles.id IS '設定唯一識別碼，主鍵';
COMMENT ON COLUMN putaway_profiles.sku_id IS 'SKU ID，關聯到 skus 表';
COMMENT ON COLUMN putaway_profiles.preferred_bin_id IS '首選儲位 ID，關聯到 locations 表';
COMMENT ON COLUMN putaway_profiles.created_at IS '設定建立時間';