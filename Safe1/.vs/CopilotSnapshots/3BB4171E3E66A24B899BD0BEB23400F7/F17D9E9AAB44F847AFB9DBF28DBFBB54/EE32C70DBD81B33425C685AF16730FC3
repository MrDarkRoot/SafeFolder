#include "pch.h"
#include <bcrypt.h>
#include <vector>
#include <string>
#include <memory>
#include <fstream>

#pragma comment(lib, "bcrypt.lib")

extern "C" __declspec(dllexport)
int TestPInvokeConnection(int value)
{
    return value + 99;
}

static bool NT_SUCCESS_CHECK(NTSTATUS status)
{
    return BCRYPT_SUCCESS(status);
}

// Simple helper: perform HMAC-SHA256 using CNG (BCrypt). key and data are byte buffers.
static bool HmacSha256(const unsigned char* key, int keyLen, const unsigned char* data, int dataLen, unsigned char* outMac, int outMacLen)
{
    if (outMacLen < 32) return false;

    BCRYPT_ALG_HANDLE hAlg = NULL;
    BCRYPT_HASH_HANDLE hHash = NULL;
    NTSTATUS status = BCryptOpenAlgorithmProvider(&hAlg, BCRYPT_SHA256_ALGORITHM, NULL, 0);
    if (!NT_SUCCESS_CHECK(status)) return false;

    // Create an HMAC hash by passing the secret key to BCryptCreateHash
    status = BCryptCreateHash(hAlg, &hHash, NULL, 0, (PUCHAR)key, (ULONG)keyLen, 0);
    if (!NT_SUCCESS_CHECK(status))
    {
        BCryptCloseAlgorithmProvider(hAlg, 0);
        return false;
    }

    status = BCryptHashData(hHash, (PUCHAR)data, (ULONG)dataLen, 0);
    if (!NT_SUCCESS_CHECK(status))
    {
        BCryptDestroyHash(hHash);
        BCryptCloseAlgorithmProvider(hAlg, 0);
        return false;
    }

    ULONG resultLen = 0;
    // Query result length
    status = BCryptFinishHash(hHash, outMac, 32, 0);
    BCryptDestroyHash(hHash);
    BCryptCloseAlgorithmProvider(hAlg, 0);
    return NT_SUCCESS_CHECK(status);
}

// PBKDF2-HMAC-SHA256 implementation
extern "C" __declspec(dllexport)
int PBKDF2_Derive(const char* password, const unsigned char* salt, int saltLen, int iterations, unsigned char* outKey, int outKeyLen)
{
    if (!password || !salt || saltLen <= 0 || !outKey || outKeyLen <= 0) return -1;
    if (iterations <= 0) iterations = 310000; // default

    const unsigned char* P = reinterpret_cast<const unsigned char*>(password);
    int Plen = (int)strlen(password);

    const int hLen = 32; // SHA256 output size
    int l = (outKeyLen + hLen - 1) / hLen;
    int r = outKeyLen - (l - 1) * hLen;

    std::vector<unsigned char> U(hLen);
    std::vector<unsigned char> T(hLen);

    std::vector<unsigned char> intBlock(4);
    std::vector<unsigned char> saltInt(saltLen + 4);

    for (int i = 1; i <= l; ++i)
    {
        // salt || INT(i)
        memcpy(saltInt.data(), salt, saltLen);
        intBlock[0] = (unsigned char)((i >> 24) & 0xFF);
        intBlock[1] = (unsigned char)((i >> 16) & 0xFF);
        intBlock[2] = (unsigned char)((i >> 8) & 0xFF);
        intBlock[3] = (unsigned char)(i & 0xFF);
        memcpy(saltInt.data() + saltLen, intBlock.data(), 4);

        // U1 = PRF(P, salt || INT(i))
        if (!HmacSha256(P, Plen, saltInt.data(), (int)saltInt.size(), U.data(), hLen)) return -2;
        memcpy(T.data(), U.data(), hLen);

        for (int j = 1; j < iterations; ++j)
        {
            if (!HmacSha256(P, Plen, U.data(), hLen, U.data(), hLen)) return -3;
            for (int k = 0; k < hLen; ++k) T[k] ^= U[k];
        }

        int destPos = (i - 1) * hLen;
        int copyLen = (i == l) ? r : hLen;
        memcpy(outKey + destPos, T.data(), copyLen);
    }

    return 0;
}

