namespace WebmentionService

open System
open Azure
open Azure.Data.Tables

type WebmentionEntity (source:string, target:string, isBookmark:bool, isLike:bool, isReply:bool, isRepost:bool) = 
    interface ITableEntity with
        member val ETag = ETag "" with get,set
        member val PartitionKey = "" with get,set
        member val RowKey = "" with get,set
        member val Timestamp = Nullable() with get,set

    new () = WebmentionEntity("", "", false, false, false, false)

    member val PartitionKey = target with get,set
    member val RowKey = source with get,set
    member val IsBookmark = isBookmark with get,set
    member val IsLike = isLike with get,set
    member val IsReply = isReply with get,set
    member val IsRepost = isRepost with get,set