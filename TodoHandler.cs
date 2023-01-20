using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TodoApp.Core.Models;

namespace TodoApp
{
    public class TodoHandler
    {
        [FunctionName("TodoHandler")]
        public async Task Run(
            [QueueTrigger("todoMessages", Connection = "AzureWebJobsStorage")]Todo todoMessage,
            [Blob("todos/{queueTrigger}", FileAccess.Read, Connection = "AzureWebJobsStorage")] BlobContainerClient blobContainer,
            ILogger log)
        {
            log.LogInformation($"C# Queue trigger function processed: { JsonConvert.SerializeObject(todoMessage)}");

            await blobContainer.CreateIfNotExistsAsync().ConfigureAwait(false);
            var blob = blobContainer.GetBlobClient($"{todoMessage.Id}.txt");
            await blob.UploadAsync(new BinaryData(todoMessage.TaskDescription)).ConfigureAwait(false);
        }
    }
}
