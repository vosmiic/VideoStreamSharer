import {IQueue} from "./IQueue.tsx";
import StreamUrl from "../Models/StreamUrl.tsx";

export interface GetRoomResponse {
    Room: IRoom;
    Users: string[]
}

export interface IRoom {
    Id: string;
    Name: string;
    Queue: Array<IQueue>;
    StreamUrls: Array<StreamUrl>;
}