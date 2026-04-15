using Manian.Domain.Entities.Assets;
using Manian.Domain.Repositories.Assets;
using Manian.Domain.Services;
using Po.Media;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Assets;

/// <summary>
/// 新增資產命令 (CQRS 模式中的 Command)
/// 
/// 用途：封裝新增媒體資源所需的資訊，包含檔案流與副檔名
/// 設計模式：實作 IRequest<Asset>，表示這是一個會回傳建立資產的命令
/// 
/// 使用場景：
/// - 使用者上傳產品圖片
/// - 使用者上傳品牌 Logo
/// - 系統上傳分類縮圖
/// 
/// 注意事項：
/// - 此類別實作了 IDisposable，使用完畢後應確保呼叫 Dispose 以釋放記憶體
/// - 檔案大小限制由 MediaService 控制
/// - 副檔名用於簡單判斷媒體類型
/// </summary>
public class AssetAddCommand : IRequest<Asset>
{
    /// <summary>
    /// 檔案資料流
    /// 
    /// 用途：
    /// - 儲存上傳的圖片或影片二進位資料
    /// - 傳遞給 MediaService 進行實際上傳
    /// 
    /// 驗證規則：
    /// - 不得為 Null
    /// - 必須包含有效的檔案內容
    /// 
    /// 資源管理：
    /// - 使用 MemoryStream 包裝，需手動釋放
    /// </summary>
    public MemoryStream File { get; set; } = new ();

    /// <summary>
    /// 檔案副檔名
    /// 
    /// 用途：
    /// - 判斷檔案類型 (image/video)
    /// - 生成 S3 儲存路徑 (S3Key)
    /// 
    /// 可選值：
    /// - 圖片：.jpg, .png, .jpeg 等
    /// - 影片：.mp4
    /// 
    /// 驗證規則：
    /// - 必填
    /// - 必須包含點號 (.)
    /// </summary>
    public required string FileExt { get; set; }
    
    /// <summary>
    /// 釋放資源
    /// 
    /// 用途：
    /// - 釋放 <see cref="File"/> 佔用的記憶體
    /// 
    /// 執行時機：
    /// - Mediator 流程結束後自動呼叫
    /// - 或由呼叫端手動呼叫
    /// </summary>
    public void Dispose()
    {
        File.Dispose();
    }
}

/// <summary>
/// 新增資產命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 AssetAddCommand 命令
/// - 生成唯一的資源 ID
/// - 建立資產實體並設定 S3 路徑
/// - 呼叫 MediaService 上傳檔案
/// - 更新資產 URL 與大小並儲存
/// 
/// 設計模式：
/// - 實作 IRequestHandler<AssetAddCommand, Asset> 介面
/// - 遵循單一職責原則 (SRP)
/// - 使用依賴注入 (DI) 取得所需服務
/// 
/// 生命週期：
/// - 由 Mediator 框架管理
/// - 每次請求建立新實例 (Transient)
/// 
/// 測試性：
/// - 可輕易 Mock IAssetRepository 與 IMediaService
/// - 邏輯清晰，方便單元測試
/// 
/// 潛在問題：
/// - 目前 TargetId 與 TargetType 未在此處理，需在後續流程補充關聯
/// - 媒體類型判斷邏輯較為簡單 (.mp4 vs 其他)，未檢查 MIME Header
/// 
/// 參考實作：
/// - CartItemUpdateHandler：類似的查詢與更新邏輯
/// - ProductDeleteHandler：類似的資源處理邏輯
/// </summary>
public class AssetAddHandler : IRequestHandler<AssetAddCommand, Asset>
{
    /// <summary>
    /// 資產倉儲介面
    /// 
    /// 用途：
    /// - 存取資產資料
    /// - 提供新增、儲存等操作
    /// 
    /// 實作方式：
    /// - 使用 EF Core 實作
    /// - 繼承自 Repository<Asset>，獲得通用 CRUD 功能
    /// 
    /// 介面定義：
    /// - 見 Domain/Repositories/Assets/IAssetRepository.cs
    /// </summary>
    private readonly IAssetRepository _assetRepository;

