namespace WebmentionService

open System
open System.IO
open System.Xml
open Microsoft.Azure.Functions.Worker
open Microsoft.Extensions.Logging
open Azure.Data.Tables
open WebmentionService.Services

type WebmentionToRss (rssService:RssService, tableServiceClient: TableServiceClient) = 

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
         [<BlobOutput("feeds/webmentions/index.xml", Connection="AzureWebJobsStorage")>] rssBlob: Stream,
         context: FunctionContext) =

        task {
            let logger = context.GetLogger("WebmentionToRss")
            
            // Get table client from injected service
            let t = tableServiceClient.GetTableClient("webmentions")
            let mentions = getMentions t

            let rss = x.RssService.BuildRssFeed mentions "lqdev's Webmentions" "http://lqdev.me" "lqdev's Webmentions" "en"

            logger.LogInformation("Generated RSS feed with webmentions")                

            use xmlWriter = XmlWriter.Create(rssBlob)

            rss.WriteTo(xmlWriter) |> ignore
        }
