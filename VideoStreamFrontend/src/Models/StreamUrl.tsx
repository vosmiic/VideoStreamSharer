import {StreamType} from "./Enums/StreamType.tsx";

export default class StreamUrl {
    Url: string;
    StreamType: StreamType;

    constructor(url: string, type : StreamType) {
        this.Url = url;
        this.StreamType = type;
    }
}