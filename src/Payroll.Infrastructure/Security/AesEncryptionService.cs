using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Payroll.Domain.Interfaces;

namespace Payroll.Infrastructure.Security;

// AES-256-GCM for PAN, Aadhaar, bank account. Key from Docker secrets / env.
internal sealed class AesEncryptionService : IEncryptionService
{
    private readonly byte[] _key;

    public AesEncryptionService(IConfiguration configuration)
    {
        string? keyBase64 = configuration["Encryption:Key"];
        if (string.IsNullOrEmpty(keyBase64))
            throw new InvalidOperationException("Encryption:Key not configured.");
        _key = Convert.FromBase64String(keyBase64);
        if (_key.Length != 32)
            throw new InvalidOperationException("Encryption key must be 32 bytes (AES-256).");
    }

    public string Encrypt(string plaintext)
    {
        byte[] nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
        RandomNumberGenerator.Fill(nonce);

        byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        byte[] ciphertext = new byte[plaintextBytes.Length];
        byte[] tag = new byte[AesGcm.TagByteSizes.MaxSize];

        using AesGcm aes = new(_key, AesGcm.TagByteSizes.MaxSize);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        byte[] result = new byte[nonce.Length + tag.Length + ciphertext.Length];
        nonce.CopyTo(result, 0);
        tag.CopyTo(result, nonce.Length);
        ciphertext.CopyTo(result, nonce.Length + tag.Length);

        return Convert.ToBase64String(result);
    }

    public string Decrypt(string ciphertextBase64)
    {
        byte[] data = Convert.FromBase64String(ciphertextBase64);
        int nonceSize = AesGcm.NonceByteSizes.MaxSize;
        int tagSize = AesGcm.TagByteSizes.MaxSize;

        byte[] nonce = data[..nonceSize];
        byte[] tag = data[nonceSize..(nonceSize + tagSize)];
        byte[] ciphertext = data[(nonceSize + tagSize)..];
        byte[] plaintext = new byte[ciphertext.Length];

        using AesGcm aes = new(_key, AesGcm.TagByteSizes.MaxSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }
}
