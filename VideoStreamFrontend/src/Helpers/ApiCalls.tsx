import * as constants from "../Constants/constants.tsx";
import QueueAddBody from "../Models/QueueAdd.tsx";
import QueueOrder from "../Models/QueueOrder.tsx";
import {IQueueAdd} from "../Interfaces/IQueueAdd.tsx";


export async function GetRoom(roomId) {
    return await fetch(`${constants.API_URL}/room/${roomId}`, {
        method: "GET",
        headers: {
            "accept": "application/json"
        }
    })
}

export async function AddToQueue(roomId : string, input : IQueueAdd) {
    return await fetch(`${constants.API_URL}/queue/${roomId}/add`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify(input)
    });
}

export async function ChangeQueueOrder(roomId : string, request : [QueueOrder]) {
    return await fetch(`${constants.API_URL}/queue/${roomId}/order`, {
        method: "PUT",
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify(request)
    });
}

export async function GetStream(userId: string) {
    return await fetch(`${constants.API_URL}/stream/${userId}`, {
        method: "GET",
        headers: {
            "Content-Type": "application/json"
        },
        credentials: "include"
    });
}

export async function Lookup(url : string) {
    return await fetch(`${constants.API_URL}/queue/lookup?url=${url}`, {
        method: "GET",
        headers: {
            "Content-Type": "application/json"
        }
    });
}