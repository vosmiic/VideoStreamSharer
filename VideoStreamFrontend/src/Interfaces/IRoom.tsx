import {IQueue} from "./IQueue.tsx";

export interface GetRoomResponse {
    Room: IRoom;
    Users: string[]
}

export interface IRoom {
    Id: string;
    Name: string;
    Queue: Array<IQueue>;
}