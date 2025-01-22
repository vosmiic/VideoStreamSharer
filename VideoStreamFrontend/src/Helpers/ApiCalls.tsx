import * as constants from "../constants.tsx";


export async function GetRoom(roomId) {
    return await fetch(`${constants.API_URL}/room/${roomId}`, {
        method: "GET",
        headers: {
            "accept": "application/json"
        }
    })
}