-- =========================================================================
-- after_user_insert：新使用者建立後的自動化處理函數
-- 用途：自動建立個人資料記錄、積分帳戶，可選自動分配預設角色
-- =========================================================================
CREATE OR REPLACE FUNCTION after_user_insert()
RETURNS TRIGGER AS $$
BEGIN    
    -- 自動建立積分帳戶
    INSERT INTO point_accounts (id, balance, updated_at) 
    VALUES (NEW.id, 0, NOW());
    
    -- 自動分配預設角色（如果需要的話）
    INSERT INTO user_roles (user_id, role_id) 
    SELECT NEW.id, id FROM roles WHERE code = 'customer';
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- 在 users 表上建立觸發器
CREATE TRIGGER tr_users_after_insert
    AFTER INSERT ON users
    FOR EACH ROW EXECUTE FUNCTION after_user_insert();


-- =========================================================================
-- 加入預設系統管理員
-- =========================================================================
-- 1. 插入 Admin 用戶
INSERT INTO users (
    id, 
    password_hash, 
    status, 
    display_name, 
    full_name, 
    birth_date, 
    gender, 
    avatar, 
    email, 
    email_verified, 
    membership_level, 
    note, 
    created_at, 
    updated_at
)
VALUES (
    1,                                                                  -- ID: 假設為 1
    'pR3Hdx4mYk92YCcSm+BsBzWQ5ovxoa/xnvQAqaaOgjj3SCrZy/6yJa3mIq4M/RvD', -- PasswordHash: 您提供的雜湊值
    'active',                                                           -- Status: 帳號狀態 (active/suspended/deleted)
    'Admin',                                                            -- DisplayName: 顯示名稱
    '系統管理員',                                                         -- FullName: 真實姓名
    '1990-01-01',                                                       -- BirthDate: 生日
    'other',                                                            -- Gender: 性別
    '',                                                                 -- Avatar: 頭像 URL
    'admin@example.com',                                                -- Email: 登入信箱
    true,                                                               -- EmailVerified: 信箱已驗證
    'vip',                                                              -- MembershipLevel: 會員等級
    '預設系統管理員帳號',                                                  -- Note: 備註
    NOW(),                                                              -- CreatedAt: 建立時間
    NOW()                                                               -- UpdatedAt: 更新時間
)
ON CONFLICT (email) DO NOTHING; -- 如果 email 已存在則不執行插入

-- 2. 為 Admin 用戶分配 Admin 角色
-- 注意：這裡手動插入是為了確保 Admin 用戶擁有 Admin 角色
-- 觸發器 after_user_insert 會自動分配 'customer' 角色，所以我們需要額外插入 'admin' 角色
INSERT INTO user_roles (user_id, role_id)
VALUES (1, 2)
ON CONFLICT DO NOTHING;