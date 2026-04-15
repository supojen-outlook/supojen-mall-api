using Manian.Domain.Repositories.Assets;
using Po.Api.Response;
using Po.Media;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Assets;

/// <summary>
/// 刪除資產命令 (CQRS 模式中的 Command)
/// 
/// 用途：封裝刪除媒體資源所需的資訊
/// 設計模式：實作 IRequest，表示這是一個不回傳資料的命令
/// 
/// 使用場景：
/// - 使用者刪除已上傳但未關聯的圖片
/// - 管理員移除過期的媒體資源
/// - 系統定時任務清理孤兒資源
/// 
/// 注意事項：
/// - 刪除操作不可逆，建議在 UI 層加入確認對話框
/// - 此操作僅刪除資料庫記錄，不會刪除 S3 上的實體檔案
/// - S3 檔案清理通常由獨立的背景服務或定時任務處理
/// </summary>
public class AssetDeleteCommand : IRequest
{
    /// <summary>
    /// 資產 Url
    /// </summary>
    public string[] Urls { get; set; }
}

/// <summary>
/// 刪除資產命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 AssetDeleteCommand 命令
/// - 查詢資產是否存在
/// - 執行刪除操作
/// 
/// 設計模式：
/// - 實作 IRequestHandler<AssetDeleteCommand> 介面
/// - 遵循單一職責原則 (SRP)
/// - 使用依賴注入 (DI) 取得所需服務
/// 
/// 生命週期：
/// - 由 Mediator 框架管理
/// - 每次請求建立新實例
/// 
/// 測試性：
/// - 可輕易 Mock IAssetRepository
/// - 邏輯清晰，方便單元測試
/// 
/// 潛在問題：
/// - 未檢查使用者是否有權限刪除此資源
/// - 未處理 S3 檔案刪除，可能導致存儲空間浪費
/// 
/// 參考實作：
/// - AssetUpdateHandler：類似的查詢邏輯
/// - CartItemDeleteHandler：類似的刪除邏輯
/// </summary>
public class AssetDeleteHandler : IRequestHandler<AssetDeleteCommand>
{
    /// <summary>
    /// 資產倉儲介面
    /// 
    /// 用途：
    /// - 存取資產資料
    /// - 提供查詢、刪除等操作
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
    /// - 進行媒體資源操作
    /// - 上傳、下載、刪除等操作
    /// 
    /// 實作方式：
    /// - 使用 AWS SDK 實作
    /// - 繼承自 MediaService，獲得通用媒體操作功能
    /// 
    /// 介面定義：
    /// - 見 Shared/Media/IMediaService.cs
    /// </summary>
    private readonly IMediaService _mediaService;

    /// <summary>
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="assetRepository">資產倉儲，用於查詢和刪除資產實體</param>
    /// <param name="mediaService">媒體服務，用於刪除 S3 檔案</param>
    public AssetDeleteHandler(IAssetRepository assetRepository, IMediaService mediaService)
    {
        _assetRepository = assetRepository;
        _mediaService = mediaService;
    }

    /// <summary>
    /// 處理刪除資產命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 根據 ID 查詢資產是否存在
    /// 2. 驗證資產是否存在
    /// 3. 標記資產為刪除狀態
    /// 4. 儲存變更
    /// 
    /// 錯誤處理：
    /// - 資產不存在：拋出 Failure.NotFound("找不到資源")
    /// - 資料庫刪除失敗：由 Repository 拋出例外
    /// 
    /// 注意事項：
    /// - 刪除操作不可逆
    /// - 僅刪除資料庫記錄，不刪除 S3 檔案
    /// - S3 檔案清理需由定時任務處理
    /// </summary>
    /// <param name="request">刪除資產命令物件，包含資產 ID</param>
    /// <returns>一個表示非同步操作的工作 (Task)</returns>
    public async Task HandleAsync(AssetDeleteCommand request)
    {
        // ========== 第一步：根據 ID 查詢資產是否存在 ==========
        var assets = await _assetRepository.GetByUrlsAsync(request.Urls);

        // ========== 第二步：驗證資產是否存在 ==========
        if(assets == null || !assets.Any())
            throw Failure.NotFound("找不到資源");

        // ========== 第三步：刪除 S3 檔案 ==========
        foreach (var asset in assets)
        {
            await _mediaService.DeleteAsync(new DeleteOption
            {
                Directory = asset.Bucket,
                Name = asset.Key
            });            
        }

        // ========== 第四步：標記資產為刪除狀態 ==========
        // 使用 IAssetRepository.Delete() 刪除資產
        // 注意：這會從資料庫中永久刪除該筆記錄（硬刪除）
        // 這個方法只會標記實體為待刪除，不會立即寫入資料庫
        foreach (var asset in assets)
        {
            _assetRepository.Delete(asset);   
        }

        // ========== 第四步：儲存變更 ==========
        // 使用 IAssetRepository.SaveChangeAsync() 將變更寫入資料庫
        // 這會提交所有被追蹤的實體變更
        await _assetRepository.SaveChangeAsync();
    }
}
