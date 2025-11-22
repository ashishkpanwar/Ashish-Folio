namespace Ashish_Backend_Folio.Storage.Interface
{
    public interface IBlobService
    {
        Task<string> UploadAsync(Stream stream, string container, string filename);
        Task<Stream> DownloadAsync(string container, string filename);
    }
}
