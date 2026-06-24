namespace RunCrypt.Models
{
    public enum CryptoAlgorithm
    {
        AesGcm256,
        ChaCha20Poly1305,
        Experimental_Base64,
        Experimental_NotCipher,
        Experimental_XorCascade
    }

    public enum AppLanguage { RU, EN }
    public enum AppTheme { Light, Dark }
}