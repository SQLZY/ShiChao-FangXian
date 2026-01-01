using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

/// <summary>
/// AES加密解密工具类
/// 提供对称加密功能，保护游戏数据不被轻易修改
/// </summary>
public static class AesUtility
{
    #region 核心加密解密方法

    /// <summary>
    /// AES加密方法
    /// </summary>
    /// <param name="plainText">要加密的明文</param>
    /// <param name="key">加密密钥</param>
    /// <returns>Base64编码的加密字符串</returns>
    public static string Encrypt(string plainText, string key)
    {
        // 参数校验
        if (string.IsNullOrEmpty(plainText))
        {
            Debug.LogWarning("加密内容为空，返回原内容");
            return plainText;
        }

        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException("加密密钥不能为空");
        }

        try
        {
            using (Aes aesAlg = Aes.Create())
            {
                // 从用户密钥派生出符合AES标准的Key和IV
                (byte[] keyBytes, byte[] ivBytes) = DeriveKeyAndIV(key);
                aesAlg.Key = keyBytes;
                aesAlg.IV = ivBytes;

                // 使用CBC模式和PKCS7填充，这是AES的常用安全配置
                aesAlg.Mode = CipherMode.CBC;
                aesAlg.Padding = PaddingMode.PKCS7;

                // 创建加密器
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                // 执行加密
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }
                    }
                    // 返回Base64编码的加密数据，便于存储和传输
                    return Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"AES加密失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// AES解密方法
    /// </summary>
    /// <param name="cipherText">Base64编码的加密字符串</param>
    /// <param name="key">解密密钥（必须与加密密钥相同）</param>
    /// <returns>解密后的明文字符串，解密失败返回null</returns>
    public static string Decrypt(string cipherText, string key)
    {
        // 参数校验
        if (string.IsNullOrEmpty(cipherText))
        {
            Debug.LogWarning("解密内容为空");
            return cipherText;
        }

        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException("解密密钥不能为空");
        }

        try
        {
            using (Aes aesAlg = Aes.Create())
            {
                // 从用户密钥派生出符合AES标准的Key和IV（必须与加密时相同）
                (byte[] keyBytes, byte[] ivBytes) = DeriveKeyAndIV(key);
                aesAlg.Key = keyBytes;
                aesAlg.IV = ivBytes;

                // 使用与加密相同的配置
                aesAlg.Mode = CipherMode.CBC;
                aesAlg.Padding = PaddingMode.PKCS7;

                // 创建解密器
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                // 将Base64字符串转换为字节数组
                byte[] cipherBytes = Convert.FromBase64String(cipherText);

                // 执行解密
                using (MemoryStream msDecrypt = new MemoryStream(cipherBytes))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            return srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
        }
        catch (CryptographicException ex)
        {
            // 密钥错误或数据被篡改
            Debug.LogError($"AES解密失败：可能是密钥错误或数据被损坏: {ex.Message}");
            return null;
        }
        catch (FormatException ex)
        {
            // Base64格式错误
            Debug.LogError($"AES解密失败：数据格式错误，请检查是否为有效的Base64字符串: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Debug.LogError($"AES解密失败: {ex.Message}");
            return null;
        }
    }

    #endregion

    #region 密钥派生方法

    /// <summary>
    /// 从用户提供的字符串密钥派生出AES算法所需的Key和IV
    /// 使用SHA256确保无论输入密钥长度如何，都能生成固定长度的密钥材料
    /// </summary>
    /// <param name="userKey">用户提供的原始密钥</param>
    /// <returns>符合AES标准的Key(32字节)和IV(16字节)</returns>
    private static (byte[] key, byte[] iv) DeriveKeyAndIV(string userKey)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            // 使用SHA256哈希用户密钥，生成固定长度的密钥材料
            byte[] keyMaterial = sha256.ComputeHash(Encoding.UTF8.GetBytes(userKey));

            // AES-256需要32字节的Key
            byte[] key = new byte[32];
            // CBC模式需要16字节的IV
            byte[] iv = new byte[16];

            // 从哈希结果中提取Key和IV
            // Key: 使用前32字节
            Array.Copy(keyMaterial, 0, key, 0, 32);
            // IV: 使用第17-32字节（确保与加密时一致）
            Array.Copy(keyMaterial, 16, iv, 0, 16);

            return (key, iv);
        }
    }

    #endregion
}

/// <summary>
/// 安全的密钥管理器
/// 提供多种密钥生成和管理策略，避免密钥硬编码
/// </summary>
public static class EncryptionKeyManager
{
    /// <summary>
    /// 基于设备信息的动态密钥
    /// 结合设备特定信息，不同设备密钥不同
    /// 注意：用户更换设备或重装系统会导致旧存档无法读取
    /// </summary>
    public static string GetDeviceBasedKey()
    {
        StringBuilder keyBuilder = new StringBuilder();

        // 使用设备特定信息（选择相对稳定的信息）
        keyBuilder.Append(SystemInfo.deviceUniqueIdentifier); // 设备唯一标识
        keyBuilder.Append(SystemInfo.deviceName);             // 设备名称
        keyBuilder.Append(Application.version);               // 游戏版本

        // 添加静态盐值
        keyBuilder.Append("SQLZY78789191");

        // 使用哈希确保长度固定
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(keyBuilder.ToString()));
            return Convert.ToBase64String(hash).Substring(0, 32); // 取前32字符作为密钥
        }
    }

    /// <summary>
    /// 获取固定密钥 用于加密解密不变的配置数据文件
    /// </summary>
    public static string GetDefaultKey(string dataName)
    {
        // 自定义密钥 混合存取数据名称
        string key = "7878SQLZY9191" + dataName + "JHQMYXJJ";

        // 使用哈希确保长度固定
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
            return Convert.ToBase64String(hash).Substring(0, 32); // 取前32字符作为密钥
        }
    }
}