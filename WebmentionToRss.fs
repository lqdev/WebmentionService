namespace WebmentionService

open System
open System.IO
open System.Xml
open Microsoft.Azure.Functions.Worker
open Microsoft.Extensions.Logging
open Azure.Data.Tables
open Azure.Storage.Blobs
open WebmentionService.Services

type WebmentionToRss (rssService:RssService, tableServiceClient: TableServiceClient, blobServiceClient: BlobServiceClient) = 

    let getMentions (t:TableClient) = 
        let timespan = 
            DateTimeOffset(DateTime.UtcNow)
                .Subtract(TimeSpan.FromDays(31))
                .ToString()

        // let query = $"Timestamp ge datetime'{timespan}'"
        
        let webmentions = t.Query<WebmentionEntity>()

        webmentions


    member x.RssService = rssService

    [<Function("WebmentionToRss")>]
    member x.Run
        ([<TimerTrigger("0 0 3 * * *")>] info: TimerInfo,
         context: FunctionContext) =

        task {
            let logger = context.GetLogger("WebmentionToRss")
            
            // Get table client from injected service
            let t = tableServiceClient.GetTableClient("webmentions")
            let mentions = getMentions t

            let rss = x.RssService.BuildRssFeed mentions "lqdev's Webmentions" "http://lqdev.me" "lqdev's Webmentions" "en"

            logger.LogInformation("Generated RSS feed with webmentions")

            // Get blob client from injected service
            let containerClient = blobServiceClient.GetBlobContainerClient("feeds")
            do! containerClient.CreateIfNotExistsAsync() |> Async.AwaitTask |> Async.Ignore
            
            let blobClient = containerClient.GetBlobClient("webmentions/index.xml")
            
            // Write RSS to blob storage
            use memoryStream = new MemoryStream()
            use xmlWriter = XmlWriter.Create(memoryStream)
            rss.WriteTo(xmlWriter)
            xmlWriter.Flush()
            memoryStream.Position <- 0L
            
            do! blobClient.UploadAsync(memoryStream, overwrite = true) |> Async.AwaitTask |> Async.Ignore
            
            logger.LogInformation("RSS feed uploaded to blob storage successfully")
        }
