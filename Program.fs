namespace WebmentionService

module Program =
    open System
    open Microsoft.Extensions.Hosting
    open Microsoft.Extensions.DependencyInjection
    open Microsoft.Azure.Functions.Worker
    open Azure.Data.Tables
    open WebmentionFs.Services
    open WebmentionService.Services

    [<EntryPoint>]
    let main args =
        let host =
            HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureServices(fun services ->
                    
                    // Add request validation service
                    services.AddScoped<RequestValidationService>(fun _ -> 
                        let hostNames = Environment.GetEnvironmentVariable("PERSONAL_WEBSITE_HOSTNAMES")
                        let hostNameList = 
                            if isNull hostNames then [||]
                            else hostNames.Split(',')
                        new RequestValidationService(hostNameList)) |> ignore

                    // Add webmention validation service
                    services.AddScoped<WebmentionValidationService>() |> ignore

                    // Add RSS service
                    services.AddScoped<RssService>() |> ignore

                    // Add Table Storage client
                    services.AddSingleton<TableServiceClient>(fun _ ->
                        let connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage")
                        new TableServiceClient(connectionString)) |> ignore
                )
                .Build()

        host.Run()
        0
