namespace WebmentionService.Services

open System
open System.Net
open System.Linq
open System.Xml.Linq
open Azure.Data.Tables
open WebmentionService

type RssService () = 
    
    let getMentionType (m:WebmentionEntity) = 
        match m.IsBookmark, m.IsLike, m.IsReply, m.IsRepost with 
        | true,false,false,false -> "bookmarked"
        | false,true,false,false -> "liked"
        | false,false,true,false -> "replied"
        | false,false,false,true -> "reposted"
        | true,true,true,true | false,false,false,false -> "mentioned"
        | _ -> "mentioned"

    let webmentionChannelXml (title:string) (link:string) (description:string) (language:string) = 
        
        let lastPubDate = DateTimeOffset(DateTime.UtcNow).ToString()

        XElement(XName.Get "rss",
            XAttribute(XName.Get "version","2.0"),
                XElement(XName.Get "channel",
                    XElement(XName.Get "title", title),
                    XElement(XName.Get "link", link),
                    XElement(XName.Get "description", description),
                    XElement(XName.Get "lastPubDate", lastPubDate),
                    XElement(XName.Get "language", language)))

    let webmentionEntryXml (m: WebmentionEntity) = 
        let decodedSourceUrl = m.RowKey |> WebUtility.UrlDecode |> Uri
        let decodedTargetUrl = m.PartitionKey |> WebUtility.UrlDecode |> Uri

        let mentionType = getMentionType m

        let description = 
            $"<p><a href=\"{decodedSourceUrl.OriginalString}\">{decodedSourceUrl.Host}</a> {mentionType} <a href=\"{decodedTargetUrl.OriginalString}\">{decodedTargetUrl.AbsolutePath}</a></p>"

        let timestamp = 
            let ts = (m :> ITableEntity).Timestamp
            match ts.HasValue with
            | true -> ts.Value.ToUniversalTime().DateTime.ToString()
            | false -> DateTimeOffset(DateTime.UtcNow).ToString()

        XElement(XName.Get "item", 
            XElement(XName.Get "description", description),
            XElement(XName.Get "link", decodedSourceUrl.OriginalString),
            XElement(XName.Get "guid", decodedSourceUrl.OriginalString),
            XElement(XName.Get "pubDate", timestamp))


    member _.BuildRssFeed (webmentions:WebmentionEntity seq) (title:string) (link:string) (description:string) (language:string) = 
        
        let entries = webmentions |> Seq.map(webmentionEntryXml)
        let channel = webmentionChannelXml title link description language

        channel.Descendants(XName.Get "channel").First().Add(entries)

        channel       