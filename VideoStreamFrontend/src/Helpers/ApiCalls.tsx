import * as constants from "../Constants/constants.tsx";
import QueueAddBody from "../Models/QueueAdd.tsx";
import QueueOrder from "../Models/QueueOrder.tsx";


export async function GetRoom(roomId) {
    return await fetch(`${constants.API_URL}/room/${roomId}`, {
        method: "GET",
        headers: {
            "accept": "application/json"
        }
    })
}

export async function AddToQueue(request : QueueAddBody) {
    return await fetch(`${constants.API_URL}/queue/${request.RoomId}/add`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json"
        },
        body: '"' + request.Url + '"'
    });
}

export async function ChangeQueueOrder(roomId : string, request : [QueueOrder]) {
    return await fetch(`${constants.API_URL}/queue/${roomId}/order`, {
        method: "PUT",
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify(request)
    })
}