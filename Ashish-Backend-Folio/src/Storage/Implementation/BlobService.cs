using Ashish_Backend_Folio.Storage.Interface;
using Azure.Storage.Blobs;


namespace Ashish_Backend_Folio.Storage.Implementation
{
    public class BlobService : IBlobService
    {
        private readonly BlobServiceClient _client;
        public BlobService(BlobServiceClient client) => _client = client;

        public async Task<string> UploadAsync(Stream stream, string container, string filename)
        {
            var containerClient = _client.GetBlobContainerClient(container);
            await containerClient.CreateIfNotExistsAsync();
            var blobClient = containerClient.GetBlobClient(filename);
            await blobClient.UploadAsync(stream, overwrite: true);
            return blobClient.Uri.ToString();
        }

        public async Task<Stream> DownloadAsync(string container, string filename)
        {
            var blobClient = _client.GetBlobContainerClient(container).GetBlobClient(filename);
            var resp = await blobClient.DownloadAsync();
            return resp.Value.Content;
        }
    }

}
