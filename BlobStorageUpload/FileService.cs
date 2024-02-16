using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Azure.Storage.Queues;
using Azure.Storage.Blobs.Models;
using System.Text.Json;

namespace BlobStorageUpload;

public class FileService
{
    private readonly string _storageAccount;
    private readonly string _key;
    private readonly string _queueName;
    private readonly string _queueConnectionString;
    private readonly BlobContainerClient _containerClient;
    private readonly StorageSharedKeyCredential _credential;

    public FileService(IConfiguration configuration)
    {
        _storageAccount = configuration.GetValue<string>("StorageAccount");
        _key = configuration.GetValue<string>("AccessKey");
        _queueName = configuration.GetValue<string>("QueueName");
        _queueConnectionString = configuration.GetValue<string>("QueueConnectionString");

        _credential = new StorageSharedKeyCredential(_storageAccount, _key);
        var blobUri = $"https://{_storageAccount}.blob.core.windows.net";
        var blobServiceClient = new BlobServiceClient(new Uri(blobUri), _credential);
        _containerClient = blobServiceClient.GetBlobContainerClient("democontainer2004");
    }

    public async Task<BlobResponseDto> UploadAsync(string email, IFormFile blob)
    {
        BlobResponseDto response = new();
        BlobClient client = _containerClient.GetBlobClient(blob.FileName);

        await using (Stream? data = blob.OpenReadStream())
        {
            await client.UploadAsync(data);
        }

        response.Status = $"File {blob.FileName} Upload Successfully";
        response.Error = false;
        response.Blob.Uri = client.Uri.AbsoluteUri;
        response.Blob.Name = client.Name;

        string sasToken = GenerateSasToken(client.Uri);

        await SendMessageToQueue(email, blob.FileName, sasToken);

        return response;
    }

    public string GenerateSasToken(Uri blobUri)
    {
        BlobSasBuilder sasBuilder = new BlobSasBuilder()
        {
            BlobContainerName = _containerClient.Name,
            BlobName = blobUri.Segments.Last(),
            Resource = "b",
            StartsOn = DateTimeOffset.UtcNow,
            ExpiresOn = DateTimeOffset.UtcNow.AddHours(1),
            Protocol = SasProtocol.Https
        };

        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        string sadToken = sasBuilder.ToSasQueryParameters(_credential).ToString(); 

        return sadToken;
    }

    public async Task SendMessageToQueue(string email, string fileName, string sasToken)
    {
        var queueClient = new QueueClient(_queueConnectionString, _queueName);

        var messageBody = JsonSerializer.Serialize(new {Email=email, FileName=fileName, SasToken= sasToken});
        var res = await queueClient.SendMessageAsync(messageBody);
    }
}
