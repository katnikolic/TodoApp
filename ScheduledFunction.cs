using System;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using TodoApp.Core.Extensions;
using TodoApp.Core.Models;

namespace TodoApp
{
    public class ScheduledFunction
    {
        [FunctionName("ScheduledFunction")]
        public async Task Run([TimerTrigger("0 */1 * * * *")]TimerInfo myTimer,
            [Table("todos", Connection = "AzureWebJobsStorage")] TableClient todoTable,
            ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            var page1 = await todoTable.QueryAsync<TodoTableEntity>().AsPages().FirstOrDefault();
            int deleted = 0;
            foreach(var todo in page1.Values)
            {
                if (todo.IsCompleted)
                {
                    await todoTable.DeleteEntityAsync("TODO", todo.RowKey, ETag.All).ConfigureAwait(false);
                    deleted++;
                }
            }

            log.LogInformation($"Deleted {deleted} items at: {DateTime.Now}");
        }
    }
}