// AES-256-GCM file encryption. Output file will contain ciphertext followed by 16-byte tag.
// inPath/outPath are wide strings (UTF-16). key is expected to be 32 bytes. iv is provided by caller.
extern "C" __declspec(dllexport)
int EncryptFileAesGcm(const wchar_t* inPath, const wchar_t* outPath, const unsigned char* key, int keyLen, const unsigned char* iv, int ivLen)
{
    if (!inPath || !outPath || !key || keyLen != 32 || !iv || ivLen <= 0) return -1;

    // Read input file
    std::ifstream ifs(inPath, std::ios::binary);
    if (!ifs) return -2;
    std::vector<unsigned char> plaintext((std::istreambuf_iterator<char>(ifs)), std::istreambuf_iterator<char>());
    ifs.close();

    BCRYPT_ALG_HANDLE hAlg = NULL;
    BCRYPT_KEY_HANDLE hKey = NULL;
    NTSTATUS status = BCryptOpenAlgorithmProvider(&hAlg, BCRYPT_AES_ALGORITHM, NULL, 0);
    if (!NT_SUCCESS_CHECK(status)) return -3;

    // Create symmetric key
    status = BCryptGenerateSymmetricKey(hAlg, &hKey, NULL, 0, (PUCHAR)key, (ULONG)keyLen, 0);
    if (!NT_SUCCESS_CHECK(status))
    {
        BCryptCloseAlgorithmProvider(hAlg, 0);
        return -4;
    }

    // Prepare auth info
    BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO authInfo;
    BCRYPT_INIT_AUTH_MODE_INFO(authInfo);
    authInfo.pbNonce = (PUCHAR)iv;
    authInfo.cbNonce = (ULONG)ivLen;

    const ULONG tagLen = 16;
    std::vector<unsigned char> tag(tagLen);
    authInfo.pbTag = tag.data();
    authInfo.cbTag = tagLen;
    authInfo.pbAuthData = nullptr;
    authInfo.cbAuthData = 0;

    // Determine output size
    ULONG cipherSize = 0;
    status = BCryptEncrypt(hKey, plaintext.empty() ? nullptr : plaintext.data(), (ULONG)plaintext.size(), &authInfo, NULL, 0, NULL, 0, &cipherSize, 0);
    if (!NT_SUCCESS_CHECK(status))
    {
        BCryptDestroyKey(hKey);
        BCryptCloseAlgorithmProvider(hAlg, 0);
        return -5;
    }

    std::vector<unsigned char> ciphertext(cipherSize);
    ULONG resultSize = 0;
    status = BCryptEncrypt(hKey, plaintext.empty() ? nullptr : plaintext.data(), (ULONG)plaintext.size(), &authInfo, NULL, 0, ciphertext.data(), cipherSize, &resultSize, 0);

    // On success, authInfo.pbTag is filled with tag
    if (!NT_SUCCESS_CHECK(status))
    {
        BCryptDestroyKey(hKey);
        BCryptCloseAlgorithmProvider(hAlg, 0);
        return -6;
    }

    // Write ciphertext + tag to output file
    std::ofstream ofs(outPath, std::ios::binary | std::ios::trunc);
    if (!ofs)
    {
        BCryptDestroyKey(hKey);
        BCryptCloseAlgorithmProvider(hAlg, 0);
        return -7;
    }
    ofs.write(reinterpret_cast<const char*>(ciphertext.data()), resultSize);
    ofs.write(reinterpret_cast<const char*>(tag.data()), tagLen);
    ofs.close();

    BCryptDestroyKey(hKey);
    BCryptCloseAlgorithmProvider(hAlg, 0);
    return 0;
}

// AES-256-GCM file decryption. Expects input file to contain ciphertext followed by 16-byte tag.
extern "C" __declspec(dllexport)
int DecryptFileAesGcm(const wchar_t* inPath, const wchar_t* outPath, const unsigned char* key, int keyLen, const unsigned char* iv, int ivLen)
{
    if (!inPath || !outPath || !key || keyLen != 32 || !iv || ivLen <= 0) return -1;

    // Read input file
    std::ifstream ifs(inPath, std::ios::binary);
    if (!ifs) return -2;
    std::vector<unsigned char> fileData((std::istreambuf_iterator<char>(ifs)), std::istreambuf_iterator<char>());
    ifs.close();

    const ULONG tagLen = 16;
    if (fileData.size() < tagLen) return -3;
    size_t cipherSize = fileData.size() - tagLen;
    unsigned char* tagPtr = fileData.data() + cipherSize;

    BCRYPT_ALG_HANDLE hAlg = NULL;
    BCRYPT_KEY_HANDLE hKey = NULL;
    NTSTATUS status = BCryptOpenAlgorithmProvider(&hAlg, BCRYPT_AES_ALGORITHM, NULL, 0);
    if (!NT_SUCCESS_CHECK(status)) return -4;

    // Create symmetric key
    status = BCryptGenerateSymmetricKey(hAlg, &hKey, NULL, 0, (PUCHAR)key, (ULONG)keyLen, 0);
    if (!NT_SUCCESS_CHECK(status))
    {
        BCryptCloseAlgorithmProvider(hAlg, 0);
        return -5;
    }

    BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO authInfo;
    BCRYPT_INIT_AUTH_MODE_INFO(authInfo);
    authInfo.pbNonce = (PUCHAR)iv;
    authInfo.cbNonce = (ULONG)ivLen;
    authInfo.pbTag = tagPtr;
    authInfo.cbTag = tagLen;
    authInfo.pbAuthData = nullptr;
    authInfo.cbAuthData = 0;

    // Determine plaintext size
    ULONG plainSize = 0;
    status = BCryptDecrypt(hKey, cipherSize == 0 ? nullptr : fileData.data(), (ULONG)cipherSize, &authInfo, NULL, 0, NULL, 0, &plainSize, 0);
    if (!NT_SUCCESS_CHECK(status))
    {
        BCryptDestroyKey(hKey);
        BCryptCloseAlgorithmProvider(hAlg, 0);
        return -6;
    }

    std::vector<unsigned char> plaintext(plainSize);
    ULONG resultSize = 0;
    status = BCryptDecrypt(hKey, cipherSize == 0 ? nullptr : fileData.data(), (ULONG)cipherSize, &authInfo, NULL, 0, plaintext.data(), plainSize, &resultSize, 0);

    if (!NT_SUCCESS_CHECK(status))
    {
        // Authentication tag mismatch will return a specific error code
        BCryptDestroyKey(hKey);
        BCryptCloseAlgorithmProvider(hAlg, 0);
        return -7;
    }

    // Write plaintext to output
    std::ofstream ofs(outPath, std::ios::binary | std::ios::trunc);
    if (!ofs)
    {
        BCryptDestroyKey(hKey);
        BCryptCloseAlgorithmProvider(hAlg, 0);
        return -8;
    }
    ofs.write(reinterpret_cast<const char*>(plaintext.data()), resultSize);
    ofs.close();

    BCryptDestroyKey(hKey);
    BCryptCloseAlgorithmProvider(hAlg, 0);
    return 0;
}