import {IQueue} from "./IQueue.tsx";

export interface IRoom {
    Id: string;
    OwnerId: string;
    Name: string;
    Queue: Array<IQueue>;
}