import {useContext, useEffect, useRef, useState} from "react";
import {HubContext} from "../../Contexts/HubContext.tsx";
import StreamUrl from "../../Models/StreamUrl.tsx";
import {StreamType} from "../../Models/Enums/StreamType.tsx";

export default function FilePlayer(params: {streamUrls : Array<StreamUrl>}) {
    const hub = useContext(HubContext);
    const videoPlayerRef = useRef<HTMLVideoElement>(null);
    const audioPlayerRef = useRef<HTMLAudioElement>(null);
    const [urls, setUrls] = useState(params.streamUrls);

    useEffect(() => {
        videoPlayerRef.current.addEventListener("play", () => {
            audioPlayerRef.current.play();
        })

        videoPlayerRef.current.addEventListener("pause", () => {
            audioPlayerRef.current.pause();
        })

        hub.on("LoadVideo", (urlsOfNextVideo : StreamUrl[] | null) => {
            if (urlsOfNextVideo != null) {
                setUrls(urlsOfNextVideo);
            }
        })
    }, [hub]);


    return (
        <div id="player">
            <video ref={videoPlayerRef} controls={true} preload={"auto"}>
                <source src={urls.find(url => url.StreamType === StreamType.Video)?.Url} />
            </video>
            <audio ref={audioPlayerRef} preload={"auto"}>
                <source src={urls.find(url => url.StreamType === StreamType.Audio)?.Url} />
            </audio>
        </div>
    )
}