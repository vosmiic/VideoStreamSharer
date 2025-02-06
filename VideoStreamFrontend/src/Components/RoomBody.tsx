import {useContext, useEffect, useState} from "react";
import * as apiCalls from "../Helpers/ApiCalls.tsx";
import NotFound from "./NotFound.tsx";
import Loading from "./Loading.tsx";
import {IRoom} from "../Interfaces/IRoom.tsx";
import YouTubePlayer from "./Players/YouTubePlayer.tsx";
import Queue from "./Queue/Queue.tsx";
import {RoomContext} from "../Contexts/RoomContext.tsx";
import {HubContext} from "../Contexts/HubContext.tsx";

export default function RoomBody(params: {roomId: string}) {
    const hub = useContext(HubContext);
    const [room, setRoom] = useState<IRoom>();
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
                                setRoom(json as IRoom);
                                setLoadState(2);
                            })
                        } else {
                            setLoadState(1);
                        }
                    }
                });
        }

        getRoom();

        return () => {
            ignore = true;
        }
    }, [params.roomId]);

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
    }, [loadState, room]);

    function onLoaded() {
        setRender(LoadedState());

        hub.send("JoinedRoom", params.roomId);
    }

    function LoadedState() {
        return <RoomContext.Provider value={params.roomId}>
            <p>Loaded {room.Name}</p>
            <div className={"flex w-full"}>
                <div className={"flex-auto w-20 bg-red-500"}>
                    <Queue queueItems={room.Queue} />
                </div>
                <div className={"flex-none w-60 bg-yellow-500"}>
                    <YouTubePlayer />
                </div>
                <div className={"flex-auto w-20 bg-blue-500"}>

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