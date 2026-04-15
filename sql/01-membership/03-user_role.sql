-- =========================================================================
-- user_roles：使用者與角色關聯表（多對多）
-- 用途：一個使用者可以擁有多個角色，一個角色可以分配給多個使用者
-- =========================================================================
CREATE TABLE user_roles (
    user_id     INT NOT NULL,                       -- 使用者ID (User ID)
    role_id     INT NOT NULL,                       -- 角色ID (Role ID)
    assigned_at TIMESTAMPTZ NOT NULL DEFAULT NOW(), -- 分配時間 (Assignment time)
    
    -- 複合主鍵：確保同一使用者不會重複分配同一角色
    CONSTRAINT pk_user_roles PRIMARY KEY (user_id, role_id),
    
    -- 外鍵約束：當使用者刪除時，自動刪除關聯記錄
    CONSTRAINT fk_user_roles_user FOREIGN KEY (user_id) 
        REFERENCES users(id) ON DELETE CASCADE,
    
    -- 外鍵約束：當角色刪除時，自動刪除關聯記錄
    CONSTRAINT fk_user_roles_role FOREIGN KEY (role_id) 
        REFERENCES roles(id) ON DELETE CASCADE
);

-- 索引建立 (Indexes)
-- 查詢特定角色的使用者
CREATE INDEX idx_user_roles_role ON user_roles(role_id);