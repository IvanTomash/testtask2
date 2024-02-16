using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Microsoft.Azure.Documents;
using Microsoft.Azure.KeyVault.Core;
using Microsoft.Azure.Storage.Queue;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System.Net.Mail;

namespace SendEmailBlobTrigger;

public class Function1
{
    public class ReceivedMessage
    {
        public string Email { get; set; }
        public string FileName { get; set; }
        public string SasToken { get; set; }
    }

    [FunctionName("Function1")]
    public async Task Run([BlobTrigger("democontainer2004/{name}", Connection = "")]Stream myBlob, string name, ILogger log)
    {
        string storageAccount = "blobstorage2004";
        string _key = "C7U37MvP2J6G6qvtcUW9Vec9lOWF5aY71Ajv01+B+gB1XnMExLVyu8CmVHDE8x2SmHKXvI4pc9uQ+AStKL8awA==";
        var blobUri = $"https://{storageAccount}.blob.core.windows.net";
        var credential = new StorageSharedKeyCredential(storageAccount, _key);
        BlobContainerClient containerClient = new BlobServiceClient(new Uri(blobUri), credential).GetBlobContainerClient("democontainer1");

        var message = await GetMessageFromQueue();
        var messageObj = JsonSerializer.Deserialize<ReceivedMessage>(message);
        var email = messageObj.Email;
        var sasToken = messageObj.SasToken;

        string blobUriWithSas = GenerateBlobUriWithSas(name, messageObj.SasToken, containerClient.GetBlobClient(name));

        await SendEmailNotification(messageObj.Email, name, blobUriWithSas);
    }

    private async Task<string> GetMessageFromQueue()
    {
        string connectionString = "DefaultEndpointsProtocol=https;AccountName=blobstorage2004;AccountKey=C7U37MvP2J6G6qvtcUW9Vec9lOWF5aY71Ajv01+B+gB1XnMExLVyu8CmVHDE8x2SmHKXvI4pc9uQ+AStKL8awA==;EndpointSuffix=core.windows.net";
        string queueName = "notificationqueue";
        var queueClient = new QueueClient(connectionString, queueName);

        // Receive message from queue
        var messages = await queueClient.ReceiveMessagesAsync();
        if (messages.Value.Length > 0)
        {
            var message = messages.Value[0];

            // Delete the message from the queue
            await queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt);

            return message.MessageText;
        }
        return null;
    }

    private string GenerateBlobUriWithSas(string blobName, string sasToken, BlobClient client)
    {
        var blobUriWithSas = $"{client.Uri}?{sasToken}";
        return blobUriWithSas;
    }

    private async Task SendEmailNotification(string email, string fileName, string blobUriWithSas)
    {
        var smtpClient = new SmtpClient("smtp.gmail.com");
        var mailMessage = new MailMessage();

        mailMessage.From = new MailAddress("ivantomashchuk31@gmail.com");
        mailMessage.To.Add(email);
        mailMessage.Subject = "New File Uploaded";
        mailMessage.IsBodyHtml = true;
        mailMessage.Body = $"<strong>The file {fileName} has been uploaded successfully. You can access it using the following link: <a href=\"{blobUriWithSas}\">{blobUriWithSas}</a></strong>";

        smtpClient.EnableSsl = true;
        smtpClient.Port = 587;
        smtpClient.Credentials = new NetworkCredential("ivantomashchuk31@gmail.com", "");

        smtpClient.Send(mailMessage);
    }
}
