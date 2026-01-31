import * as constants from "../Constants/constants";
import {IQueueAdd} from "../Interfaces/IQueueAdd";
import {QueueOrder} from "../Models/QueueOrder";


export async function GetRoom(roomId : string) {
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

export async function ChangeQueueOrder(roomId: string, request : QueueOrder[]) {
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

export async function UploadVideo(roomId : string, data : FormData) {
    return await fetch(`${constants.API_URL}/queue/${roomId}/upload`, {
        method: "POST",
        body: data
    })
}

export async function GetLoginInfo() {
    return await fetch(`${constants.API_URL}/manage/info`, {
        method: "GET",
        credentials: "include"
    });
}

export async function Logout() {
    return await fetch(`${constants.API_URL}/logout`, {
        method: "POST",
        credentials: "include"
    });
}

export async function GetHomeInfo(includeRecentRooms : boolean) {
    return await fetch(`${constants.API_URL}/home${includeRecentRooms ? '?includeRecentRooms=true' : ''}`, {
        method: "GET"
    });
}