namespace WebmentionService

open System
open System.IO
open System.Xml
open Microsoft.Azure.Functions.Worker
open Microsoft.Extensions.Logging
open Azure.Data.Tables
open WebmentionService.Services

type WebmentionToRss (rssService:RssService) = 

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
        ([<TimerTrigger("0 0 3 * * *")>] info: TimerInfo)
        ([<TableInput("webmentions",Connection="AzureWebJobsStorage")>] t: TableClient)
        ([<BlobOutput("feeds/webmentions/index.xml", Connection="AzureWebJobsStorage")>] rssBlob: Stream)
        (context: FunctionContext) =

        task {
            let logger = context.GetLogger("WebmentionToRss")
            let mentions = getMentions t

            let rss = x.RssService.BuildRssFeed mentions "lqdev's Webmentions" "http://lqdev.me" "lqdev's Webmentions" "en"

            logger.LogInformation("Generated RSS feed with webmentions")                

            use xmlWriter = XmlWriter.Create(rssBlob)

            rss.WriteTo(xmlWriter) |> ignore
        }
