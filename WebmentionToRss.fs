namespace WebmentionService

open System
open System.IO
open System.Xml
open Microsoft.Azure.WebJobs
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
        
        let webmentions = t.Query<WebmentionEntity>

        webmentions


    member x.RssService = rssService

    [<FunctionName("WebmentionToRss")>]
    member x.Run
        ([<TimerTrigger("0 0 3 * * *")>] info: TimerInfo)
        ([<Table("webmentions",Connection="AzureWebJobsStorage")>] t: TableClient)
        ([<Blob("feeds/webmentions/index.xml", FileAccess.Write, Connection="AzureWebJobsStorage")>] rssBlob: Stream)
        (log: ILogger) =

        task {
            let mentions = getMentions t

            let rss = x.RssService.BuildRssFeed mentions "lqdev's Webmentions" "http://lqdev.me" "lqdev's Webmentions" "en"

            log.LogInformation(rss.ToString())                

            use xmlWriter = XmlWriter.Create(rssBlob)

            rss.WriteTo(xmlWriter) |> ignore
        }
