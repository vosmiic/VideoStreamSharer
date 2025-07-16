import {useContext, useEffect, useState} from "react";
import * as apiCalls from "../Helpers/ApiCalls.tsx";
import NotFound from "./NotFound.tsx";
import Loading from "./Loading.tsx";
import {GetRoomResponse} from "../Interfaces/IRoom.tsx";
import Queue from "./Queue/Queue.tsx";
import {RoomContext} from "../Contexts/RoomContext.tsx";
import {HubContext} from "../Contexts/HubContext.tsx";
import {HubConnectionState} from "@microsoft/signalr";
import Users from "./Users.tsx";
import FilePlayer from "./Players/FilePlayer.tsx";

export default function RoomBody(params: {roomId: string}) {
    const hub = useContext(HubContext);
    const [getRoom, setGetRoom] = useState<GetRoomResponse>();
    const [render, setRender] = useState(Loading());
    const [loadState, setLoadState] = useState(0);
    /*
    State:
    0 = initial load/loading
    1 = not found
    2 = loaded
     */

    useEffect(() => {
        let ignore = false;

        async function getRoom() {
            apiCalls.GetRoom(params.roomId)
                .then(response => {
                    if (!ignore) {
                        if (response.status == 200) {
                            response.json().then(json => {
                                setGetRoom(json as GetRoomResponse);
                                setLoadState(2);
                            })
                        } else {
                            setLoadState(1);
                        }
                    }
                });
        }

        async function connectHub() {
            if (hub.state == HubConnectionState.Disconnected) {
                await hub.start();
            }
        }

        getRoom();
        connectHub();

        return () => {
            ignore = true;
        }
    }, [params.roomId, hub]);

    useEffect(() => {
        switch (loadState) {
            case 0:
                setRender(Loading());
                break;
            case 1:
                setRender(NotFound());
                break;
            case 2:
                onLoaded();
                break;
        }
    }, [loadState, getRoom]);

    async function onLoaded() {
        setRender(LoadedState());
    }

    function LoadedState() {
        return <RoomContext.Provider value={params.roomId}>
            <p>Loaded {getRoom.Room.Name}</p>
            <div className={"flex w-full"}>
                <div className={"flex-auto w-20 bg-red-500"}>
                    <Queue queueItems={getRoom.Room.Queue} />
                </div>
                <div className={"flex-auto w-60 bg-yellow-500"}>
                    <FilePlayer streamUrls={getRoom?.Room.StreamUrls} />
                </div>
                <div className={"flex-auto w-20 bg-blue-500"}>
                    <Users users={getRoom.Users}/>
                </div>
            </div>
        </RoomContext.Provider>
    }

    return (
        <>
            <h1>Room</h1>
            {render}
        </>
    )
}