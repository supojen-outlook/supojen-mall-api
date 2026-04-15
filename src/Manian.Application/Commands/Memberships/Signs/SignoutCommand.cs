using Shared.Mediator.Interface;

namespace Manian.Application.Commands.Memberships.Signs;

/// <summary>
/// 登出命令類別 (CQRS 模式中的 Command)
/// 
/// 用途：封裝使用者登出所需的資訊
/// 
/// 設計模式：
/// - 實作 IRequest，表示這是一個不回傳資料的命令
/// - 遵循 CQRS (Command Query Responsibility Segregation) 原則
/// - 與 SignoutCommandHandler 配合使用，完成登出處理
/// 
/// 安全性考量：
/// - 登出操作不需要額外參數，依賴 HTTP 上下文中的認證資訊
/// - 登出會清除認證 Cookie，確保安全性
/// </summary>
public class SignoutCommand : IRequest
{
    // 登出命令不需要任何參數
    // 認證資訊會從 HTTP 上下文中獲取
}

/// <summary>
/// 登出命令處理器 (CQRS 模式中的 Command Handler)
/// 
/// 職責：
/// - 接收 SignoutCommand 命令
/// - 處理登出相關邏輯（如果需要）
/// - 準備登出回應
/// 
/// 設計模式：
/// - 實作 IRequestHandler<SignoutCommand> 介面
/// - 遵循單一職責原則 (SRP)
/// - 使用依賴注入 (DI) 取得所需服務
/// 
/// 生命週期：
/// - 由 Mediator 框架管理
/// - 每次請求建立新實例 (Transient)
/// 
/// 測試性：
/// - 可輕易 Mock 所有依賴服務
/// - 邏輯清晰，方便單元測試
/// 
/// 注意事項：
/// - 實際的登出操作（清除 Cookie）在 Endpoint 層處理
/// - 這個 Handler 主要用於未來可能需要的登出日誌記錄或其他業務邏輯
/// </summary>
public class SignoutCommandHandler : IRequestHandler<SignoutCommand>
{
    /// <summary>
    /// 建構函式 - 初始化處理器
    /// </summary>
    public SignoutCommandHandler()
    {
        // 目前登出不需要任何依賴服務
        // 預留空間給未來可能需要的功能（如日誌記錄、統計等）
    }

    /// <summary>
    /// 處理登出命令的主要方法
    /// 
    /// 執行流程：
    /// 1. 記錄登出操作（可選）
    /// 2. 執行其他登出相關業務邏輯（可選）
    /// 3. 回傳成功結果
    /// 
    /// 注意事項：
    /// - 實際的 Cookie 清除在 Endpoint 層通過 HttpContext.SignOutAsync() 處理
    /// - 這個方法主要用於業務邏輯處理，如需要可在這裡添加
    /// </summary>
    /// <param name="request">登出命令物件（不包含任何參數）</param>
    /// <returns>表示操作完成的 Task</returns>
    public async Task HandleAsync(SignoutCommand request)
    {
        // 目前登出操作不需要任何業務邏輯處理
        // 實際的登出（清除 Cookie）在 Endpoint 層處理
        
        // 預留空間給未來可能需要的功能：
        // - 記錄登出日誌
        // - 更新使用者最後登出時間
        // - 清除快取資料
        // - 發送登出通知等
        
        await Task.CompletedTask;
    }
}
