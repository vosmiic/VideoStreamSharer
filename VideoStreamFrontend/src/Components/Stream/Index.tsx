import {useParams} from "react-router-dom";
import {useEffect, useRef, useState} from "react";
import {GetStream} from "../../Helpers/ApiCalls.tsx";
import MediaMTXWebRTCReader from "../../Helpers/MediaMTXWebRTCReader.tsx";
import {HubConnectionBuilder, HubConnectionState} from "@microsoft/signalr";
import {API_URL} from "../../Constants/constants.tsx";

export default function Index() {
    const params = useParams();
    const videoPlayerRef = useRef<HTMLVideoElement>(null);
    const [outputUrl, setOutputUrl] = useState<string>();

    const hubConnection = new HubConnectionBuilder()
        .withUrl(`${API_URL}/streamHub?userId=${params.userId}`)
        .build();

    if (hubConnection.state == HubConnectionState.Disconnected) {
        hubConnection.start();
    }

    useEffect(() => {
        GetStream(params.userId).then((response : Response) => {
            if (response.ok) {
                response.json().then(json => {
                    setOutputUrl(json.OutputUrl);
                    new MediaMTXWebRTCReader({
                        url: json.OutputUrl,
                        onError: (err) => {
                            console.log(err);
                        },
                        onTrack: (evt) => {
                            videoPlayerRef.current.srcObject = evt.streams[0];
                            videoPlayerRef.current.play().catch((error) => {
                                if (error.name == "NotAllowedError") {
                                    videoPlayerRef.current.volume = 0;
                                    videoPlayerRef.current.play();
                                }
                            });
                        },
                    });
                })
            }
        }).catch(() => {

        });
    }, []);

    useEffect(() => {
        hubConnection.on("IsLive", () => {
            if (outputUrl != undefined) {
                new MediaMTXWebRTCReader({
                    url: outputUrl,
                    onError: (err) => {
                        console.log(err);
                    },
                    onTrack: (evt) => {
                        videoPlayerRef.current.srcObject = evt.streams[0];
                        videoPlayerRef.current.play().catch((error) => {
                            if (error.name == "NotAllowedError") {
                                videoPlayerRef.current.volume = 0;
                                videoPlayerRef.current.play();
                            }
                        });
                    },
                });
            }
        });

        hubConnection.on("Offline", () => {
            videoPlayerRef.current.pause();
        })
    }, [hubConnection, outputUrl])

    return <>
        <video ref={videoPlayerRef} controls={true} />
    </>
}