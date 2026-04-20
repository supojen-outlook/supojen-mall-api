namespace Manian.Application.Services;

/// <summary>
/// 藍新金流服務介面
/// 
/// 職責：
/// - 定義與藍新金流平台交互所需的基本操作
/// - 提供資料加密、解密和檢查碼生成功能
/// - 封裝藍新金流 API 的通訊細節
/// 
/// 設計原則：
/// - 遵循介面隔離原則 (ISP)
/// - 定義最小且必要的方法集合
/// - 不包含實作細節，由 Infrastructure 層提供實作
/// 
/// 實作方式：
/// - 由 Infrastructure 層的 NewebPayService 實作
/// - 使用 AES 加密演算法保護資料傳輸
/// - 使用 SHA256 演算法生成檢查碼
/// 
/// 使用場景：
/// - 訂單付款處理
/// - 付款回調驗證
/// - 退款處理
/// - 定期對帳
/// 
/// 設定需求：
/// - 需要在 appsettings.json 中配置 HashKey 和 HashIV
/// - 透過 NewebPaySettings 類別管理設定
/// - 支援多環境配置（開發、測試、生產）
/// 
/// 安全考量：
/// - 使用 AES-CBC 加密模式保護敏感資料
/// - 使用 PKCS7 填充方式
/// - 檢查碼確保資料完整性
/// - 金鑰和 IV 從設定檔讀取，不硬編碼
/// 
/// 註冊方式：
/// - 在 Infrastructure/DI.cs 中透過 AddNewebPayService 方法註冊
/// - 使用 Transient 生命週期
/// - 透過 IOptions<NewebPaySettings> 注入設定
/// </summary>
public interface INewebPayService
{
    /// <summary>
    /// 商店代號
    /// </summary>
    public string MerchantID { get; }

    /// <summary>
    /// 加密：將物件轉為 AES Hex 字串
    /// </summary>
    /// <param name="queryString">要加密的查詢字串</param>
    /// <returns>加密後的 Hex 字串</returns>
    public string EncryptAes(string queryString);

    /// <summary>
    /// 解密：將藍新回傳的 Hex 字串轉回原始字串
    /// </summary>
    /// <param name="hexString">要解密的 Hex 字串</param>
    /// <returns>解密後的原始字串</returns>
    public string DecryptAes(string hexString);

    /// <summary>
    /// 產生檢查碼
    /// </summary>
    /// <param name="aesString">加密後的 AES 字串</param>
    /// <returns>SHA256 檢查碼</returns>
    public string GetSha256(string aesString);
}
