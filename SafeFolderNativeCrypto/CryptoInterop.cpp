#include "CryptoInterop.h"
#include <iostream>
#include <fstream>

__declspec(dllexport) bool EncryptFile(
    const wchar_t* inputPath,
    const wchar_t* outputPath,
    const unsigned char* key,
    int keySize)
{

    std::wcout << L"Encrypting file: " << inputPath << std::endl;
    return true;
}

__declspec(dllexport) bool DeriveKeyPBKDF2(
    const char* password,
    const unsigned char* salt,
    int saltSize,
    int iterations,
    unsigned char* outputKey,
    int outputKeySize)
{
    std::cout << "Deriving key for password: " << password << std::endl;
    return true;
}