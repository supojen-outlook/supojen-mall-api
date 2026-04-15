-- =============================================================================
-- promotions：促銷活動主檔
-- 用途：定義促銷活動的基本資訊、適用對象、使用限制
-- 設計考量：一個活動可以有多條規則 (promotion_rules)、多個適用範圍 (promotion_scopes)
-- =============================================================================
CREATE TABLE promotions (
    -- 基礎資訊欄位 (Basic Information)
    -- -------------------------------------------------------------------------
    id              INT NOT NULL,               -- 促銷活動唯一識別碼 (Unique promotion ID)
    name            VARCHAR(100) NOT NULL,      -- 促銷活動名稱 (Promotion name)
    description     VARCHAR(255),               -- 促銷活動描述 (Promotion description)
    
    -- 時間範圍 (Time Range)
    -- -------------------------------------------------------------------------
    start_date      TIMESTAMPTZ NOT NULL,       -- 促銷開始時間 (Promotion start time)
    end_date        TIMESTAMPTZ NOT NULL,       -- 促銷結束時間 (Promotion end time)
    
    -- 適用通路 (Application Channel)
    -- -------------------------------------------------------------------------
    channel         VARCHAR(20) NOT NULL DEFAULT 'all', -- 適用通路：app行動版/web網頁版/all全部 (Application channel)
    
    -- 會員等級限制 (Member Level Restriction)
    -- -------------------------------------------------------------------------
    user_scope      VARCHAR(20) NOT NULL DEFAULT 'all', -- 適用會員：all全部/bronze青銅/silver白銀/gold黃金/vip尊榮 (Member level scope)
    
    -- 使用限制 (Usage Limits)
    -- -------------------------------------------------------------------------
    limit_per_user  INT,                                -- 每人可使用次數，NULL 表示不限制 (Usage limit per user)
    limit_total     INT,                                -- 總可使用次數，NULL 表示不限制 (Total usage limit)
    used_count      INT NOT NULL DEFAULT 0,             -- 目前已使用次數（方便快速檢查）(Current used count)
    
    -- 狀態控制 (Status Control)
    -- -------------------------------------------------------------------------
    status          VARCHAR(20) NOT NULL DEFAULT 'active', -- 狀態：draft草稿/active啟用/expired過期/disabled停用 (Promotion status)
    
    -- 時間戳欄位 (Timestamp Fields)
    -- -------------------------------------------------------------------------
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),  -- 記錄建立時間 (Record creation time)

    -- 約束條件 (Constraints)
    -- -------------------------------------------------------------------------
    
    -- 主鍵約束：促銷活動唯一識別碼 (Primary key: unique promotion ID)
    CONSTRAINT pk_promotions PRIMARY KEY (id),
    
    -- 檢查約束：通路必須為 app/web/all (Check: channel must be app, web, or all)
    CONSTRAINT ck_promotions_channel CHECK 
        (channel IN ('app', 'web', 'all')),
    
    -- 檢查約束：會員等級必須為指定值 (Check: user scope must be valid)
    CONSTRAINT ck_promotions_user_scope CHECK 
        (user_scope IN ('all', 'bronze', 'silver', 'gold', 'vip')), 
    
    -- 檢查約束：狀態必須為指定值 (Check: status must be draft, active, expired, or disabled)
    CONSTRAINT ck_promotions_status CHECK 
        (status IN ('draft', 'active', 'expired', 'disabled')),
    
    -- 檢查約束：結束時間必須大於開始時間 (Check: end date must be after start date)
    CONSTRAINT ck_promotions_dates CHECK 
        (end_date > start_date),                           
    
    -- 檢查約束：使用限制必須大於 0 或為 NULL (Check: usage limits must be > 0 or NULL)
    CONSTRAINT ck_promotions_limits CHECK 
        ((limit_per_user IS NULL OR limit_per_user > 0) AND
         (limit_total IS NULL OR limit_total > 0) AND
         (used_count >= 0))
);

-- =============================================================================
-- 索引建立 (Indexes)
-- 用途：優化各類常見查詢的效能
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 索引 1：日期範圍查詢索引（必要）
-- 名稱：idx_promotions_dates
-- 類型：B-tree
-- 欄位：start_date, end_date
-- 用途：加速查詢目前有效的促銷活動
-- 場景：結帳時計算適用折扣、促銷列表頁面
-- 範例：SELECT * FROM promotions 
--       WHERE start_date <= NOW() AND end_date >= NOW() AND status = 'active';
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_promotions_dates 
    ON promotions (start_date, end_date);

