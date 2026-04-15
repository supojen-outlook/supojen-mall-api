-- =============================================================================
-- promotion_scopes：促銷適用範圍表
-- 用途：定義促銷活動適用的商品、類別、品牌
-- 設計考量：支援多對多關係，一個活動可適用多個範圍，也可排除特定範圍
-- 注意：scope_id 沒有直接的外鍵約束，因為它可能指向不同表，需在應用層確保資料一致性
-- =============================================================================
CREATE TABLE promotion_scopes (
    -- 基礎資訊欄位 (Basic Information)
    -- -------------------------------------------------------------------------
    id              INT NOT NULL,  -- 範圍記錄唯一識別碼 (Unique scope record ID)
    promotion_id    INT NOT NULL,  -- 所屬促銷活動 ID (Parent promotion ID)
    
    -- 範圍類型 (Scope Type)
    -- -------------------------------------------------------------------------
    scope_type      VARCHAR(20) NOT NULL,  -- 範圍類型：product商品/category類別/brand品牌/all全館 (Scope type)
    
    -- 範圍 ID (Scope Identifier)
    -- -------------------------------------------------------------------------
    scope_id        INT NOT NULL, -- 根據 scope_type 對應到不同表的 ID (Scope identifier)
    
    -- 包含/排除 (Include/Exclude)
    -- -------------------------------------------------------------------------
    is_exclude      BOOLEAN NOT NULL DEFAULT FALSE, -- TRUE 表示排除這些商品，FALSE 表示包含 (Exclude flag)
    
    -- 時間戳欄位 (Timestamp Fields)
    -- -------------------------------------------------------------------------
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(), -- 記錄建立時間 (Record creation time)

    -- 約束條件 (Constraints)
    -- -------------------------------------------------------------------------

    -- 主鍵約束：範圍記錄唯一識別碼 (Primary key: unique scope record ID)
    CONSTRAINT pk_promotion_scopes PRIMARY KEY (id),
    
    -- 外鍵約束：刪除促銷活動時自動刪除範圍 (Foreign key: delete scopes when promotion is deleted)
    CONSTRAINT fk_promotion_scopes_promotion 
        FOREIGN KEY (promotion_id) 
        REFERENCES promotions(id) 
        ON DELETE CASCADE,
    
    -- 檢查約束：範圍類型必須為指定值 (Check: scope type must be valid)
    CONSTRAINT ck_promotion_scopes_type CHECK 
        (scope_type IN ('product', 'category', 'brand', 'all')),
    
    -- 檢查約束：全館適用時 scope_id 固定為 0 (Check: when scope_type is 'all', scope_id must be 0)
    CONSTRAINT ck_promotion_scopes_all CHECK (
        (scope_type = 'all' AND scope_id = 0) OR
        scope_type != 'all'
    ),
    
    -- 唯一約束：確保同一促銷下不會重複設定相同範圍 (Unique: prevent duplicate scope definitions for the same promotion)
    CONSTRAINT uk_promotion_scopes_unique 
        UNIQUE (promotion_id, scope_type, scope_id, is_exclude)
);

-- =============================================================================
-- 索引建立 (Indexes)
-- 用途：優化各類常見查詢的效能
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 索引 1：促銷活動查詢索引（必要）
-- 名稱：idx_promotion_scopes_promotion
-- 類型：B-tree
-- 欄位：promotion_id
-- 用途：加速查詢某個促銷活動的所有適用範圍
-- 場景：促銷詳情頁面、結帳時檢查商品是否適用
-- 範例：SELECT * FROM promotion_scopes WHERE promotion_id = 1;
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_promotion_scopes_promotion 
    ON promotion_scopes (promotion_id);

-- -----------------------------------------------------------------------------
-- 索引 2：範圍查詢索引（必要）
-- 名稱：idx_promotion_scopes_lookup
-- 類型：B-tree
-- 欄位：scope_type, scope_id
-- 用途：加速查詢某個商品/類別/品牌有哪些適用的促銷
-- 場景：商品詳情頁顯示可用的促銷標籤
-- 範例：SELECT * FROM promotion_scopes 
--       WHERE scope_type = 'product' AND scope_id = 1001;
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_promotion_scopes_lookup 
    ON promotion_scopes (scope_type, scope_id);

