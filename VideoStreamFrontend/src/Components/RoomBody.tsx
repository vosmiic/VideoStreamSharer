import {useContext, useEffect, useMemo, useState} from "react";
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
    const [roomStatus, setRoomStatus] = useState<VideoStatus>();
    const [currentTime, setCurrentTime] = useState<number>();
    const [queue, setQueue] = useState<Array<IQueue>>([]);
    const [streamUrls, setStreamUrls] = useState<Array<StreamUrl>>([]);
    const [users, setUsers] = useState<string[]>([]);
    const filePlayer = useMemo(() => <FilePlayer videoId={queue.find(queue => queue.Order == 0)?.Id} streamUrls={streamUrls} autoplay={roomStatus == VideoStatus.Playing} startTime={currentTime}/>, [currentTime, roomStatus, streamUrls]);
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
                                const parsed = json as GetRoomResponse;
                                setRoomStatus(parsed.Room.Status);
                                setCurrentTime(parsed.Room.CurrentTime);
                                setQueueItems(parsed.Room.Queue);
                                setStreamUrls(parsed.Room.StreamUrls);
                                setUsers(parsed.Users);
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
        const handleVideoFinished = (Data: {Room: {StreamUrls : StreamUrl[], Queue : IQueue[]}}) => {
            setStreamUrls(Data.Room.StreamUrls);
            setRoomStatus(VideoStatus.Playing);
            setCurrentTime(0);
            setQueueItems(Data.Room.Queue);
        };

        hub.on("VideoFinished", handleVideoFinished);

        // Cleanup function to remove the listener
        return () => {
            hub.off("VideoFinished", handleVideoFinished);
        };
    }, [hub]); // Remove getRoom from dependencies to avoid stale closures

    useEffect(() => {
        const handleVideoChanged = (streamUrls : StreamUrl[]) => {
            setStreamUrls(streamUrls);
            setRoomStatus(VideoStatus.Playing);
            setCurrentTime(0);
        };

        hub.on("VideoChanged", handleVideoChanged)

        hub.on("LoadVideo", (urlsOfNextVideo: Array<StreamUrl>) => {
            if (urlsOfNextVideo.length > 0) {
                setStreamUrls(urlsOfNextVideo);
            }
        });

        return () => {
            hub.off("VideoChanged", handleVideoChanged)
        }
    }, [hub])

    function setQueueItems(newQueue : IQueue[]) {
        setQueue(newQueue);
    }

    function LoadedState() {
        return <RoomContext.Provider value={params.roomId}>
            <div className={"flex w-full"}>
                <div className={"flex-auto w-1/6 bg-red-500"}>
                    <Queue queueItems={queue} setQueueItems={(queueItems) => setQueueItems(queueItems)} />
                </div>
                <div className={"flex-auto w-4/6 bg-yellow-500"}>
                    {queue && (streamUrls && streamUrls.length > 0) ?
                        filePlayer
                        : <></>}
                </div>
                <div className={"flex-auto w-1/6 bg-blue-500"}>
                    <Users users={users}/>
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