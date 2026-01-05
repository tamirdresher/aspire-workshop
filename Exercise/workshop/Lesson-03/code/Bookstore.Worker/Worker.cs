using Bookstore.Shared;
using System.Net.Http.Json;
using System.Text.Json;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;

namespace Bookstore.Worker;

public class Worker(ILogger<Worker> logger, HttpClient httpClient, QueueServiceClient queueServiceClient) : BackgroundService
{
    private static readonly string[] DescriptionTemplates = new[]
    {
        "A captivating tale that explores the depths of human nature and the complexities of life.",
        "An unforgettable journey through time and space, filled with adventure and discovery.",
        "A masterpiece of storytelling that will keep you on the edge of your seat from start to finish.",
        "A profound exploration of love, loss, and the human condition that resonates with readers of all ages.",
        "A thrilling narrative that weaves together suspense, drama, and unexpected twists.",
        "An inspiring story of courage, resilience, and the triumph of the human spirit.",
        "A thought-provoking examination of society and the challenges we face in the modern world.",
        "A beautifully written work that captures the essence of the human experience with grace and eloquence."
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Bookstore Worker starting...");
        
        // Wait a bit for the API to start
        await Task.Delay(5000, stoppingToken);

        // Get the queue client
        var queueClient = queueServiceClient.GetQueueClient("book-created");
        await queueClient.CreateIfNotExistsAsync(cancellationToken: stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                logger.LogInformation("Checking for new books to process...");
                
                // Receive messages from Azure Storage Queue
                QueueMessage[] messages = await queueClient.ReceiveMessagesAsync(
                    maxMessages: 10,
                    visibilityTimeout: TimeSpan.FromMinutes(5),
                    cancellationToken: stoppingToken);
                
                if (messages != null && messages.Length > 0)
                {
                    logger.LogInformation("Found {Count} books to process", messages.Length);
                    
                    foreach (var queueMessage in messages)
                    {
                        try
                        {
                            var message = JsonSerializer.Deserialize<BookCreatedMessage>(queueMessage.MessageText);
                            if (message != null)
                            {
                                await ProcessBookMessage(message, stoppingToken);
                                
                                // Delete the message from the queue after successful processing
                                await queueClient.DeleteMessageAsync(
                                    queueMessage.MessageId,
                                    queueMessage.PopReceipt,
                                    stoppingToken);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Error processing queue message: {MessageId}", queueMessage.MessageId);
                        }
                    }
                }
                else
                {
                    logger.LogDebug("No new books to process");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing book messages");
            }
            
            // Wait before checking again
            await Task.Delay(10000, stoppingToken);
        }
    }

    private async Task ProcessBookMessage(BookCreatedMessage message, CancellationToken stoppingToken)
    {
        try
        {
            logger.LogInformation("Processing book: {Title} by {Author}", message.Title, message.Author);
            
            // Generate a random description (simulating AI)
            var random = new Random();
            var description = $"{DescriptionTemplates[random.Next(DescriptionTemplates.Length)]} " +
                             $"'{message.Title}' by {message.Author} is a must-read for any book lover.";
            
            // Simulate some processing time
            await Task.Delay(2000, stoppingToken);
            
            // Update the book description via API
            var updateResponse = await httpClient.PutAsJsonAsync(
                $"/books/{message.BookId}/description",
                description,
                stoppingToken);
            
            if (updateResponse.IsSuccessStatusCode)
            {
                logger.LogInformation("Successfully updated description for book: {Title}", message.Title);
            }
            else
            {
                logger.LogWarning("Failed to update description for book: {Title}", message.Title);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing book: {BookId}", message.BookId);
        }
    }
}
