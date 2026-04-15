-- =========================================================================
-- assets：媒體資源主檔
-- 用途：儲存所有上傳的圖片、影片等媒體檔案及其 S3 存儲資訊
-- 設計考量：使用 TargetType 和 TargetId 實現多態關聯，支援關聯至 Product, Category 或 Brand
-- =========================================================================
CREATE TABLE assets (
    -- 核心識別欄位 (Core identification fields)
    id              INT PRIMARY KEY,                     -- 資源ID，自動遞增 (Asset ID, auto-increment)
    target_type     VARCHAR(50),                         -- 關聯目標類型 (Target type: e.g., 'product', 'category', 'brand')，可為空
    target_id       BIGINT,                              -- 關聯目標 ID (Target ID)，可為空，代表未關聯的孤兒資源
    
    -- 媒體內容欄位 (Media content fields)
    media_type      VARCHAR(20) NOT NULL,                -- 媒體類型 (Media type: 'image', 'video')
    url             VARCHAR(2048) NOT NULL,              -- 公開訪問 URL (Public access URL)
    mime_type       VARCHAR(100) NOT NULL,               -- MIME 類型 (MIME type: e.g., 'image/jpeg')
    file_size_bytes DECIMAL(10,2) NOT NULL,              -- 檔案大小 (位元組) (File size in bytes)
    
    -- S3 存儲欄位 (S3 storage fields)
    bucket          VARCHAR(255) NOT NULL,               -- S3 存儲桶名稱 (S3 bucket name)
    key             VARCHAR(500) NOT NULL,               -- S3 對象鍵 (S3 object key)
    
    -- 檢查約束 (Check constraints)
    CONSTRAINT ck_assets_target_type CHECK (target_type IS NULL OR target_type IN ('product', 'category', 'brand', 'user')),
    CONSTRAINT ck_assets_media_type CHECK (media_type IN ('image', 'video'))
);

-- =========================================================================
-- 索引建立 (Indexes)
-- 用途：優化查詢效能，加快資料檢索速度
-- 設計重點：優化「孤兒資源清理」與「S3 批次操作」的效能
-- =========================================================================

-- -------------------------------------------------------------------------
-- 索引 1：孤兒資源清理索引 (核心索引)
-- 名稱：idx_assets_cleanup
-- 欄位：(target_id) INCLUDE (bucket, key)
-- 用途：專門用於定期清理任務，快速找出未關聯的資源並獲取 S3 路徑
-- 設計理由：
--   1. 將 target_id 放在最前，因為我們主要查詢條件是 target_id IS NULL。
--   2. 使用 INCLUDE (Covering Index)，查詢時無需回表，直接從索引獲取 bucket 和 key，
--      大幅提升批次刪除 S3 檔案的效率。
-- 場景：定時任務尋找無效圖片並刪除
-- 範例查詢：SELECT bucket, key FROM assets 
--          WHERE target_id IS NULL;
-- -------------------------------------------------------------------------
CREATE INDEX idx_assets_cleanup ON assets(target_id) INCLUDE (bucket, key);

-- -------------------------------------------------------------------------
-- 索引 2：目標關聯查詢索引
-- 名稱：idx_assets_target
-- 欄位：(target_type, target_id)
-- 用途：加速根據關聯目標查詢其所有媒體資源
-- 場景：查詢某個產品的所有圖片
-- 範例查詢：SELECT * FROM assets 
--          WHERE target_type = 'product' AND target_id = 123;
-- -------------------------------------------------------------------------
CREATE INDEX idx_assets_target ON assets(target_type, target_id);

-- -------------------------------------------------------------------------
-- 索引 3：S3 Key 唯一索引 (可選，視業務需求)
-- 名稱：uk_assets_key
-- 欄位：key
-- 用途：防止重複上傳同一個檔案到 S3 (如果業務邏輯保證 Key 唯一)
-- 場景：上傳前檢查 Key 是否已存在
-- 範例查詢：SELECT 1 FROM assets 
--          WHERE key = 'uploads/2023/10/image_001.jpg';
-- -------------------------------------------------------------------------
-- CREATE UNIQUE INDEX uk_assets_key ON assets(key);

-- -------------------------------------------------------------------------
-- 索引 4：公開 URL 查詢索引
-- 名稱：idx_assets_url
-- 欄位：url
-- 用途：加速根據公開 URL 查詢資源，或用於檢查 URL 是否重複
-- 設計理由：
--   1. 業務端可能透過完整的 URL 字串來反查 Asset 資訊。
--   2. 確保在大量資料下，依 URL 搜尋仍能保持高效。
-- 場景：
--   1. 前端傳入圖片 URL，後端需驗證該 URL 是否屬於系統內的資源。
--   2. 資料遷移或同步時，透過 URL 比對資料。
-- 範例查詢：SELECT id, target_type, target_id FROM assets 
--          WHERE url = 'https://cdn.example.com/uploads/img_001.jpg';
-- -------------------------------------------------------------------------
CREATE INDEX idx_assets_url ON assets(url);
