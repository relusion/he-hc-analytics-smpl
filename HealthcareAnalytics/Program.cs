using Microsoft.Research.SEAL;

namespace HealthcareAnalytics
{   
    class Program
    {
        static void Main(string[] args)
        {
            using var predictor = new DiabetesRiskPredictor();

            // Example 1: normal
            double glucose1 = 70.0;
            double bmi1 = 20.0;
            var enc1 = predictor.EncryptPatientData(glucose1, bmi1);
            var riskCipher1 = predictor.PredictRiskWithSigmoid(enc1);
            double risk1 = predictor.DecryptRiskScore(riskCipher1);
            Console.WriteLine($"Approx risk for (g=70, bmi=20) => {risk1:F4}");

            // Example 2: borderline
            double glucose2 = 120.0;
            double bmi2 = 28.0;
            var enc2 = predictor.EncryptPatientData(glucose2, bmi2);
            var riskCipher2 = predictor.PredictRiskWithSigmoid(enc2);
            double risk2 = predictor.DecryptRiskScore(riskCipher2);
            Console.WriteLine($"Approx risk for (g=120, bmi=28) => {risk2:F4}");

            // Example 3: high
            double glucose3 = 140.0;
            double bmi3 = 35.0;
            var enc3 = predictor.EncryptPatientData(glucose3, bmi3);
            var riskCipher3 = predictor.PredictRiskWithSigmoid(enc3);
            double risk3 = predictor.DecryptRiskScore(riskCipher3);
            Console.WriteLine($"Approx risk for (g=140, bmi=35) => {risk3:F4}");
        }
    }
}