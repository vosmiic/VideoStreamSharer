import {StreamType} from "./Enums/StreamType.tsx";
import {Protocol} from "./Enums/Protocol.tsx";

export default class StreamUrl {
    Url: string;
    StreamType: StreamType;
    Protocol: Protocol;

    constructor(url: string, streamType : StreamType, fileType : Protocol) {
        this.Url = url;
        this.StreamType = streamType;
        this.Protocol = fileType;
    }
}