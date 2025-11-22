namespace Ashish_Backend_Folio.Storage.Models
{
    public record JwtOptions
    {
        public string Issuer { get; init; }
        public string Audience { get; init; }
        public string SigningKeySecretName { get; init; }
    }
}
