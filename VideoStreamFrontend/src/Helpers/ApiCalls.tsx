import * as constants from "../constants.tsx";
import QueueAddBody from "../Models/QueueAdd.tsx";


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