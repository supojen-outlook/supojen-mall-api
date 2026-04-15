-- =========================================================================
-- identities：登入身份表
-- 用途：支援第三方登入（Google、Facebook、LINE等），一個使用者可以綁定多個第三方帳號
-- =========================================================================
CREATE TABLE identities (
    id           INT NOT NULL,              -- 使用者ID，與 users.id 1:1對應 (User ID, 1:1 with users)
    user_id      INT NOT NULL,              -- 關聯的使用者ID (Associated user ID)
    provider     VARCHAR(50) NOT NULL,      -- 第三方提供者：google, facebook, line, apple (Provider name)
    provider_uid VARCHAR(255) NOT NULL,     -- 第三方平台的使用者唯一識別碼 (Provider's user ID)
    created_at   TIMESTAMPTZ DEFAULT NOW(), -- 綁定時間 (Binding time)
    
    -- 主鍵約束
    CONSTRAINT pk_identities PRIMARY KEY (id),

    -- 外鍵約束：使用者刪除時自動刪除所有綁定的第三方身份
    CONSTRAINT fk_identities_user FOREIGN KEY (user_id) 
    REFERENCES users(id) ON DELETE CASCADE,
    
    -- 唯一約束：同一個第三方平台不能重複綁定同一個第三方UID
    CONSTRAINT uk_identities_provider_uid UNIQUE (provider, provider_uid),
    
    -- 唯一約束：同一個使用者不能重複綁定同一個第三方平台
    CONSTRAINT uk_identities_user_provider UNIQUE (id, provider)
);


-- =========================================================================
-- 索引建立 (Indexes)
-- 用途：優化查詢效能，加快資料檢索速度
-- 說明：PostgreSQL 會自動為 PRIMARY KEY 和 UNIQUE 約束建立索引，
--       以下為額外建立的索引，用於加速常見的查詢模式
-- =========================================================================

-- -------------------------------------------------------------------------
-- 索引 1：第三方登入查詢索引
-- 名稱：idx_identities_provider
-- 欄位組合：(provider, provider_uid)
-- 用途：加速「使用者透過第三方登入」的查詢流程
-- 場景：當使用者點擊「使用 Google 登入」時，系統需要透過第三方回傳的 
--       provider_uid 快速找到對應的本站使用者
-- 範例查詢：SELECT user_id FROM identities 
--          WHERE provider = 'google' AND provider_uid = '123456789';
-- -------------------------------------------------------------------------
CREATE INDEX idx_identities_provider ON identities(provider, provider_uid);

-- 為什麼這樣設計？
-- 1. 複合索引的欄位順序很重要：先 provider 再 provider_uid
--    - 因為查詢時通常會同時指定 provider 和 provider_uid
--    - 先過濾 provider，再過濾 provider_uid，符合索引的使用原則
-- 2. 這個索引可以大幅加快 OAuth 認證流程，因為第三方登入是高频操作
-- 3. 雖然已經有 UNIQUE (provider, provider_uid) 約束會自動建立索引，
--    但這裡明確建立索引可以：
--    - 讓其他開發者清楚知道這個欄位組合經常被查詢
--    - 如果未來移除了 UNIQUE 約束，索引仍然存在

-- -------------------------------------------------------------------------
-- 索引 2：使用者關聯查詢索引
-- 名稱：idx_identities_user
-- 欄位：user_id (單一欄位索引)
-- 用途：加速「查詢某個使用者綁定了哪些第三方帳號」的操作
-- 場景：使用者在個人設定頁面查看「已綁定的社群帳號」列表時
-- 範例查詢：SELECT provider, created_at FROM identities 
--          WHERE user_id = 1001;
-- -------------------------------------------------------------------------
CREATE INDEX idx_identities_user ON identities(user_id);
