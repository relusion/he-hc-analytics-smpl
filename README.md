# Homomorphic Encryption POC for Privacy-Preserving Healthcare

This repository contains a simple proof-of-concept project demonstrating how to apply Homomorphic Encryption (HE) in a healthcare scenario using [Microsoft SEAL](https://github.com/microsoft/SEAL). It encrypts patient data (e.g., glucose, BMI) locally, runs an approximate risk prediction model on the encrypted data, and then decrypts only the final result—helping to protect sensitive information end-to-end.

## Contents

- **`DiabetesRiskPredictor.cs`**: Core class handling key generation, encryption, homomorphic operations (multiplication, rotation, addition, rescaling), and final decryption.
- **`Program.cs`**: Console application showcasing how to use `DiabetesRiskPredictor` with sample inputs for normal, borderline, and high risk scenarios.
- **Article Reference**: [Privacy-Preserving Healthcare with Homomorphic Encryption](https://medium.com/@vgondarev/privacy-preserving-healthcare-with-homomorphic-encryption-1128f1f1abcf)

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet) (8.0 should work as well)
- [Microsoft SEAL NuGet package](https://www.nuget.org/packages/Microsoft.Research.SEALNet/)

## Getting Started

1. **Clone the repository**  

   ```bash
   git clone https://github.com/relusion/he-hc-analytics-smpl
   cd he-hc-analytics-smpl/HealthcareAnalytics
   ```

2. **Restore NuGet packages**  

   ```bash
   dotnet restore
   ```

3. **Build and run**  

   ```bash
   dotnet run
   ```

You should see console output showing approximate risk scores for different glucose/BMI inputs. Here is a typical result:

```
Approx risk for (g=70, bmi=20) => 0.1000
Approx risk for (g=120, bmi=28) => 0.5000
Approx risk for (g=140, bmi=35) => 0.8000
```

## Disclaimer

This proof-of-concept software is for demonstration and educational purposes only. It is not designed or intended for clinical use, nor is it approved to diagnose, treat, or cure any medical condition. All outputs, analyses, or insights derived from this code should be verified with qualified healthcare professionals. Users assume full responsibility for any actions taken based on its results.
