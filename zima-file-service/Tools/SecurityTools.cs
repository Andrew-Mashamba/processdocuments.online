using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ZimaFileService.Tools;

/// <summary>
/// Security Tools - Password generation, hashing, encryption, validation, etc.
/// </summary>
public class SecurityTools
{
    private readonly string _generatedPath;

    public SecurityTools()
    {
        _generatedPath = FileManager.Instance.GeneratedFilesPath;
    }

    #region Password Generation

    /// <summary>
    /// Generate secure random passwords
    /// </summary>
    public Task<string> GeneratePasswordAsync(Dictionary<string, object> args)
    {
        var length = GetInt(args, "length", 16);
        var count = GetInt(args, "count", 1);
        var includeUppercase = GetBool(args, "uppercase", true);
        var includeLowercase = GetBool(args, "lowercase", true);
        var includeNumbers = GetBool(args, "numbers", true);
        var includeSymbols = GetBool(args, "symbols", true);
        var excludeAmbiguous = GetBool(args, "exclude_ambiguous", false);
        var excludeChars = GetString(args, "exclude", "");

        if (length < 4) length = 4;
        if (length > 128) length = 128;

        var charPool = new StringBuilder();

        var uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var lowercase = "abcdefghijklmnopqrstuvwxyz";
        var numbers = "0123456789";
        var symbols = "!@#$%^&*()_+-=[]{}|;:,.<>?";
        var ambiguous = "0O1lI";

        if (includeUppercase)
            charPool.Append(uppercase);
        if (includeLowercase)
            charPool.Append(lowercase);
        if (includeNumbers)
            charPool.Append(numbers);
        if (includeSymbols)
            charPool.Append(symbols);

        var pool = charPool.ToString();

        if (excludeAmbiguous)
        {
            foreach (var c in ambiguous)
                pool = pool.Replace(c.ToString(), "");
        }

        foreach (var c in excludeChars)
            pool = pool.Replace(c.ToString(), "");

        if (string.IsNullOrEmpty(pool))
        {
            return Task.FromResult(JsonSerializer.Serialize(new {
                success = false,
                error = "No characters available in pool. Check your options."
            }));
        }

        var passwords = new List<object>();

        for (int i = 0; i < count; i++)
        {
            var password = GenerateSecurePassword(pool, length);
            var strength = AnalyzePasswordStrength(password);

            passwords.Add(new {
                password,
                strength = strength.level,
                entropy_bits = strength.entropy
            });
        }

        return Task.FromResult(JsonSerializer.Serialize(new {
            success = true,
            length,
            passwords = count == 1 ? passwords[0] : passwords
        }));
    }

    private string GenerateSecurePassword(string charPool, int length)
    {
        var password = new char[length];
        var randomBytes = new byte[length];

        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }

        for (int i = 0; i < length; i++)
        {
            password[i] = charPool[randomBytes[i] % charPool.Length];
        }

