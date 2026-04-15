-- =========================================================================
-- users：使用者主檔
-- 用途：儲存使用者的核心認證資訊與個人資料，合併認證與業務邏輯
-- 設計考量：一般專案使用一張表即可，簡化查詢複雜度
-- =========================================================================
CREATE TABLE users (
    -- 核心認證欄位 (Core authentication fields)
    id              INT NOT NULL,                          -- 使用者ID，全域唯一 (User ID, globally unique)
    password_hash   TEXT,                                  -- 密碼雜湊值，可為空（如果只用第三方登入）(Password hash, nullable)
    email           VARCHAR(255) NOT NULL,                 -- 電子郵件，用於登入與聯絡 (Email for login and contact)
    email_verified  BOOLEAN DEFAULT FALSE,                 -- 郵箱是否驗證 (Email verified flag)
    status          VARCHAR(20) NOT NULL DEFAULT 'active', -- 帳號狀態：active正常/suspended停權/deleted刪除 (Account status)
    
    -- 個人資料欄位 (Profile fields)
    display_name    VARCHAR(120),                       -- 顯示名稱（暱稱）(Display name/nickname)
    full_name       VARCHAR(120),                       -- 真實姓名 (Real name)
    birth_date      DATE,                               -- 出生日期 (Birth date)
    gender          VARCHAR(10),                        -- 性別：male, female, other (Gender)
    avatar          VARCHAR(500),                       -- 頭像網址 (Avatar URL)
    membership_level VARCHAR(20) DEFAULT 'bronze',      -- 會員等級：bronze, silver, gold, vip (Membership level)
    note            VARCHAR(500) DEFAULT '',            -- 備註 (Note)
    
    -- 時間戳欄位 (Timestamp fields)
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(), -- 註冊時間 (Registration time)
    updated_at      TIMESTAMPTZ,                        -- 最後更新時間（個人資料變更時更新）(Last profile update time)
    
    -- 主鍵約束 (Primary key constraint)
    CONSTRAINT pk_users PRIMARY KEY (id),
    
    -- 唯一約束 (Unique constraints)
    CONSTRAINT uk_users_email UNIQUE (email),                -- 電子郵件唯一 (Unique email)
    
    -- 檢查約束 (Check constraints)
    CONSTRAINT ck_users_status CHECK (status IN ('active', 'suspended', 'deleted')),
    CONSTRAINT ck_users_gender CHECK (gender IS NULL OR gender IN ('male', 'female', 'other')),
    CONSTRAINT ck_users_membership_level CHECK (membership_level IN ('bronze', 'silver', 'gold', 'vip'))
);

-- =========================================================================
-- 索引建立 (Indexes)
-- 用途：優化查詢效能，加快資料檢索速度
-- 說明：PostgreSQL 會自動為 PRIMARY KEY 和 UNIQUE 約束建立索引，
--       以下為額外建立的索引，用於加速常見的查詢模式
-- =========================================================================

-- -------------------------------------------------------------------------
-- 索引 1：電子郵件查詢索引
-- 名稱：idx_users_email
-- 欄位：email (帶條件，過濾空值)
-- 用途：加速登入驗證和使用者查詢
-- 場景：使用者登入時透過 email 查詢帳號資訊
-- 範例查詢：SELECT id, password_hash, status FROM users 
--          WHERE email = 'user@example.com';
-- -------------------------------------------------------------------------
CREATE INDEX idx_users_email ON users(email) WHERE email IS NOT NULL;

-- -------------------------------------------------------------------------
-- 索引 2：會員等級查詢索引
-- 名稱：idx_users_membership_level
-- 欄位：membership_level
-- 用途：加速會員等級相關的統計和篩選查詢
-- 場景：後台管理需要查詢特定等級的會員列表
-- 範例查詢：SELECT id, email, display_name FROM users 
--          WHERE membership_level = 'gold';
-- -------------------------------------------------------------------------
CREATE INDEX idx_users_membership_level ON users(membership_level);

-- -------------------------------------------------------------------------
-- 索引 3：帳號狀態查詢索引
-- 名稱：idx_users_status
-- 欄位：status
-- 用途：加速帳號狀態的篩選查詢
-- 場景：後台管理查詢被停權或刪除的帳號
-- 範例查詢：SELECT id, email, username FROM users 
--          WHERE status = 'suspended';
-- -------------------------------------------------------------------------
CREATE INDEX idx_users_status ON users(status);

-- -------------------------------------------------------------------------
-- 索引 4：複合查詢索引（選擇性建立）
-- 名稱：idx_users_status_level
-- 欄位：(status, membership_level)
-- 用途：加速同時篩選狀態和等級的複合查詢
-- 場景：後台管理需要查詢「活躍的金牌會員」
-- 範例查詢：SELECT id, email, display_name FROM users 
--          WHERE status = 'active' AND membership_level = 'gold';
-- 說明：如果這種查詢很頻繁才需要建立，否則可以省略
-- -------------------------------------------------------------------------
CREATE INDEX idx_users_status_level ON users(status, membership_level);