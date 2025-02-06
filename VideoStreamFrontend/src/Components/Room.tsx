import {HubContext} from "../Contexts/HubContext.tsx";
import {HubConnectionBuilder, HubConnectionState} from "@microsoft/signalr";
import {API_URL} from "../constants.tsx";
import {useParams} from "react-router-dom";
import RoomBody from "./RoomBody.tsx";


export default function Room() {
    const params = useParams();

    const hubConnection = new HubConnectionBuilder()
        .withUrl(`${API_URL}/hub?roomId=${params.roomId}`)
        .build();

    if (hubConnection.state == HubConnectionState.Disconnected) {
        hubConnection.start();
    }

    return <HubContext.Provider value={hubConnection}>
        <RoomBody roomId={params.roomId} />
    </HubContext.Provider>
}