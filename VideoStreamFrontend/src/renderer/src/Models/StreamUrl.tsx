import {StreamType} from "./Enums/StreamType.tsx";
import {Protocol} from "./Enums/Protocol.tsx";

export default class StreamUrl {
    Id: number;
    Url: string;
    StreamType: StreamType;
    Protocol: Protocol;
    Resolution: string;
    ResolutionName : string;

    constructor(id: number, url: string, streamType : StreamType, fileType : Protocol, resolution : string, resolutionName : string) {
        this.Id = id;
        this.Url = url;
        this.StreamType = streamType;
        this.Protocol = fileType;
        this.Resolution = resolution;
        this.ResolutionName = resolutionName;
    }
}