-- -----------------------------------------------------------------------------
-- 索引 3：複合查詢索引（選擇性建立）
-- 名稱：idx_promotion_scopes_composite
-- 類型：B-tree
-- 欄位：promotion_id, scope_type, scope_id
-- 用途：加速促銷與範圍的交叉查詢
-- 場景：需要同時過濾促銷和範圍的複雜查詢
-- 說明：如果常用這種複合條件才需要建立
-- -----------------------------------------------------------------------------
-- CREATE INDEX IF NOT EXISTS idx_promotion_scopes_composite 
--     ON promotion_scopes (promotion_id, scope_type, scope_id);

-- =============================================================================
-- 範例資料 (Sample Data)
-- 用途：對應 promotions 範例，為每個活動設定適用範圍
-- =============================================================================

/*
-- 對應 promotions 範例：
-- 1. 雙11全館88折 -> 全館適用
-- 2. 週年慶滿千送百 -> 只適用特定商品
-- 3. VIP 免運專屬 -> 全館適用（但只有 VIP 可見）
*/

INSERT INTO promotion_scopes (id, promotion_id, scope_type, scope_id, is_exclude) VALUES
    -- 雙11全館88折：全館適用
    (1, 1, 'all', 0, FALSE),
    
    -- 週年慶滿千送百：只適用品類 1001000 (陶瓷工藝品) 和 1002000 (玻璃工藝品)
    (2, 2, 'category', 1001000, FALSE),
    (3, 2, 'category', 1002000, FALSE),
    
    -- VIP 免運專屬：全館適用（由 user_scope 控制，這裡不用設範圍）
    (4, 3, 'all', 0, FALSE);

-- =============================================================================
-- 驗證範例 (Validation Queries)
-- =============================================================================

/*
-- 1. 查詢某個促銷活動的所有適用範圍
SELECT * FROM promotion_scopes WHERE promotion_id = 2;

-- 2. 查詢某個商品有哪些適用的促銷
SELECT ps.*, p.name, p.start_date, p.end_date
FROM promotion_scopes ps
JOIN promotions p ON ps.promotion_id = p.id
WHERE (ps.scope_type = 'product' AND ps.scope_id = 1001)
   OR (ps.scope_type = 'category' AND ps.scope_id IN (
       SELECT category_id FROM products WHERE id = 1001
   ))
   OR (ps.scope_type = 'brand' AND ps.scope_id IN (
       SELECT brand_id FROM products WHERE id = 1001
   ))
   OR (ps.scope_type = 'all' AND ps.scope_id = 0);

-- 3. 查詢某個促銷排除了哪些範圍
SELECT * FROM promotion_scopes 
WHERE promotion_id = 2 AND is_exclude = TRUE;
*/

-- =============================================================================
-- 註解說明 (Comments)
-- =============================================================================

COMMENT ON TABLE promotion_scopes IS '促銷適用範圍表，定義促銷活動適用的商品、類別、品牌';
COMMENT ON COLUMN promotion_scopes.id IS '範圍記錄唯一識別碼，主鍵';
COMMENT ON COLUMN promotion_scopes.promotion_id IS '所屬促銷活動 ID，關聯到 promotions 表';
COMMENT ON COLUMN promotion_scopes.scope_type IS '範圍類型：product商品/category類別/brand品牌/all全館';
COMMENT ON COLUMN promotion_scopes.scope_id IS '範圍 ID，根據 scope_type 對應到不同表的 ID（商品ID/類別ID/品牌ID）';
COMMENT ON COLUMN promotion_scopes.is_exclude IS '是否排除：TRUE 表示排除這些商品，FALSE 表示包含';
COMMENT ON COLUMN promotion_scopes.created_at IS '記錄建立時間';