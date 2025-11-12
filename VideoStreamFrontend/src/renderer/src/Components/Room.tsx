import {HubContext} from "../Contexts/HubContext.tsx";
import {HubConnectionBuilder, HubConnectionState} from "@microsoft/signalr";
import {API_URL} from "../Constants/constants.tsx";
import {useParams} from "react-router-dom";
import RoomBody from "./RoomBody.tsx";
import {useEffect, useMemo} from "react";


export default function Room() {
    const params = useParams();

    const hubConnection = useMemo(() => new HubConnectionBuilder()
        .withUrl(`${API_URL}/hub?roomId=${params.roomId}`)
        .build(),
        [params.roomId]
    );

    useEffect(() => {
        if (hubConnection.state === HubConnectionState.Disconnected) {
            hubConnection.start();
        }

        return () => {
            if (hubConnection.state === HubConnectionState.Connected) {
                hubConnection.stop();
            }
        };
    }, [hubConnection]);

    return <HubContext.Provider value={hubConnection}>
        <RoomBody roomId={params.roomId} />
    </HubContext.Provider>
}