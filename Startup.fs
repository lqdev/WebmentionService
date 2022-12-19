namespace WebmentionService

open System
open Microsoft.Extensions.DependencyInjection
open Microsoft.Azure.Functions.Extensions.DependencyInjection
open WebmentionFs
open WebmentionFs.Services

type Startup () = 
    inherit FunctionsStartup () 
        override x.Configure(builder:IFunctionsHostBuilder) = 
            
            // Add request validation service
            builder.Services.AddScoped<RequestValidationService>(fun _ -> 

                let hostNames = Environment.GetEnvironmentVariable("PERSONAL_WEBSITE_HOSTNAMES")

                let hostNameList = hostNames.Split(',')

                new RequestValidationService(hostNameList)) |> ignore
            

            // Add webmention validation service
            builder.Services.AddScoped<WebmentionValidationService>() |> ignore

            builder.Services.AddScoped<IWebmentionReceiver<Webmention>,WebmentionReceiverService>(fun (s:IServiceProvider) ->
                let requestValidationService = s.GetRequiredService<RequestValidationService>()
                let webmentionValidationService = s.GetRequiredService<WebmentionValidationService>()
                new WebmentionReceiverService(requestValidationService,webmentionValidationService)) |> ignore

[<assembly: FunctionsStartup(typeof<Startup>)>]

do ()