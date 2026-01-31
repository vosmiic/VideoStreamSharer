import {useParams} from "react-router-dom";
import {useEffect, useRef, useState} from "react";
import {GetStream} from "../../Helpers/ApiCalls";
import {IStream} from "../../Interfaces/IStream";
import MediaMTXWebRTCReader from "../../Helpers/MediaMTXWebRTCReader";
import {HubConnectionBuilder, HubConnectionState} from "@microsoft/signalr";
import {API_URL} from "../../Constants/constants";

export default function Index() {
    const params = useParams();
    const videoPlayerRef = useRef<HTMLVideoElement>(null);
    const [streamData, setStreamData] = useState<IStream>();
    const [isLive, setIsLive] = useState<boolean>();

    const hubConnection = new HubConnectionBuilder()
        .withUrl(`${API_URL}/streamHub?userId=${params.userId}`)
        .build();

    if (hubConnection.state == HubConnectionState.Disconnected) {
        hubConnection.start();
    }

    useEffect(() => {
        if (!params.userId) return;
        GetStream(params.userId).then((response : Response) => {
            if (response.ok) {
                response.json().then((json : IStream) => {
                    setStreamData(json);
                    new MediaMTXWebRTCReader({
                        url: json.OutputUrl,
                        onError: (err) => {
                            console.log(err);
                        },
                        onTrack: (evt) => {
                            setIsLive(true);
                            if (videoPlayerRef.current) {
                                videoPlayerRef.current.srcObject = evt.streams[0];
                                videoPlayerRef.current.play().catch((error) => {
                                    if (videoPlayerRef.current && error.name == "NotAllowedError") {
                                        videoPlayerRef.current.volume = 0;
                                        videoPlayerRef.current.play();
                                    }
                                });
                            }
                        },
                    });
                })
            }
        }).catch(() => {

        });
    }, []);

    useEffect(() => {
        hubConnection.on("IsLive", () => {
            if (streamData?.OutputUrl) {
                new MediaMTXWebRTCReader({
                    url: streamData.OutputUrl,
                    onError: (err) => {
                        console.log(err);
                    },
                    onTrack: (evt) => {
                        setIsLive(true);
                        if (videoPlayerRef.current) {
                            videoPlayerRef.current.srcObject = evt.streams[0];
                            videoPlayerRef.current.play().catch((error) => {
                                if (videoPlayerRef.current && error.name == "NotAllowedError") {
                                    videoPlayerRef.current.volume = 0;
                                    videoPlayerRef.current.play();
                                }
                            });
                        }
                    },
                });
            }
        });

        hubConnection.on("Offline", () => {
            setIsLive(false);
            if (videoPlayerRef.current)
                videoPlayerRef.current.pause();
        })
    }, [hubConnection, streamData?.OutputUrl])

    if (params.userId == undefined) {
        return <div>User ID not provided</div>
    }

    return <div className={"flex"}>
        <div className={"flex w-4/5 flex-col"}>
            <div className={"w-full"}>
                <video ref={videoPlayerRef} controls={true} className={"w-full"} />
            </div>
            <div className={"flex flex-wrap"}>
                {isLive ?
                    <div>🔴 LIVE</div> :
                    <div>OFFLINE</div>}
                <div>{streamData?.Name}</div>
            </div>
            {streamData?.InputUrl ? <div>
                Your stream key: {streamData.InputUrl}
            </div> : <></>}
        </div>
        <div className={"flex-initial w-1/5 bg-amber-600"}>
            chat goes here
        </div>
    </div>
}