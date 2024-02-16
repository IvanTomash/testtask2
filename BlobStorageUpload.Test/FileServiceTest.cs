using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BlobStorageUpload.Tests;

public class FileServiceTest
{
    [Fact]
    public async Task UploadAsync_Success()
    {
        // Arrange
        var configuration = new Mock<IConfiguration>();
        configuration.Setup(x => x.GetValue<string>("StorageAccount")).Returns("your_storage_account");
        configuration.Setup(x => x.GetValue<string>("AccessKey")).Returns("your_access_key");
        configuration.Setup(x => x.GetValue<string>("QueueName")).Returns("your_queue_name");
        configuration.Setup(x => x.GetValue<string>("QueueConnectionString")).Returns("your_queue_connection_string");

        var fileService = new FileService(configuration.Object);
        var formFile = new Mock<IFormFile>();
        var blobClientMock = new Mock<BlobClient>();

        formFile.Setup(x => x.OpenReadStream()).Returns(new MemoryStream());
        formFile.Setup(x => x.FileName).Returns("test.txt");

        var blobResponseDto = new BlobResponseDto
        {
            Blob = new BlobDto()
        };

        var uri = new Uri("https://your_storage_account.blob.core.windows.net/democontainer2004/test.txt");
        blobClientMock.Setup(x => x.Uri).Returns(uri);
        blobClientMock.Setup(x => x.Name).Returns("test.txt");

        var sasToken = "your_sas_token";

        var expectedMessageBody = JsonSerializer.Serialize(new { Email = "test@example.com", FileName = "test.txt", SasToken = sasToken });

        var queueClientMock = new Mock<QueueClient>();

        queueClientMock.Setup(x => x.SendMessageAsync(expectedMessageBody)).Returns(Task.CompletedTask);

        var queueMessage = new Mock<QueueMessage>();

        queueClientMock.Setup(x => x.SendMessageAsync(expectedMessageBody)).Returns(Task.FromResult(queueMessage.Object));

        var expectedResponse = new BlobResponseDto
        {
            Status = "File test.txt Upload Successfully",
            Error = false,
            Blob = new BlobDto
            {
                Uri = uri.AbsoluteUri,
                Name = "test.txt"
            }
        };

        // Act
        var response = await fileService.UploadAsync("test@example.com", formFile.Object);

        // Assert
        Assert.Equal(expectedResponse.Status, response.Status);
        Assert.Equal(expectedResponse.Error, response.Error);
        Assert.Equal(expectedResponse.Blob.Uri, response.Blob.Uri);
        Assert.Equal(expectedResponse.Blob.Name, response.Blob.Name);
    }

    [Fact]
    public void GenerateSasToken_Success()
    {
        // Arrange
        var configuration = new Mock<IConfiguration>();
        configuration.Setup(x => x.GetValue<string>("StorageAccount")).Returns("your_storage_account");
        configuration.Setup(x => x.GetValue<string>("AccessKey")).Returns("your_access_key");
        configuration.Setup(x => x.GetValue<string>("QueueName")).Returns("your_queue_name");
        configuration.Setup(x => x.GetValue<string>("QueueConnectionString")).Returns("your_queue_connection_string");

        var fileService = new FileService(configuration.Object);
        var uri = new Uri("https://your_storage_account.blob.core.windows.net/democontainer2004/test.txt");

        // Act
        var sasToken = fileService.GenerateSasToken(uri);

        // Assert
        Assert.NotNull(sasToken);
    }

    [Fact]
    public async Task SendMessageToQueue_Success()
    {
        // Arrange
        var configuration = new Mock<IConfiguration>();
        configuration.Setup(x => x.GetValue<string>("StorageAccount")).Returns("your_storage_account");
        configuration.Setup(x => x.GetValue<string>("AccessKey")).Returns("your_access_key");
        configuration.Setup(x => x.GetValue<string>("QueueName")).Returns("your_queue_name");
        configuration.Setup(x => x.GetValue<string>("QueueConnectionString")).Returns("your_queue_connection_string");

        var fileService = new FileService(configuration.Object);
        var expectedMessageBody = JsonSerializer.Serialize(new { Email = "test@example.com", FileName = "test.txt", SasToken = "your_sas_token" });

        var queueClientMock = new Mock<QueueClient>();

        queueClientMock.Setup(x => x.SendMessageAsync(expectedMessageBody)).Returns((Task<Azure.Response<SendReceipt>>)Task.CompletedTask);

        // Act
        await fileService.SendMessageToQueue("test@example.com", "test.txt", "your_sas_token");

        // Assert
        queueClientMock.Verify(x => x.SendMessageAsync(expectedMessageBody), Times.Once);
    }
}
