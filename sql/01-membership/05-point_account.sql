-- =========================================================================
-- points_accounts：積分帳戶表
-- 用途：每個使用者擁有一個積分帳戶，儲存目前可用積分餘額
-- 設計考量：與 users 表 1:1 分離，因為積分是高度頻繁更新的欄位
-- =========================================================================
CREATE TABLE point_accounts (
    id         INT NOT NULL,                       -- 使用者ID，與 users.id 1:1對應 (User ID, 1:1 with users)
    balance    INT NOT NULL DEFAULT 0,             -- 目前可用積分餘額（可正可負）(Current point balance)
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(), -- 最後更新時間（每次交易後更新）(Last update time)
    
    -- 主鍵約束
    CONSTRAINT pk_point_accounts PRIMARY KEY (id),

    -- 外鍵約束：使用者刪除時自動刪除積分帳戶
    CONSTRAINT fk_point_accounts_user FOREIGN KEY (id) 
        REFERENCES users(id) ON DELETE CASCADE,
    
    -- 檢查餘額不低於某個下限（例如：不低於 -1000）
    CONSTRAINT ck_point_accounts_balance CHECK (balance >= 0)
);