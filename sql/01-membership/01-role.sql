-- =========================================================================
-- roles：角色定義表
-- 用途：定義系統中的所有角色，每個角色代表一組權限的集合
-- =========================================================================
CREATE TABLE roles (
    id          INT NOT NULL,                 -- 角色ID，使用數字方便程式對照 (Role ID, numeric for easy mapping)
    code        VARCHAR(50) NOT NULL,         -- 角色代碼：customer, admin, support, ops (Role code)
    name        VARCHAR(100) NOT NULL,        -- 角色顯示名稱：商場會員、系統管理員等 (Display name)
    description TEXT,                         -- 角色描述：說明這個角色的職責和權限範圍 (Role description)
    created_at  TIMESTAMPTZ DEFAULT NOW(),    -- 建立時間 (Creation timestamp)

    CONSTRAINT pk_roles PRIMARY KEY (id),     -- 主鍵約束
    CONSTRAINT uk_roles_code UNIQUE (code)    -- 角色代碼唯一約束
);

-- 初始化系統角色資料
INSERT INTO roles (id, code, name, description) VALUES
(1, 'customer',     '商場會員',   '一般消費者，擁有基本購物與訂單功能'),
(2, 'admin',        '系統管理員', '擁有後台管理權限，可管理商品、訂單、會員'),
(3, 'support',      '客服人員',   '可查詢會員資料與訂單，協助處理問題'),
(4, 'ops',          '營運人員',   '可操作促銷活動、積分調整、行銷名單匯出');