    /// <summary>
    /// 媒體服務介面
    /// 
    /// 用途：
    /// - 處理檔案上傳至 S3 的邏輯
    /// - 生成公開訪問的 URL
    /// - 取得檔案實際大小
    /// 
    /// 實作方式：
    /// - 見 Infrastructure/Services/MediaService.cs
    /// - 封裝 AWS SDK 或其他雲端儲存 SDK
    /// </summary>
    private readonly IMediaService _mediaService;

    /// <summary>
    /// 唯一識別碼服務介面
    /// 
    /// 用途：
    /// - 生成全域唯一的整數 ID
    /// - 確保分散式環境下的 ID 唯一性
    /// 
    /// 實作方式：
    /// - 見 Domain/Services/UniqueIdentifier.cs
    /// - 可能使用 Snowflake 演算法或資料庫 Sequence
    /// </summary>
    private readonly IUniqueIdentifier _uniqueIdentifier;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="assetRepository">資產倉儲，用於新增和儲存資產實體</param>
    /// <param name="mediaService">媒體服務，負責檔案上傳至 S3</param>
    /// <param name="uniqueIdentifier">唯一 ID 產生器，用於生成資源 ID</param>
    public AssetAddHandler(
        IAssetRepository assetRepository, 
        IMediaService mediaService, 
        IUniqueIdentifier uniqueIdentifier)
    {
        _assetRepository = assetRepository;
        _mediaService = mediaService;
        _uniqueIdentifier = uniqueIdentifier;
    }

    /// <summary>
    /// 處理新增資產命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 生成全域唯一的整數 ID
    /// 2. 建立 Asset 實體並設定 S3 路徑資訊 (Bucket, Key)
    /// 3. 將實體加入 Repository 追蹤
    /// 4. 呼叫 MediaService 將檔案上傳至 S3
    /// 5. 更新 Asset 的 URL 與 FileSizeBytes
    /// 6. 將變更儲存至資料庫
    /// 
    /// 錯誤處理：
    /// - 檔案上傳失敗：由 MediaService 拋出例外
    /// - 資料庫儲存失敗：由 Repository 拋出例外
    /// 
    /// 注意事項：
    /// - S3Key 組合為 {ID}{副檔名}，例如 "12345.jpg"
    /// - MediaType 簡單判斷：.mp4 為 video，其他為 image
    /// - S3Bucket 固定為 "assets"
    /// </summary>
    /// <param name="request">新增資產命令物件，包含檔案流與副檔名</param>
    /// <returns>已建立並儲存的資產實體 <see cref="Asset"/></returns>
    public async Task<Asset> HandleAsync(AssetAddCommand request)
    {
        // ========== 第一步：生成全域唯一的整數 ID ==========
        var id = _uniqueIdentifier.NextInt();

        // ========== 第二步：建立 Asset 實體並設定初始值 ==========
        var asset = new Asset()
        {
            Id = id,
            // 依據副檔名簡單判斷媒體類型：.mp4 為影片，其他預設為圖片
            MediaType = request.FileExt == ".mp4" ? "video" : "image",
            Bucket = "assets", // 固定存儲在 'assets' 這個 Bucket
            Key = $"{id}{request.FileExt}",
            
        };

        // ========== 第三步：將實體加入 Repository 的追蹤清單 ==========
        // 注意：此時尚未寫入資料庫
        _assetRepository.Add(asset);

        // ========== 第四步：呼叫媒體服務執行非同步上傳 ==========
        var media = await _mediaService.UploadAsync(request.File, new UploadOption()
        {
            // 根據副檔名設定媒體服務所需的列舉類型
            Type = request.FileExt == ".mp4" ? MediaType.mp4 : MediaType.image,
            Directory = asset.Bucket, // 對應 S3 Bucket
            Name = asset.Key         // 對應 S3 Object Key
        });

        // ========== 第五步：上傳成功後，更新實體資訊 ==========
        asset.Url = media.Url;
        asset.FileSizeBytes = media.Size;

        // ========== 第六步：儲存變更至資料庫 ==========
        await _assetRepository.SaveChangeAsync();

        // ========== 第七步：返回完整的資產實體 ==========
        return asset;
    }
}
