using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace Manian.Infrastructure.Settings;

/// <summary>
/// 金鑰管理類別
/// 負責從檔案系統載入 RSA 非對稱金鑰對和 AES 對稱金鑰
/// 並提供 JWKS (JSON Web Key Set) 格式轉換，供 JWT 簽章驗證使用
/// </summary>
public class Key
{
    /// <summary>
    /// 建構函式 - 從指定路徑載入金鑰檔案
    /// </summary>
    /// <param name="path">金鑰檔案存放的目錄路徑</param>
    public Key(string path)
    {
        // 初始化 RSA 金鑰列表容器
        // 使用 List<RsaKey> 來管理多個金鑰，支援金鑰輪替 (Key Rotation)
        RsaKeys = new List<RsaKey>();
        
        // 搜尋指定路徑下所有以 "id_rsa" 開頭的檔案
        // 例如：id_rsa1, id_rsa2, id_rsa_old 等都會被找到
        // 這樣的命名規則讓我們可以存放多個 RSA 金鑰對
        var rsaFiles = Directory.GetFiles(path, "id_rsa*");
        
        // 逐一處理每個找到的 RSA 金鑰檔案
        foreach (var rsaFile in rsaFiles)
        {            
            // 建立一個新的 RSA 物件實例，準備載入私鑰
            // RSA.Create() 會根據作業系統決定最佳的實作（Windows 用 CAPI/CNG，Linux 用 OpenSSL）
            var prKey = RSA.Create();
            
            // 從檔案讀取所有位元組，這些位元組就是 PKCS#8 格式的私鑰資料
            // 然後用 ImportRSAPrivateKey 將原始位元組資料匯入 RSA 物件
            // out _ 是 C# 的 discard，表示我們不在乎匯入時讀取了多少位元組
            prKey.ImportRSAPrivateKey(File.ReadAllBytes(rsaFile), out _);
        
            // 建立另一個 RSA 物件來存放公鑰
            var puKey = RSA.Create();
            
            // 從剛剛載入的私鑰中匯出公鑰部分（DER 格式的位元組陣列）
            // 再用 ImportRSAPublicKey 將公鑰匯入新的 RSA 物件
            // 這樣我們就有獨立的公鑰物件可以用來加密或驗證簽章
            puKey.ImportRSAPublicKey(prKey.ExportRSAPublicKey(), out _);
            
            // 從檔案名稱中擷取金鑰的識別碼
            // 假設檔案名稱是 "id_rsa.1"，這行程式會取出 "1"
            // 這是用來區分不同金鑰的 ID，例如做金鑰輪替時可以指定用哪一把
            var id = rsaFile.Split('.')[^1];

            // 建立 RSA 金鑰對物件，將私鑰、公鑰和 ID 包裝在一起
            var rsaKey = new RsaKey()
            {
                ID = id,
                Private = prKey,  // 私鑰用於簽署 Token
                Public = puKey     // 公鑰用於驗證 Token
            };
            
            // 將這個金鑰對加入列表
            RsaKeys.Add(rsaKey);
        }
        
        // 載入 AES 對稱金鑰
        // AES 金鑰用於需要對稱加解密的場合（例如加密某些敏感資料）
        // 檔案內容應該是 16、24 或 32 位元組的原始金鑰資料（對應 AES-128、192、256）
        AesKey = File.ReadAllBytes($"{path}/id_aes");
    }
    
    /// <summary>
    /// RSA 非對稱金鑰對列表
    /// 可以包含多組金鑰，支援金鑰輪替 (Key Rotation)
    /// 例如：同時保留舊金鑰（用於驗證舊 Token）和新金鑰（用於簽發新 Token）
    /// </summary>
    public List<RsaKey> RsaKeys { get; set; }

    /// <summary>
    /// AES 對稱金鑰
    /// init 表示這個屬性是唯讀的，只能在建構函式或物件初始化器設定
    /// 確保金鑰在物件建立後就不會被意外修改
    /// </summary>
    public byte[] AesKey { get; init; }

    /// <summary>
    /// 從多組 RSA 金鑰中隨機選取一組
    /// 用於負載平衡或增加安全性（攻擊者不知道這次會用哪把金鑰）
    /// </summary>
    /// <returns>隨機選取的一組 RSA 金鑰對</returns>
    public RsaKey RandomGetRsaKey()
    {
        // 建立隨機數產生器
        var random = new Random();
        
        // 從 RsaKeys 列表中隨機選取一個索引
        // Next 的參數是上限（不包含），所以如果列表有 3 筆，會產生 0,1,2 的隨機數
        // 回傳該索引對應的 RsaKey 物件
        return RsaKeys[random.Next(RsaKeys.Count)];
    }

    /// <summary>
    /// 將 RSA 公鑰轉換為 JSON Web Key Set (JWKS) 格式
    /// 這個屬性會即時產生 JWKS，讓 OIDC Discovery 端點可以回傳給客戶端
    /// 客戶端可以用這些 JWK 來驗證我們簽發的 JWT Token 的簽章
    /// </summary>
    public List<JsonWebKey> Jwks
    {
        get
        {
            // 初始化 JWK 列表容器
            var jwks = new List<JsonWebKey>();
        
            // 逐一處理每一組 RSA 金鑰
            foreach (var rsaKey in RsaKeys)
            {
                // 1. 用 RSA 公鑰建立 RsaSecurityKey
                // RsaSecurityKey 是 Microsoft.IdentityModel 中用來表示 RSA 金鑰的類別
                var securityKey = new RsaSecurityKey(rsaKey.Public)
                {
                    // 設定金鑰的 ID (kid)，這會出現在 JWT 的 Header 中
                    // 讓接收方知道要用哪一把金鑰來驗證簽章
                    KeyId = rsaKey.ID 
                };
                
                // 2. 將 RsaSecurityKey 轉換為 JSON Web Key (JWK) 格式
                // JWK 是 RFC 7517 定義的標準格式，用 JSON 表示密碼學金鑰
                var jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(securityKey);
                
                // 3. 設定這個金鑰使用的演算法
                // RS256 代表 RSA-SHA256，這是 JWT 最常見的簽章演算法
                jwk.Alg = "RS256";
                
                // 4. 設定金鑰的用途
                // "sig" 表示這個金鑰是用來做數位簽章 (signature)
                // 另一個常見值是 "enc" 表示用來加密
                jwk.Use = "sig";
                
                // 5. 將這個 JWK 加入列表
                jwks.Add(jwk);
            }
            
            // 回傳完整的 JWK Set
            return jwks;    
        }
    }
}

/// <summary>
/// RSA 金鑰對的包裝類別
/// 將一組相關的私鑰、公鑰和識別碼包裝在一起
/// </summary>
public class RsaKey
{
    /// <summary>
    /// 金鑰的唯一識別碼 (Key ID)
    /// required 表示在建構這個物件時必須提供這個屬性的值
    /// 通常對應到 JWT 的 "kid" (Key ID) 宣告
    /// </summary>
    public required string ID { get; set; }
    
    /// <summary>
    /// RSA 私鑰
    /// 用於簽發 JWT Token（產生簽章）
    /// 必須嚴格保護，不應該外洩
    /// init 表示只能在建構時設定，之後唯讀
    /// </summary>
    public required RSA Private { get; init; }

    /// <summary>
    /// RSA 公鑰
    /// 用於驗證 JWT Token 的簽章
    /// 可以安全地公開，通常會透過 JWKS 端點提供給客戶端
    /// init 表示只能在建構時設定，之後唯讀
    /// </summary>
    public required RSA Public { get; init; }
}