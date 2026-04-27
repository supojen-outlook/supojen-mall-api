-- =============================================================================
-- promotion_usages：促銷使用紀錄表
-- 用途：記錄每個用戶使用促銷的次數，防止超用，並追蹤促銷成效
-- 設計考量：與 orders 表關聯，確保同一訂單不會重複使用同一促銷
-- =============================================================================
CREATE TABLE promotion_usages (
    -- 基礎資訊欄位 (Basic Information)
    -- -------------------------------------------------------------------------
    id              BIGINT NOT NULL,                      -- 使用紀錄唯一識別碼 (Unique usage record ID)
    promotion_id    INT NOT NULL,                         -- 促銷活動 ID (Promotion ID)
    user_id         INT NOT NULL,                         -- 用戶 ID (User ID)
    order_id        INT NOT NULL,                         -- 訂單 ID (Order ID)
    
    -- 使用資訊 (Usage Information)
    -- -------------------------------------------------------------------------
    used_quantity   INT NOT NULL DEFAULT 1,               -- 使用次數（通常為1，除非有累計次數的促銷）(Usage quantity)
    used_at         TIMESTAMPTZ NOT NULL DEFAULT NOW(),   -- 使用時間 (Usage time)
    
    -- 快照資訊 (Snapshot Information)
    -- -------------------------------------------------------------------------
    rule_snapshot   JSONB,                                -- 使用當時的規則快照，避免規則修改後影響歷史紀錄 (Rule snapshot)
    
    -- 時間戳欄位 (Timestamp Fields)
    -- -------------------------------------------------------------------------
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),   -- 記錄建立時間 (Record creation time)

    -- 約束條件 (Constraints)
    -- -------------------------------------------------------------------------

    -- 主鍵約束：使用紀錄唯一識別碼 (Primary key: unique usage record ID)
    CONSTRAINT pk_promotion_usages PRIMARY KEY (id),
    
    -- 外鍵約束：刪除促銷活動時自動刪除使用紀錄 (Foreign key: delete usages when promotion is deleted)
    CONSTRAINT fk_promotion_usages_promotion 
        FOREIGN KEY (promotion_id) 
        REFERENCES promotions(id) 
        ON DELETE CASCADE,
    
    -- 外鍵約束：刪除用戶時自動刪除使用紀錄 (Foreign key: delete usages when user is deleted)
    CONSTRAINT fk_promotion_usages_user 
        FOREIGN KEY (user_id) 
        REFERENCES users(id) 
        ON DELETE CASCADE,
    
    -- 外鍵約束：刪除訂單時自動刪除使用紀錄 (Foreign key: delete usages when order is deleted)
    CONSTRAINT fk_promotion_usages_order 
        FOREIGN KEY (order_id) 
        REFERENCES orders(id) 
        ON DELETE CASCADE,
    
    -- 唯一約束：確保同一訂單不會重複使用同一促銷
    -- 唯一約束：同一訂單只能使用同一促銷一次 (Unique: one promotion per order)
    CONSTRAINT uk_promotion_usages_order 
        UNIQUE (promotion_id, order_id),           
    
    -- 檢查約束：使用次數必須大於 0 (Check: usage quantity must be > 0)
    CONSTRAINT ck_promotion_usages_quantity CHECK 
        (used_quantity > 0)                                  
);

-- =============================================================================
-- 索引建立 (Indexes)
-- 用途：優化各類常見查詢的效能
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 索引 1：促銷活動查詢索引（必要）
-- 名稱：idx_promotion_usages_promotion
-- 類型：B-tree
-- 欄位：promotion_id
-- 用途：加速查詢某個促銷的總使用次數
-- 場景：更新 promotions.used_count、檢查促銷是否已達上限
-- 範例：SELECT COUNT(*) FROM promotion_usages WHERE promotion_id = 2;
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_promotion_usages_promotion 
    ON promotion_usages (promotion_id);

-- -----------------------------------------------------------------------------
-- 索引 2：用戶使用紀錄查詢索引（必要）
-- 名稱：idx_promotion_usages_user
-- 類型：B-tree
-- 欄位：user_id, used_at DESC
-- 用途：加速查詢某個用戶的促銷使用歷史
-- 場景：檢查用戶是否已達使用上限、用戶優惠券使用紀錄
-- 範例：SELECT * FROM promotion_usages WHERE user_id = 1001 ORDER BY used_at DESC;
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_promotion_usages_user 
    ON promotion_usages (user_id, used_at DESC);

-- -----------------------------------------------------------------------------
-- 索引 3：訂單查詢索引（選擇性建立）
-- 名稱：idx_promotion_usages_order
-- 類型：B-tree
-- 欄位：order_id
-- 用途：加速查詢某個訂單使用的促銷
-- 場景：訂單詳情頁面顯示使用的優惠
-- 範例：SELECT * FROM promotion_usages WHERE order_id = 1001;
-- 說明：因為有 UK (promotion_id, order_id)，order_id 本身也有索引效果
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_promotion_usages_order 
    ON promotion_usages (order_id);

-- =============================================================================
-- 觸發器 (Triggers)
-- 用途：自動更新 promotions 表的 used_count
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 觸發器函數：更新促銷活動的已使用次數
-- 名稱：fn_update_promotion_used_count
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_update_promotion_used_count()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'INSERT' THEN
        -- 新增使用紀錄時，增加 used_count
        UPDATE promotions 
        SET used_count = used_count + NEW.used_quantity
        WHERE id = NEW.promotion_id;
        RETURN NEW;
    ELSIF TG_OP = 'DELETE' THEN
        -- 刪除使用紀錄時，減少 used_count
        UPDATE promotions 
        SET used_count = used_count - OLD.used_quantity
        WHERE id = OLD.promotion_id;
        RETURN OLD;
    END IF;
    RETURN NULL;
END;
$$ LANGUAGE plpgsql;

-- -----------------------------------------------------------------------------
-- 觸發器：在新增或刪除使用紀錄時自動更新 used_count
-- -----------------------------------------------------------------------------
CREATE TRIGGER trg_promotion_usages_after_insert
    AFTER INSERT ON promotion_usages
    FOR EACH ROW
    EXECUTE FUNCTION fn_update_promotion_used_count();

CREATE TRIGGER trg_promotion_usages_after_delete
    AFTER DELETE ON promotion_usages
    FOR EACH ROW
    EXECUTE FUNCTION fn_update_promotion_used_count();

-- =============================================================================
-- 註解說明 (Comments)
-- =============================================================================

COMMENT ON TABLE promotion_usages IS '促銷使用紀錄表，記錄每個用戶使用促銷的次數';
COMMENT ON COLUMN promotion_usages.id IS '使用紀錄唯一識別碼，主鍵';
COMMENT ON COLUMN promotion_usages.promotion_id IS '促銷活動 ID';
COMMENT ON COLUMN promotion_usages.user_id IS '用戶 ID';
COMMENT ON COLUMN promotion_usages.order_id IS '訂單 ID';
COMMENT ON COLUMN promotion_usages.used_quantity IS '使用次數（通常為1）';
COMMENT ON COLUMN promotion_usages.used_at IS '使用時間';
COMMENT ON COLUMN promotion_usages.rule_snapshot IS '使用當時的規則快照，避免規則修改影響歷史紀錄';
COMMENT ON COLUMN promotion_usages.created_at IS '記錄建立時間';