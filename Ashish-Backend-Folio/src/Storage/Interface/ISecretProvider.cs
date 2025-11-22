namespace Ashish_Backend_Folio.Storage.Interface
{
    public interface ISecretProvider
    {
        Task<string> GetSecretAsync(string name);

    }
}
