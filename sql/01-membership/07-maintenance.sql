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