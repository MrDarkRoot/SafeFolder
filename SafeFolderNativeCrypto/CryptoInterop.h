#pragma once

#ifdef __cplusplus
extern "C" {
#endif

    __declspec(dllexport) bool EncryptFile(
        const wchar_t* inputPath,
        const wchar_t* outputPath,
        const unsigned char* key,
        int keySize);

 
    __declspec(dllexport) bool DeriveKeyPBKDF2(
        const char* password,
        const unsigned char* salt,
        int saltSize,
        int iterations,
        unsigned char* outputKey,
        int outputKeySize);

#ifdef __cplusplus
}
#endif