        return new string(password);
    }

    #endregion

    #region Password Strength Analysis

    /// <summary>
    /// Analyze password strength
    /// </summary>
    public Task<string> AnalyzePasswordStrengthAsync(Dictionary<string, object> args)
    {
        var password = GetString(args, "password");

        var analysis = AnalyzePasswordStrength(password);

        var weaknesses = new List<string>();

        if (password.Length < 8)
            weaknesses.Add("Password is too short (minimum 8 characters)");
        if (password.Length < 12)
            weaknesses.Add("Consider using 12+ characters for better security");
        if (!Regex.IsMatch(password, @"[A-Z]"))
            weaknesses.Add("Add uppercase letters");
        if (!Regex.IsMatch(password, @"[a-z]"))
            weaknesses.Add("Add lowercase letters");
        if (!Regex.IsMatch(password, @"\d"))
            weaknesses.Add("Add numbers");
        if (!Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{}|;:,.<>?]"))
            weaknesses.Add("Add special characters");

        // Common patterns
        if (Regex.IsMatch(password, @"(.)\1{2,}"))
            weaknesses.Add("Avoid repeated characters");
        if (Regex.IsMatch(password, @"123|abc|qwerty", RegexOptions.IgnoreCase))
            weaknesses.Add("Avoid common sequences");

        var commonPasswords = new[] { "password", "123456", "qwerty", "admin", "letmein" };
        if (commonPasswords.Any(p => password.ToLower().Contains(p)))
            weaknesses.Add("Password contains common weak patterns");

        return Task.FromResult(JsonSerializer.Serialize(new {
            success = true,
            password_length = password.Length,
            strength = new {
                level = analysis.level,
                score = analysis.score,
                entropy_bits = analysis.entropy
            },
            character_analysis = new {
                has_uppercase = Regex.IsMatch(password, @"[A-Z]"),
                has_lowercase = Regex.IsMatch(password, @"[a-z]"),
                has_numbers = Regex.IsMatch(password, @"\d"),
                has_symbols = Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{}|;:,.<>?]")
            },
            weaknesses = weaknesses.Count > 0 ? weaknesses : null,
            estimated_crack_time = EstimateCrackTime(analysis.entropy)
        }));
    }

    private (string level, int score, double entropy) AnalyzePasswordStrength(string password)
    {
        var poolSize = 0;
        if (Regex.IsMatch(password, @"[a-z]")) poolSize += 26;
        if (Regex.IsMatch(password, @"[A-Z]")) poolSize += 26;
        if (Regex.IsMatch(password, @"\d")) poolSize += 10;
        if (Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{}|;:,.<>?]")) poolSize += 32;

        if (poolSize == 0) poolSize = 26;

        var entropy = password.Length * Math.Log2(poolSize);
        var score = (int)Math.Min(100, entropy * 1.5);

        var level = entropy switch
        {
            < 28 => "Very Weak",
            < 36 => "Weak",
            < 60 => "Fair",
            < 80 => "Strong",
            _ => "Very Strong"
        };

        return (level, score, Math.Round(entropy, 1));
    }

    private string EstimateCrackTime(double entropy)
    {
        // Assuming 10 billion attempts per second
        var attempts = Math.Pow(2, entropy);
        var seconds = attempts / 10_000_000_000;

        if (seconds < 1) return "Instant";
        if (seconds < 60) return $"{seconds:F0} seconds";
        if (seconds < 3600) return $"{seconds / 60:F0} minutes";
        if (seconds < 86400) return $"{seconds / 3600:F0} hours";
        if (seconds < 31536000) return $"{seconds / 86400:F0} days";
        if (seconds < 31536000000) return $"{seconds / 31536000:F0} years";
        return "Centuries+";
    }

    #endregion

    #region Hash Generation and Verification

    /// <summary>
    /// Hash text or file with various algorithms
    /// </summary>
    public async Task<string> HashAsync(Dictionary<string, object> args)
    {
        var input = GetString(args, "input");
        var algorithm = GetString(args, "algorithm", "sha256").ToLower();
        var encoding = GetString(args, "encoding", "hex"); // hex, base64

        byte[] inputBytes;
        string inputType;

        var filePath = ResolvePath(input);
        if (File.Exists(filePath))
        {
            inputBytes = await File.ReadAllBytesAsync(filePath);
            inputType = "file";
        }
        else
        {
            inputBytes = Encoding.UTF8.GetBytes(input);
            inputType = "text";
        }

        byte[] hashBytes;
        using HashAlgorithm hasher = algorithm switch
        {
            "md5" => MD5.Create(),
            "sha1" => SHA1.Create(),
            "sha256" => SHA256.Create(),
            "sha384" => SHA384.Create(),
            "sha512" => SHA512.Create(),
            _ => SHA256.Create()
        };

        hashBytes = hasher.ComputeHash(inputBytes);

        var hash = encoding == "base64"
            ? Convert.ToBase64String(hashBytes)
            : BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

        return JsonSerializer.Serialize(new {
            success = true,
            input_type = inputType,
            algorithm,
            encoding,
            hash,
            hash_length_bits = hashBytes.Length * 8
        });
    }

    /// <summary>
    /// Verify a hash against input
    /// </summary>
    public async Task<string> VerifyHashAsync(Dictionary<string, object> args)
    {
        var input = GetString(args, "input");
        var expectedHash = GetString(args, "expected_hash");
        var algorithm = GetString(args, "algorithm", "sha256").ToLower();

        byte[] inputBytes;
        var filePath = ResolvePath(input);
        if (File.Exists(filePath))
        {
            inputBytes = await File.ReadAllBytesAsync(filePath);
        }
        else
        {
            inputBytes = Encoding.UTF8.GetBytes(input);
        }

        byte[] hashBytes;
        using HashAlgorithm hasher = algorithm switch
        {
            "md5" => MD5.Create(),
            "sha1" => SHA1.Create(),
            "sha256" => SHA256.Create(),
            "sha384" => SHA384.Create(),
            "sha512" => SHA512.Create(),
            _ => SHA256.Create()
        };

        hashBytes = hasher.ComputeHash(inputBytes);
        var actualHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

        var match = actualHash.Equals(expectedHash.ToLower().Replace("-", ""));

        return JsonSerializer.Serialize(new {
            success = true,
            match,
            algorithm,
            actual_hash = actualHash,
            expected_hash = expectedHash
        });
    }

    #endregion

    #region Encryption/Decryption

    /// <summary>
    /// Encrypt text with AES
    /// </summary>
    public async Task<string> EncryptAesAsync(Dictionary<string, object> args)
    {
        var input = GetString(args, "input");
        var password = GetString(args, "password");
        var outputFile = GetString(args, "output_file", null);

        string content;
        var filePath = ResolvePath(input);
        if (File.Exists(filePath))
        {
            content = await File.ReadAllTextAsync(filePath);
        }
        else
        {
            content = input;
        }

        // Generate salt and key from password
        var salt = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        using var keyDerivation = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
        var key = keyDerivation.GetBytes(32);
        var iv = keyDerivation.GetBytes(16);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;

        using var encryptor = aes.CreateEncryptor();
        var inputBytes = Encoding.UTF8.GetBytes(content);
        var encryptedBytes = encryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);

        // Combine salt + encrypted data
        var result = new byte[salt.Length + encryptedBytes.Length];
        Buffer.BlockCopy(salt, 0, result, 0, salt.Length);
        Buffer.BlockCopy(encryptedBytes, 0, result, salt.Length, encryptedBytes.Length);

        var encrypted = Convert.ToBase64String(result);

        if (!string.IsNullOrEmpty(outputFile))
        {
            var outputPath = Path.Combine(_generatedPath, outputFile);
            await File.WriteAllTextAsync(outputPath, encrypted);
            return JsonSerializer.Serialize(new {
                success = true,
                output_file = outputPath,
                algorithm = "AES-256-CBC"
            });
        }

        return JsonSerializer.Serialize(new {
            success = true,
            encrypted,
            algorithm = "AES-256-CBC"
        });
    }

    /// <summary>
    /// Decrypt AES-encrypted text
    /// </summary>
    public async Task<string> DecryptAesAsync(Dictionary<string, object> args)
    {
        var input = GetString(args, "input");
        var password = GetString(args, "password");
        var outputFile = GetString(args, "output_file", null);

        string encryptedContent;
        var filePath = ResolvePath(input);
        if (File.Exists(filePath))
        {
            encryptedContent = await File.ReadAllTextAsync(filePath);
        }
        else
        {
            encryptedContent = input;
        }

        try
        {
            var fullData = Convert.FromBase64String(encryptedContent);

            // Extract salt (first 16 bytes)
            var salt = new byte[16];
            var encryptedBytes = new byte[fullData.Length - 16];
            Buffer.BlockCopy(fullData, 0, salt, 0, 16);
            Buffer.BlockCopy(fullData, 16, encryptedBytes, 0, encryptedBytes.Length);

            // Derive key from password + salt
            using var keyDerivation = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
            var key = keyDerivation.GetBytes(32);
            var iv = keyDerivation.GetBytes(16);

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            var decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
            var decrypted = Encoding.UTF8.GetString(decryptedBytes);

            if (!string.IsNullOrEmpty(outputFile))
            {
                var outputPath = Path.Combine(_generatedPath, outputFile);
                await File.WriteAllTextAsync(outputPath, decrypted);
                return JsonSerializer.Serialize(new {
                    success = true,
                    output_file = outputPath
                });
            }

            return JsonSerializer.Serialize(new {
                success = true,
                decrypted
            });
        }
        catch (CryptographicException)
        {
            return JsonSerializer.Serialize(new {
                success = false,
                error = "Decryption failed. Wrong password or corrupted data."
            });
        }
    }

    #endregion

    #region Token Generation

    /// <summary>
    /// Generate secure random tokens
    /// </summary>
    public Task<string> GenerateTokenAsync(Dictionary<string, object> args)
    {
        var length = GetInt(args, "length", 32);
        var format = GetString(args, "format", "hex"); // hex, base64, alphanumeric
        var count = GetInt(args, "count", 1);

        var tokens = new List<string>();

        for (int i = 0; i < count; i++)
        {
            var bytes = new byte[length];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }

            var token = format switch
            {
                "base64" => Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('='),
                "alphanumeric" => GenerateAlphanumericToken(length),
                _ => BitConverter.ToString(bytes).Replace("-", "").ToLower()
            };

            tokens.Add(token);
        }

        return Task.FromResult(JsonSerializer.Serialize(new {
            success = true,
            format,
            length,
            tokens = count == 1 ? (object)tokens[0] : tokens
        }));
    }

    private string GenerateAlphanumericToken(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var token = new char[length];
        var bytes = new byte[length];

        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }

        for (int i = 0; i < length; i++)
        {
            token[i] = chars[bytes[i] % chars.Length];
        }

        return new string(token);
    }

    #endregion

    #region JWT Tools

    /// <summary>
    /// Decode JWT token (without verification)
    /// </summary>
    public Task<string> DecodeJwtAsync(Dictionary<string, object> args)
    {
        var token = GetString(args, "token");

        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3)
            {
                return Task.FromResult(JsonSerializer.Serialize(new {
                    success = false,
                    error = "Invalid JWT format. Expected 3 parts separated by dots."
                }));
            }

            var header = DecodeBase64Url(parts[0]);
            var payload = DecodeBase64Url(parts[1]);

            var headerObj = JsonSerializer.Deserialize<JsonElement>(header);
            var payloadObj = JsonSerializer.Deserialize<JsonElement>(payload);

            // Check expiration
            var isExpired = false;
            var expiresAt = (DateTime?)null;
            if (payloadObj.TryGetProperty("exp", out var expElement))
            {
                var exp = expElement.GetInt64();
                expiresAt = DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime;
                isExpired = expiresAt < DateTime.UtcNow;
            }

            return Task.FromResult(JsonSerializer.Serialize(new {
                success = true,
                header = headerObj,
                payload = payloadObj,
                signature = parts[2],
                validation = new {
                    is_expired = isExpired,
                    expires_at = expiresAt?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    warning = "This decodes without verifying the signature"
                }
            }));
        }
        catch (Exception ex)
        {
            return Task.FromResult(JsonSerializer.Serialize(new {
                success = false,
                error = $"Failed to decode JWT: {ex.Message}"
            }));
        }
    }

    private string DecodeBase64Url(string input)
    {
        var base64 = input.Replace('-', '+').Replace('_', '/');
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        var bytes = Convert.FromBase64String(base64);
        return Encoding.UTF8.GetString(bytes);
    }

    #endregion

    #region IP/Network Tools

    /// <summary>
    /// Validate and analyze IP address
    /// </summary>
    public Task<string> AnalyzeIpAsync(Dictionary<string, object> args)
    {
        var ip = GetString(args, "ip");

        if (!System.Net.IPAddress.TryParse(ip, out var ipAddress))
        {
            return Task.FromResult(JsonSerializer.Serialize(new {
                success = false,
                error = "Invalid IP address format"
            }));
        }

        var isIPv4 = ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork;
        var bytes = ipAddress.GetAddressBytes();

        var isPrivate = false;
        var isLoopback = System.Net.IPAddress.IsLoopback(ipAddress);
        var ipClass = "Unknown";

        if (isIPv4)
        {
            // Check private ranges
            isPrivate = (bytes[0] == 10) ||
                       (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
                       (bytes[0] == 192 && bytes[1] == 168) ||
                       (bytes[0] == 127);

            // Determine class
            if (bytes[0] < 128) ipClass = "A";
            else if (bytes[0] < 192) ipClass = "B";
            else if (bytes[0] < 224) ipClass = "C";
            else if (bytes[0] < 240) ipClass = "D (Multicast)";
            else ipClass = "E (Reserved)";
        }

        return Task.FromResult(JsonSerializer.Serialize(new {
            success = true,
            ip,
            version = isIPv4 ? "IPv4" : "IPv6",
            ip_class = isIPv4 ? ipClass : "N/A",
            is_private = isPrivate,
            is_loopback = isLoopback,
            binary = isIPv4 ? string.Join(".", bytes.Select(b => Convert.ToString(b, 2).PadLeft(8, '0'))) : null,
            hex = BitConverter.ToString(bytes).Replace("-", ":")
        }));
    }

    /// <summary>
    /// Calculate subnet information
    /// </summary>
    public Task<string> CalculateSubnetAsync(Dictionary<string, object> args)
    {
        var cidr = GetString(args, "cidr"); // e.g., "192.168.1.0/24"

        var parts = cidr.Split('/');
        if (parts.Length != 2 || !System.Net.IPAddress.TryParse(parts[0], out var ip) || !int.TryParse(parts[1], out var prefix))
        {
            return Task.FromResult(JsonSerializer.Serialize(new {
                success = false,
                error = "Invalid CIDR notation. Use format like 192.168.1.0/24"
            }));
        }

        if (prefix < 0 || prefix > 32)
        {
            return Task.FromResult(JsonSerializer.Serialize(new {
                success = false,
                error = "Prefix must be between 0 and 32"
            }));
        }

        var bytes = ip.GetAddressBytes();
        var ipValue = (uint)(bytes[0] << 24 | bytes[1] << 16 | bytes[2] << 8 | bytes[3]);

        var mask = prefix == 0 ? 0 : ~((1u << (32 - prefix)) - 1);
        var network = ipValue & mask;
        var broadcast = network | ~mask;
        var hostCount = (1u << (32 - prefix)) - 2;
        if (prefix >= 31) hostCount = prefix == 32 ? 1u : 2u;

        var firstHost = network + 1;
        var lastHost = broadcast - 1;

        return Task.FromResult(JsonSerializer.Serialize(new {
            success = true,
            cidr,
            network_address = UintToIp(network),
            broadcast_address = UintToIp(broadcast),
            subnet_mask = UintToIp(mask),
            first_host = UintToIp(firstHost),
            last_host = UintToIp(lastHost),
            total_hosts = hostCount,
            prefix_length = prefix
        }));
    }

    private string UintToIp(uint value)
    {
        return $"{(value >> 24) & 0xFF}.{(value >> 16) & 0xFF}.{(value >> 8) & 0xFF}.{value & 0xFF}";
    }

    #endregion

    #region Validation Tools

    /// <summary>
    /// Validate email format
    /// </summary>
    public Task<string> ValidateEmailAsync(Dictionary<string, object> args)
    {
        var email = GetString(args, "email");

        var emailRegex = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
        var isValid = emailRegex.IsMatch(email);

        var issues = new List<string>();
        if (!email.Contains("@"))
            issues.Add("Missing @ symbol");
        if (!email.Contains("."))
            issues.Add("Missing domain extension");
        if (email.StartsWith(".") || email.EndsWith("."))
            issues.Add("Cannot start or end with period");
        if (email.Contains(".."))
            issues.Add("Consecutive periods not allowed");

        var parts = email.Split('@');
        string? domain = null;
        if (parts.Length == 2)
        {
            domain = parts[1];
        }

        return Task.FromResult(JsonSerializer.Serialize(new {
            success = true,
            email,
            is_valid = isValid,
            domain,
            issues = issues.Count > 0 ? issues : null
        }));
    }

    /// <summary>
    /// Validate credit card number (Luhn algorithm)
    /// </summary>
    public Task<string> ValidateCreditCardAsync(Dictionary<string, object> args)
    {
        var number = GetString(args, "number").Replace(" ", "").Replace("-", "");

        if (!Regex.IsMatch(number, @"^\d+$"))
        {
            return Task.FromResult(JsonSerializer.Serialize(new {
                success = false,
                error = "Card number must contain only digits"
            }));
        }

        // Luhn algorithm
        var sum = 0;
        var alternate = false;

        for (int i = number.Length - 1; i >= 0; i--)
        {
            var digit = number[i] - '0';

            if (alternate)
            {
                digit *= 2;
                if (digit > 9)
                    digit -= 9;
            }

            sum += digit;
            alternate = !alternate;
        }

        var isValid = sum % 10 == 0;

        // Detect card type
        var cardType = "Unknown";
        if (Regex.IsMatch(number, @"^4")) cardType = "Visa";
        else if (Regex.IsMatch(number, @"^5[1-5]")) cardType = "Mastercard";
        else if (Regex.IsMatch(number, @"^3[47]")) cardType = "American Express";
        else if (Regex.IsMatch(number, @"^6(?:011|5)")) cardType = "Discover";
        else if (Regex.IsMatch(number, @"^(?:2131|1800|35)")) cardType = "JCB";

        return Task.FromResult(JsonSerializer.Serialize(new {
            success = true,
            is_valid = isValid,
            card_type = cardType,
            length = number.Length,
            masked = $"****-****-****-{number.Substring(Math.Max(0, number.Length - 4))}"
        }));
    }

    #endregion

    #region Helper Methods

    private string ResolvePath(string path)
    {
        if (string.IsNullOrEmpty(path) || Path.IsPathRooted(path))
            return path;

        var generatedPath = Path.Combine(_generatedPath, path);
        if (File.Exists(generatedPath))
            return generatedPath;

        return path;
    }

    private static string GetString(Dictionary<string, object> args, string key, string? defaultValue = null)
    {
        if (args.TryGetValue(key, out var value))
        {
            if (value is JsonElement je)
            {
                return je.ValueKind == JsonValueKind.String ? je.GetString() ?? defaultValue ?? "" : je.ToString();
            }
            return value?.ToString() ?? defaultValue ?? "";
        }
        return defaultValue ?? "";
    }

    private static int GetInt(Dictionary<string, object> args, string key, int defaultValue = 0)
    {
        if (args.TryGetValue(key, out var value))
        {
            if (value is JsonElement je)
            {
                return je.ValueKind == JsonValueKind.Number ? je.GetInt32() : defaultValue;
            }
            if (int.TryParse(value?.ToString(), out var result))
                return result;
        }
        return defaultValue;
    }

    private static bool GetBool(Dictionary<string, object> args, string key, bool defaultValue = false)
    {
        if (args.TryGetValue(key, out var value))
        {
            if (value is JsonElement je)
            {
                return je.ValueKind == JsonValueKind.True ||
                       (je.ValueKind == JsonValueKind.String && je.GetString()?.ToLower() == "true");
            }
            if (bool.TryParse(value?.ToString(), out var result))
                return result;
        }
        return defaultValue;
    }

    #endregion
}
