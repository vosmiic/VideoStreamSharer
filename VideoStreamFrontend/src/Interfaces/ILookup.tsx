export interface ILookup {
    VideoFormats : IFormat[]
    AudioFormats : IFormat[]
    ThumbnailUrl : string
    Title : string
    ChannelTitle : string
    Viewcount : string
    Duration : string
}

export interface IFormat {
    Id : string,
    Value : string
}