using Microsoft.Research.SEAL;

namespace HealthcareAnalytics;
public class DiabetesRiskPredictor : IDisposable
{
    private SEALContext context;
    private PublicKey publicKey;
    private SecretKey secretKey;
    private RelinKeys relinKeys;
    private GaloisKeys galoisKeys;
    private Encryptor encryptor;
    private Decryptor decryptor;
    private Evaluator evaluator;
    private CKKSEncoder encoder;
    private double scale;

    // Calibrated weights to produce more realistic probabilities
    //   for normal & elevated glucose/BMI in this toy example.
    //   [bias, w_glucose, w_bmi]
    private List<double> weights = new List<double>
        {
            -10.2735,  // bias (beta_0)
            0.01685,   // w_glucose
            0.2947     // w_bmi
        };

    public DiabetesRiskPredictor(ulong polyModulusDegree = 8192)
    {
        var parms = new EncryptionParameters(SchemeType.CKKS)
        {
            PolyModulusDegree = polyModulusDegree,
            CoeffModulus = CoeffModulus.Create(polyModulusDegree, new int[] { 60, 40, 40, 60 })
        };

        context = new SEALContext(parms);

        using KeyGenerator keygen = new KeyGenerator(context);
        secretKey = keygen.SecretKey;
        keygen.CreatePublicKey(out publicKey);
        keygen.CreateRelinKeys(out relinKeys);
        keygen.CreateGaloisKeys(out galoisKeys);

        encryptor = new Encryptor(context, publicKey);
        decryptor = new Decryptor(context, secretKey);
        evaluator = new Evaluator(context);
        encoder = new CKKSEncoder(context);

        // Scale of 2^40
        scale = Math.Pow(2.0, 40);
    }

    public Ciphertext EncryptPatientData(double glucose, double bmi)
    {
        var features = new List<double> { glucose, bmi };
        Plaintext plainFeatures = new Plaintext();
        encoder.Encode(features, scale, plainFeatures);

        Ciphertext encryptedFeatures = new Ciphertext();
        encryptor.Encrypt(plainFeatures, encryptedFeatures);
        return encryptedFeatures;
    }

    public Ciphertext PredictLinearRisk(Ciphertext encryptedFeatures)
    {
        // 1) Encode [w_glucose, w_bmi] and encrypt
        Plaintext wPlain = new Plaintext();
        encoder.Encode(new List<double> { weights[1], weights[2] }, scale, wPlain);
        Ciphertext wCipher = new Ciphertext();
        encryptor.Encrypt(wPlain, wCipher);

        // 2) Multiply elementwise => [glucose*w_glucose, bmi*w_bmi]
        Ciphertext productCipher = new Ciphertext();
        evaluator.Multiply(encryptedFeatures, wCipher, productCipher);
        evaluator.RelinearizeInplace(productCipher, relinKeys);
        evaluator.RescaleToNextInplace(productCipher);
        productCipher.Scale = scale;

        // 3) Rotate slot 1 -> slot 0, add => slot 0 = glucose*w_g + bmi*w_b
        Ciphertext rotatedCipher = new Ciphertext();
        evaluator.RotateVector(productCipher, 1, galoisKeys, rotatedCipher);
        evaluator.AddInplace(productCipher, rotatedCipher);

        // 4) Add bias
        Plaintext biasPlain = new Plaintext();
        encoder.Encode(new List<double> { weights[0] }, scale, biasPlain);
        Ciphertext biasCipher = new Ciphertext();
        encryptor.Encrypt(biasPlain, biasCipher);

        // Ensure same level
        evaluator.ModSwitchToInplace(biasCipher, productCipher.ParmsId);

        Ciphertext linearRisk = new Ciphertext();
        evaluator.Add(productCipher, biasCipher, linearRisk);

        return linearRisk;
    }

    /// <summary>
    /// Very rough polynomial:  sigmoid_approx(x) = 0.5 + 0.125 * x
    /// </summary>
    public Ciphertext ApplySigmoidApprox(Ciphertext encryptedX)
    {
        // (1) Multiply by 0.125
        double xScale = encryptedX.Scale;
        Plaintext multPlain = new Plaintext();
        encoder.Encode(0.125, xScale, multPlain);
        evaluator.ModSwitchToInplace(multPlain, encryptedX.ParmsId);

        Ciphertext scaledX = new Ciphertext();
        evaluator.MultiplyPlain(encryptedX, multPlain, scaledX);
        evaluator.RelinearizeInplace(scaledX, relinKeys);
        evaluator.RescaleToNextInplace(scaledX);
        scaledX.Scale = scale;

        // (2) Add 0.5
        Plaintext halfPlain = new Plaintext();
        encoder.Encode(0.5, scale, halfPlain);
        Ciphertext halfCipher = new Ciphertext();
        encryptor.Encrypt(halfPlain, halfCipher);

        evaluator.ModSwitchToInplace(halfCipher, scaledX.ParmsId);

        Ciphertext sigmoidResult = new Ciphertext();
        evaluator.Add(scaledX, halfCipher, sigmoidResult);

        return sigmoidResult;
    }

    public Ciphertext PredictRiskWithSigmoid(Ciphertext encryptedFeatures)
    {
        var linearRisk = PredictLinearRisk(encryptedFeatures);
        var approximateSigmoid = ApplySigmoidApprox(linearRisk);
        return approximateSigmoid;
    }

    public double DecryptRiskScore(Ciphertext encryptedRisk)
    {
        Plaintext plainResult = new Plaintext();
        decryptor.Decrypt(encryptedRisk, plainResult);

        List<double> decoded = new List<double>();
        encoder.Decode(plainResult, decoded);

        return decoded[0];
    }

    public void Dispose()
    {
        context?.Dispose();
        publicKey?.Dispose();
        secretKey?.Dispose();
        relinKeys?.Dispose();
        galoisKeys?.Dispose();
        encryptor?.Dispose();
        decryptor?.Dispose();
        evaluator?.Dispose();
        encoder?.Dispose();
    }
}
