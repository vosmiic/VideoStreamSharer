import {IQueue} from "./IQueue.tsx";
import StreamUrl from "../Models/StreamUrl.tsx";
import {VideoStatus} from "../Constants/constants.tsx";

export interface GetRoomResponse {
    Room: RoomResponse;
    Users: string[]
}

export interface RoomResponse extends IRoom {
    Queue: Array<IQueue>;
    StreamUrls: Array<StreamUrl>;
}

export interface IRoom {
    Id: string;
    Name: string;
    Status: VideoStatus;
    CurrentTime: number;
}