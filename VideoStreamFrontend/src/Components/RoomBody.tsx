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
import {VideoStatus} from "../Constants/constants.tsx";
import {IQueue} from "../Interfaces/IQueue.tsx";
import StreamUrl from "../Models/StreamUrl.tsx";

export default function RoomBody(params: {roomId: string}) {
    const hub = useContext(HubContext);
    const [getRoom, setGetRoom] = useState<GetRoomResponse>();
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
        const handleVideoFinished = (newRoomData) => {
            const newItems : IQueue[] = newRoomData.room.queue.map(queueItem => {
                return {
                    Title : queueItem.title,
                    ThumbnailLocation : queueItem.thumbnailLocation,
                    Order : queueItem.order,
                    Id : queueItem.id,
                    Type : queueItem.type
                } as IQueue
            });
            const newUrls : StreamUrl[] = newRoomData.room.streamUrls.map(streamUrl => {
                return {
                    Url : streamUrl.url,
                    StreamType: streamUrl.streamType
                } as StreamUrl
            });
            setGetRoom(previousRoom => {
                if (!previousRoom) return previousRoom;
                return {
                    ...previousRoom,
                    Room: {
                        ...previousRoom.Room,
                        Status: VideoStatus.Playing,
                        CurrentTime: 0,
                        Queue: newItems,
                        StreamUrls: newUrls
                    }
                } as GetRoomResponse;
            });
        };

        hub.on("VideoFinished", handleVideoFinished);

        // Cleanup function to remove the listener
        return () => {
            hub.off("VideoFinished", handleVideoFinished);
        };
    }, [hub]); // Remove getRoom from dependencies to avoid stale closures

    useEffect(() => {
        console.log('getRoom state changed:', getRoom);
    }, [getRoom]);


    function LoadedState() {
        return <RoomContext.Provider value={params.roomId}>
            <div className={"flex w-full"}>
                <div className={"flex-auto w-1/6 bg-red-500"}>
                    <Queue queueItems={getRoom.Room.Queue} />
                </div>
                <div className={"flex-auto w-4/6 bg-yellow-500"}>
                    <FilePlayer videoId={getRoom?.Room.Queue.find(queue => queue.Order == 0)?.Id} streamUrls={getRoom?.Room.StreamUrls} autoplay={getRoom?.Room.Status == VideoStatus.Playing} startTime={getRoom?.Room.CurrentTime}/>
                </div>
                <div className={"flex-auto w-1/6 bg-blue-500"}>
                    <Users users={getRoom.Users}/>
                </div>
            </div>
        </RoomContext.Provider>
    }

    // Render based on load state
    function renderContent() {
        switch (loadState) {
            case 0:
                return <Loading />;
            case 1:
                return <NotFound />;
            case 2:
                return LoadedState();
            default:
                return <Loading />;
        }
    }

    return (
        <>
            <h1>Room</h1>
            {renderContent()}
        </>
    )
}