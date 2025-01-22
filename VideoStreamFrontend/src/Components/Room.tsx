import {useEffect, useState} from "react";
import * as apiCalls from "../Helpers/ApiCalls.tsx";
import NotFound from "./NotFound.tsx";
import Loading from "./Loading.tsx";
import {useParams} from "react-router-dom";

export default function Room() {
    const params = useParams();
    const [room, setRoom] = useState<IRoom>({});
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
                setRender(LoadedState());
                break;
        }
    }, [loadState, room]);

    function LoadedState() {
        return <>
            <p>Loaded {room.Name}</p>
        </>
    }

    return (
        <>
            <h1>Room</h1>
            {render}
        </>
    )
}

export interface IRoom {
    Id: string;
    OwnerId: string;
    Name: string;
}