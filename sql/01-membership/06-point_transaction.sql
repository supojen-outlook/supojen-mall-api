-- =========================================================================
-- points_transactions：積分交易明細表
-- 用途：記錄每一筆積分變動的詳細資訊，用於查詢和對帳
-- =========================================================================
CREATE TABLE point_transactions (
    id            INT NOT NULL,                       -- 交易記錄ID (Transaction ID)
    user_id       INT NOT NULL,                       -- 使用者ID (User ID)
    delta         INT NOT NULL,                       -- 變動量：正數為增加，負數為扣減 (Change amount)
    balance_after INT NOT NULL,                       -- 交易後的餘額（用於快速查詢和對帳）(Balance after transaction)
    reason        VARCHAR(100) NOT NULL,              -- 交易原因：order_purchase, points_expire, admin_adjust (Transaction reason)
    ref_type      VARCHAR(50),                        -- 關聯的業務類型：order, promotion, etc (Reference type)
    ref_id        VARCHAR(100),                       -- 關聯的業務ID (Reference ID)
    occurred_at   TIMESTAMPTZ NOT NULL DEFAULT NOW(), -- 交易發生時間 (Transaction time)
    metadata      JSONB      DEFAULT '{}',            -- 額外的交易資訊（JSON格式）(Additional metadata)
    
    -- 主鍵約束
    CONSTRAINT pk_point_transactions PRIMARY KEY (id),

    -- 外鍵約束：使用者刪除時是否保留交易記錄？
    -- 這裡選擇保留（NO ACTION），因為交易記錄可能是財務稽核所需
    CONSTRAINT fk_point_transactions_user FOREIGN KEY (user_id) 
    REFERENCES users(id) ON DELETE NO ACTION,
    
    -- 檢查變動量不為0
    CONSTRAINT ck_point_transactions_delta CHECK (delta != 0),

    -- 檢查 ref_type 的允許值
    CONSTRAINT ck_point_transactions_ref_type 
    CHECK (ref_type IS NULL OR ref_type IN ('none', 'refund', 'order', 'promotion'))
);


-- =========================================================================
-- 索引 (Indexes)
-- =========================================================================

-- 1. 為 user_id 建立索引（最常用）
-- 用途：加速「查詢特定使用者的積分明細」和 JOIN users 表的效能
-- 說明：因為所有查詢幾乎都是以 user_id 為起點，這個索引最重要
CREATE INDEX idx_point_transactions_user_id 
    ON point_transactions(user_id);