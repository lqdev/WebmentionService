namespace WebmentionService

open System
open System.Net
open Microsoft.Azure.WebJobs
open Microsoft.AspNetCore.Mvc
open Microsoft.Azure.WebJobs.Extensions.Http
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open Azure.Data.Tables
open WebmentionFs
open WebmentionFs.Services


type ReceiveWebmention (webmentionReceiver: IWebmentionReceiver<Webmention>) = 

    let urlEncode (uri:Uri) = 
        uri.OriginalString |> WebUtility.UrlEncode

    let mapMentionToTableEntity (m:Webmention) = 
        
        let encodedSourceUrl  = urlEncode m.RequestBody.Source
        let encodedTargetUrl = urlEncode m.RequestBody.Target
        
        let entity = 
            new WebmentionEntity(
                encodedSourceUrl,
                encodedTargetUrl,
                m.Mentions.IsBookmark,
                m.Mentions.IsLike,
                m.Mentions.IsReply,
                m.Mentions.IsRepost)
        
        entity


    member x.WebmentionReceiver = webmentionReceiver

    [<FunctionName("ReceiveWebmention")>]
    member x.Run 
        ([<HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "inbox")>] req: HttpRequest) 
        ([<Table("webmentions", Connection="AzureWebJobsStorage")>] t: TableClient)
        (log: ILogger) =
        task {
            
            log.LogInformation("Processing webmention request")

            let! validationResult = x.WebmentionReceiver.ReceiveAsync(req)

            let response = 
                match validationResult with
                | ValidationSuccess m -> 
                    let entity = mapMentionToTableEntity m
                    try
                        t.AddEntity(entity) |> ignore
                        OkObjectResult("Webmention processed successfully") :> IActionResult
                    with
                        | ex -> 
                            log.LogError($"{ex}")
                            BadRequestObjectResult($"Error processing webmention. Webmention already exists") :> IActionResult
                | ValidationError e -> 
                    log.LogError(e)
                    BadRequestObjectResult(e) :> IActionResult

            return response

        }