-- -----------------------------------------------------------------------------
-- 索引 2：狀態查詢索引（必要）
-- 名稱：idx_promotions_status
-- 類型：B-tree
-- 欄位：status
-- 用途：加速依狀態篩選促銷活動
-- 場景：後台管理列表、定時任務更新過期活動
-- 範例：SELECT * FROM promotions WHERE status = 'active';
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_promotions_status 
    ON promotions (status);

-- -----------------------------------------------------------------------------
-- 索引 3：通路查詢索引（選擇性建立）
-- 名稱：idx_promotions_channel
-- 類型：B-tree
-- 欄位：channel
-- 用途：加速依適用通路篩選促銷活動
-- 場景：APP 只顯示適用 APP 的促銷、網頁只顯示適用網頁的促銷
-- 範例：SELECT * FROM promotions WHERE channel IN ('app', 'all') AND status = 'active';
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_promotions_channel 
    ON promotions (channel);

-- =============================================================================
-- 範例資料 (Sample Data)
-- 用途：展示各種促銷活動的設定方式
-- =============================================================================

/*
-- 促銷活動範例：
-- 1. 全館 88 折（雙11活動）
-- 2. 滿千送百（週年慶活動）
-- 3. VIP 專屬免運
*/

INSERT INTO promotions (id, name, description, start_date, end_date, channel, user_scope, limit_per_user, limit_total, status) VALUES
    (1, '雙11全館88折', '雙11購物節全館商品88折', '2024-11-01 00:00:00+00', '2024-11-11 23:59:59+00', 'all', 'all', NULL, NULL, 'active'),
    (2, '週年慶滿千送百', '歡慶週年，單筆滿1000送100', '2024-10-01 00:00:00+00', '2024-10-31 23:59:59+00', 'all', 'all', 3, 1000, 'active'),
    (3, 'VIP 免運專屬', 'VIP會員不限金額免運費', '2024-09-01 00:00:00+00', '2024-12-31 23:59:59+00', 'all', 'vip', NULL, NULL, 'active');

-- =============================================================================
-- 驗證範例 (Validation Queries)
-- =============================================================================

/*
-- 1. 查詢目前有效的促銷活動
SELECT * FROM promotions 
WHERE start_date <= NOW() 
  AND end_date >= NOW() 
  AND status = 'active'
ORDER BY created_at DESC;

-- 2. 查詢 APP 可用的促銷
SELECT * FROM promotions 
WHERE channel IN ('app', 'all') 
  AND status = 'active'
  AND start_date <= NOW() 
  AND end_date >= NOW();

-- 3. 查詢 VIP 會員專屬促銷
SELECT * FROM promotions 
WHERE user_scope IN ('vip', 'all') 
  AND status = 'active';
*/

-- =============================================================================
-- 註解說明 (Comments)
-- =============================================================================

COMMENT ON TABLE promotions IS '促銷活動主檔，定義促銷活動的基本資訊';
COMMENT ON COLUMN promotions.id IS '促銷活動唯一識別碼，主鍵';
COMMENT ON COLUMN promotions.name IS '促銷活動名稱';
COMMENT ON COLUMN promotions.description IS '促銷活動描述';
COMMENT ON COLUMN promotions.start_date IS '促銷開始時間';
COMMENT ON COLUMN promotions.end_date IS '促銷結束時間';
COMMENT ON COLUMN promotions.channel IS '適用通路：app行動版/web網頁版/all全部';
COMMENT ON COLUMN promotions.user_scope IS '適用會員：all全部/bronze青銅/silver白銀/gold黃金/vip尊榮';
COMMENT ON COLUMN promotions.limit_per_user IS '每人可使用次數，NULL 表示不限制';
COMMENT ON COLUMN promotions.limit_total IS '總可使用次數，NULL 表示不限制';
COMMENT ON COLUMN promotions.used_count IS '目前已使用次數';
COMMENT ON COLUMN promotions.status IS '狀態：draft草稿/active啟用/expired過期/disabled停用';
COMMENT ON COLUMN promotions.created_at IS '記錄建立時間';