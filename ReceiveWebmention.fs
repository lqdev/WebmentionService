namespace WebmentionService

open System
open System.IO
open System.Net
open System.Web
open System.Threading.Tasks
open Microsoft.Azure.Functions.Worker
open Microsoft.Azure.Functions.Worker.Http
open Microsoft.Extensions.Logging
open Azure.Data.Tables
open WebmentionFs
open WebmentionFs.Services


type ReceiveWebmention (requestValidationService: RequestValidationService, webmentionValidationService: WebmentionValidationService) = 

    member x.RequestValidationService = requestValidationService
    member x.WebmentionValidationService = webmentionValidationService

    [<Function("ReceiveWebmention")>]
    member x.Run 
        ([<HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "inbox")>] req: HttpRequestData) 
        ([<TableInput("webmentions", Connection="AzureWebJobsStorage")>] tableClient: TableClient)
        (context: FunctionContext) : Task<HttpResponseData> =
        task {
            let logger = context.GetLogger("ReceiveWebmention")
            logger.LogInformation("Processing webmention request")

            // Read form body
            use reader = new StreamReader(req.Body)
            let! body = reader.ReadToEndAsync()
            let formData = HttpUtility.ParseQueryString(body)
            
            let source = formData.["source"]
            let target = formData.["target"]
            
            // Validate URLs exist
            if String.IsNullOrEmpty(source) || String.IsNullOrEmpty(target) then
                let response = req.CreateResponse(HttpStatusCode.BadRequest)
                do! response.WriteStringAsync("Missing source or target parameter")
                return response
            else
                // Create UrlData for validation (Source and Target, not SourceUrl/TargetUrl)
                let urlData: UrlData = { Source = Uri(source); Target = Uri(target) }
                
                // Validate request
                let! requestValidationResult = x.RequestValidationService.ValidateAsync(urlData)
                
                match requestValidationResult with
                | RequestError errorMsg ->
                    logger.LogError($"Invalid webmention request: {errorMsg}")
                    let response = req.CreateResponse(HttpStatusCode.BadRequest)
                    do! response.WriteStringAsync("Invalid webmention request")
                    return response
                | RequestSuccess validData ->
                    // Validate webmention (returns WebmentionValidationResult)
                    let! webmentionValidationResult = x.WebmentionValidationService.ValidateAsync validData.Source validData.Target
                    
                    match webmentionValidationResult with
                    | MentionError errorMsg ->
                        logger.LogError($"Could not verify webmention from {source} to {target}: {errorMsg}")
                        let response = req.CreateResponse(HttpStatusCode.BadRequest)
                        do! response.WriteStringAsync("Could not verify webmention")
                        return response
                    | AnnotatedMention mentionTypes ->
                        // URL encode for storage keys
                        let encodedSource = Uri.EscapeDataString(source)
                        let encodedTarget = Uri.EscapeDataString(target)
                        
                        // Store in table with mention type info
                        let entity = WebmentionEntity(encodedSource, encodedTarget, mentionTypes.IsBookmark, mentionTypes.IsLike, mentionTypes.IsReply, mentionTypes.IsRepost)
                        
                        try
                            let! _ = tableClient.UpsertEntityAsync(entity) |> Async.AwaitTask
                            logger.LogInformation($"Stored webmention from {source} to {target}")
                            let response = req.CreateResponse(HttpStatusCode.OK)
                            do! response.WriteStringAsync("Webmention received")
                            return response
                        with
                        | ex -> 
                            logger.LogError($"Error storing webmention: {ex.Message}")
                            let response = req.CreateResponse(HttpStatusCode.InternalServerError)
                            do! response.WriteStringAsync("Error processing webmention")
                            return response
                    | UnannotatedMention ->
                        // URL encode for storage keys
                        let encodedSource = Uri.EscapeDataString(source)
                        let encodedTarget = Uri.EscapeDataString(target)
                        
                        // Store in table with default mention types
                        let entity = WebmentionEntity(encodedSource, encodedTarget, false, false, false, false)
                        
                        try
                            let! _ = tableClient.UpsertEntityAsync(entity) |> Async.AwaitTask
                            logger.LogInformation($"Stored webmention from {source} to {target}")
                            let response = req.CreateResponse(HttpStatusCode.OK)
                            do! response.WriteStringAsync("Webmention received")
                            return response
                        with
                        | ex -> 
                            logger.LogError($"Error storing webmention: {ex.Message}")
                            let response = req.CreateResponse(HttpStatusCode.InternalServerError)
                            do! response.WriteStringAsync("Error processing webmention")
                            return response
        }