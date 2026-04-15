using Manian.Domain.Repositories.Assets;
using Po.Api.Response;
using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Assets;

/// <summary>
/// 更新資產關聯命令 (CQRS 模式中的 Command)
/// 
/// 用途：封裝更新資產關聯目標所需的資訊，主要用於將已上傳的孤兒資源綁定到具體業務實體
/// 設計模式：實作 IRequest，表示這是一個不回傳資料的命令
/// 
/// 使用場景：
/// - 使用者建立產品後，將上傳的圖片關聯至該產品
/// - 管理員上傳 Logo 後，將其關聯至品牌
/// - 批次處理未關聯的媒體資源
/// 
/// 注意事項：
/// - 此操作僅更新關聯資訊，不會修改檔案本身
/// - 透過 URL 作為查詢條件，確保操作的準確性
/// </summary>
public class AssetUpdateCommand : IRequest
{
    /// <summary>
    /// 資源 URL
    /// 
    /// 用途：
    /// - 指定要更新的資源 URL
    /// - 用於查詢資源是否存在
    /// 
    /// 驗證規則：
    /// - 必須符合資料庫中存儲的 URL 格式
    /// 
    /// 範例：
    /// - /assets/1.jpg
    /// - /assets/2.png
    /// </summary>
    public string[] Urls { get; set; }

    /// <summary>
    /// 關聯目標類型
    /// 
    /// 用途：
    /// - 指定資源將要關聯到的實體類型
    /// - 與 TargetId 配合使用
    /// 
    /// 可選值：
    /// - "product"：產品
    /// - "category"：分類
    /// - "brand"：品牌
    /// 
    /// 驗證規則：
    /// - 必須符合資料庫 ck_assets_target_type 約束
    /// </summary>
    public string TargetType { get; set; }

    /// <summary>
    /// 關聯目標 ID
    /// 
    /// 用途：
    /// - 指定資源將要關聯到的實體 ID
    /// - 與 TargetType 配合使用
    /// 
    /// 驗證規則：
    /// - 必須對應資料庫中存在的實體 ID
    /// - 可為空 (若為空則解除關聯)
    /// </summary>
    public int? TargetId { get; set; }
}

/// <summary>
/// 更新資產關聯命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 AssetUpdateCommand 命令
/// - 根據 URL 查詢資產是否存在
/// - 更新資產的關聯資訊 (TargetType, TargetId)
/// 
/// 設計模式：
/// - 實作 IRequestHandler<AssetUpdateCommand> 介面
/// - 遵循單一職責原則 (SRP)
/// - 使用依賴注入 (DI) 取得所需服務
/// 
/// 生命週期：
/// - 由 Mediator 框架管理
/// - 每次請求建立新實例 (Transient)
/// 
/// 測試性：
/// - 可輕易 Mock IAssetRepository
/// - 邏輯清晰，方便單元測試
/// 
/// 潛在問題：
/// - URL 可能被偽造，建議在實際專案中加入權限檢查
/// - 若 URL 重複，可能導致更新錯誤的資產 (需確保 URL 唯一性)
/// 
/// 參考實作：
/// - AssetAddHandler：類似的資料庫操作邏輯
/// - CartItemUpdateHandler：類似的更新邏輯
/// </summary>
public class AssetUpdateHandler : IRequestHandler<AssetUpdateCommand>
{
    /// <summary>
    /// 資產倉儲介面
    /// 
    /// 用途：
    /// - 存取資產資料
    /// - 提供查詢、更新等操作
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
    /// 建構函式 - 初始化處理器並注入依賴服務
    /// </summary>
    /// <param name="assetRepository">資產倉儲，用於查詢和更新資產實體</param>
    public AssetUpdateHandler(IAssetRepository assetRepository)
    {
        _assetRepository = assetRepository;
    }

    /// <summary>
    /// 處理更新資產關聯命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 根據 URL 查詢資產是否存在
    /// 2. 驗證資產是否存在
    /// 3. 更新資產的關聯資訊 (TargetType, TargetId)
    /// 4. 儲存變更
    /// 
    /// 錯誤處理：
    /// - 資產不存在：拋出 Failure.NotFound("找不到檔案")
    /// - 資料庫更新失敗：由 Repository 拋出例外
    /// 
    /// 注意事項：
    /// - 使用 URL 作為查詢條件，確保操作的準確性
    /// - 更新操作會直接修改資料庫中的記錄
    /// </summary>
    /// <param name="request">更新資產關聯命令物件，包含 URL 及新的關聯資訊</param>
    /// <returns>一個表示非同步操作的工作 (Task)</returns>
    public async Task HandleAsync(AssetUpdateCommand request)
    {
        // ========== 第一步：根據 URL 查詢資產是否存在 ==========
        var assets = await _assetRepository.GetAllAsync(
            q => q.Where(x => request.Urls.Contains(x.Url))
        );

        // ========== 第二步：驗證資產是否存在 ==========
        if (assets == null || !assets.Any())
            throw Failure.NotFound("找不到檔案");

        // ========== 第三步：更新資產的關聯資訊 ==========
        foreach (var asset in assets)
        {
            asset.TargetType = request.TargetType;
            asset.TargetId = request.TargetId;
        }

        // ========== 第四步：儲存變更 ==========
        await _assetRepository.SaveChangeAsync();
    }
}
