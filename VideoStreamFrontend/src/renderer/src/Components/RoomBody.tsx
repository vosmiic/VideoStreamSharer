import {useContext, useEffect, useMemo, useState} from "react";
import * as apiCalls from "../Helpers/ApiCalls";
import NotFound from "./NotFound";
import Loading from "./Loading";
import {GetRoomResponse} from "../Interfaces/IRoom";
import Queue from "./Queue/Queue";
import {RoomContext} from "../Contexts/RoomContext";
import {HubContext} from "../Contexts/HubContext";
import {HubConnectionState} from "@microsoft/signalr";
import FilePlayer from "./Players/FilePlayer";
import {RecentRoomsCookieName, VideoStatus, MaxRecentRoomsCount} from "../Constants/constants";
import {IQueue} from "../Interfaces/IQueue";
import StreamUrl from "../Models/StreamUrl";
import RoomRightPanel from "./RoomRightPanel";
import {IRoomName} from "../Interfaces/IHome";

export default function RoomBody(params: {roomId: string}) {
    const hub = useContext(HubContext);
    const [roomStatus, setRoomStatus] = useState<VideoStatus>();
    const [currentTime, setCurrentTime] = useState<number>();
    const [queue, setQueue] = useState<Array<IQueue>>([]);
    const [currentVideoId, setCurrentVideoId] = useState<string>();
    const [streamUrls, setStreamUrls] = useState<Array<StreamUrl>>([]);
    const [users, setUsers] = useState<string[]>([]);
    const filePlayer = useMemo(() => <FilePlayer videoId={currentVideoId} streamUrls={streamUrls} autoplay={roomStatus == VideoStatus.Playing} startTime={currentTime}/>, [currentTime, currentVideoId, roomStatus, streamUrls]);
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
                                // hub.send("VisitedRoom");
                                cookieStore.get(RecentRoomsCookieName).then((recentRoomsCookieItem) => {
                                    const newCookieItem: IRoomName = {
                                        Id: parsed.Room.Id,
                                        Name: parsed.Room.Name,
                                        VisitDateTime: new Date()
                                    };
                                    const cookieExpiration = new Date();
                                    cookieExpiration.setDate(cookieExpiration.getDate() + 365);
                                    const cookieOptions: CookieInit = {
                                        name: RecentRoomsCookieName,
                                        value: "",
                                        expires: cookieExpiration.getTime()
                                    }
                                    if (recentRoomsCookieItem == null || !recentRoomsCookieItem.value) {
                                        cookieOptions.value = JSON.stringify([newCookieItem]);
                                        cookieStore.set(cookieOptions).catch((e) => console.log(e));
                                        console.log(cookieOptions);
                                    } else {
                                        const existingCookieItem = JSON.parse(recentRoomsCookieItem.value) as Array<IRoomName>;
                                        const existingRoomName = existingCookieItem.find(roomName => roomName.Id == params.roomId);
                                        moveRoomToMostRecent(existingCookieItem, existingRoomName ?? newCookieItem, existingRoomName !== undefined);
                                        cookieOptions.value = JSON.stringify(existingCookieItem);
                                        cookieStore.set(cookieOptions);
                                    }
                                })

                            })
                        } else {
                            setLoadState(1);
                        }
                    }
                });
        }

        function moveRoomToMostRecent(currentList : IRoomName[], newItem : IRoomName, alreadyExists : boolean) {
            if (currentList.length === MaxRecentRoomsCount) {
                currentList.sort((a, b) => a.VisitDateTime.getTime() - b.VisitDateTime.getTime()).pop();
            }

            if (alreadyExists) {
                newItem.VisitDateTime = new Date();
            } else {
                currentList.push(newItem);
            }
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
        const videoId = newQueue.find(queue => queue.Order == 0)?.Id;
        if (videoId)
            setCurrentVideoId(videoId);
        setQueue(newQueue);
    }

    function LoadedState() {
        return <RoomContext.Provider value={params.roomId}>
            <div className={"flex w-full"}>
                <div className={"flex-auto w-1/6 bg-red-500"}>
                    <Queue queueItems={queue} setQueueItems={(queueItems) => setQueueItems(queueItems)} />
                </div>
                <div className={"flex-auto w-4/6 bg-yellow-500"}>
                    {currentVideoId && (streamUrls && streamUrls.length > 0) ?
                        filePlayer
                        : <></>}
                </div>
                <div className={"flex-auto w-1/6 bg-blue-500"}>
                    <RoomRightPanel users={users}/>
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