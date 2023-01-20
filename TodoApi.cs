using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TodoApp.Core.Extensions;
using TodoApp.Core.Mappings;
using TodoApp.Core.Models;

namespace TodoApp
{
    public static class TodoApi
    {

        [FunctionName("CreateTodo")]
        public static async Task<IActionResult> CreateTodo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "todo")] HttpRequest req,
            [Table("todos", Connection = "AzureWebJobsStorage")] IAsyncCollector<TodoTableEntity> todoTable,
            [Queue("todoMessages", Connection = "AzureWebJobsStorage")] IAsyncCollector<Todo> todoQueue,
            ILogger log)
        {
            log.LogInformation("Creating new todo list item.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var input = JsonConvert.DeserializeObject<TodoCreateModel>(requestBody);
 
            var todo = new Todo() { TaskDescription = input.TaskDescription };

            await todoTable.AddAsync(todo.ToTableEntity());
            await todoQueue.AddAsync(todo);

            return new OkObjectResult(todo);
        }

        [FunctionName("GetTodos")]
        public static async Task<IActionResult> GetTodos(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "todo")] HttpRequest req,
            [Table("todos", Connection = "AzureWebJobsStorage")] TableClient todoTable,
            ILogger log)
        {
            log.LogInformation("Get list of todos.");

            var page1 = await todoTable.QueryAsync<TodoTableEntity>().AsPages().FirstOrDefault();


            return new OkObjectResult(page1.Values.Select(TodoMapping.ToTodo));
        }

        [FunctionName("GetTodoById")]
        public static async Task<IActionResult> GetTodoById(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "todo/{id}")] HttpRequest req,
            [Table("todos", "TODO", "{id}", Connection = "AzureWebJobsStorage")] TodoTableEntity todo, 
            ILogger log, string id)
        {
            log.LogInformation($"Get todo with an id: {id} .");

            if(todo == null)
            {
                log.LogInformation($"Item {id} not found");
                return new NotFoundResult();
            }

            return new OkObjectResult(todo.ToTodo());
        }

        [FunctionName("UpdateTodo")]
        public static async Task<IActionResult> UpdateTodo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "todo/{id}")] HttpRequest req,
            [Table("todos", Connection = "AzureWebJobsStorage" )] TableClient todoTable,
            ILogger log, string id)
        {
            log.LogInformation($"Update todo with an id: {id} .");

            // chekc if todo exists
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var updated = JsonConvert.DeserializeObject<TodoUpdateModel>(requestBody);
            TodoTableEntity existingRow;

            try
            {
                var findResult = await todoTable.GetEntityAsync<TodoTableEntity>("TODO", id);
                existingRow = findResult.Value;
            }
            catch(RequestFailedException ex) when (ex.Status == 404)
            {
                return new NotFoundResult();
            }

            existingRow.IsCompleted = updated.IsCompleted;
            if(!string.IsNullOrEmpty(updated.TaskDescription))
                existingRow.TaskDescription = updated.TaskDescription;

            await todoTable.UpdateEntityAsync<TodoTableEntity>(existingRow, existingRow.ETag, TableUpdateMode.Replace);

            return new OkObjectResult(existingRow.ToTodo());
        }

        [FunctionName("DeleteTodo")]
        public static async Task<IActionResult> DeleteTodo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "todo/{id}")] HttpRequest req,
            [Table("todos", Connection = "AzureWebJobsStorage")] TableClient todoTable,
            ILogger log, string id)
        {
            log.LogInformation($"Delete todo with an id: {id} .");

            try
            {
                await todoTable.DeleteEntityAsync("TODO", id, ETag.All).ConfigureAwait(false);
            }
            catch(RequestFailedException ex) when (ex.Status == 404)
            {
                return new NotFoundResult();
            }

            return new OkResult();
        }
    }
}
