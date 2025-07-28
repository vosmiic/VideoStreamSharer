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
        syncControl();

        hub.on("LoadVideo", (urlsOfNextVideo : StreamUrl[] | null) => {
            if (urlsOfNextVideo != null) {
                setUrls(urlsOfNextVideo);
            }
        })

        function syncControl() {
            videoPlayerRef.current.addEventListener("play", () => {
                console.log("play")
                console.log(videoPlayerRef.current.seeking)
                if (!videoPlayerRef.current.seeking) {
                    audioPlayerRef.current.play();
                }
            });

            videoPlayerRef.current.addEventListener("pause", () => {
                console.log("pause")
                audioPlayerRef.current.pause();
            });

            videoPlayerRef.current.addEventListener("seeked", () => {
                console.log("seeked")
                audioPlayerRef.current.currentTime = videoPlayerRef.current.currentTime;
                audioPlayerRef.current.play();
            });

            videoPlayerRef.current.addEventListener("ratechange", () => {
                console.log("ratechange")
                audioPlayerRef.current.playbackRate = videoPlayerRef.current.playbackRate;
            });

            videoPlayerRef.current.addEventListener("seeking", () => {
                console.log("seeking")
                audioPlayerRef.current.currentTime = videoPlayerRef.current.currentTime;
                audioPlayerRef.current.pause();
            });
        }
    }, [hub]);

    function changeVolume(element) {
        audioPlayerRef.current.volume = element.target.value / 100;
    }


    return (
        <div id="player">
            <video ref={videoPlayerRef} controls={true} preload={"auto"}>
                <source src={urls.find(url => url.StreamType === StreamType.Video)?.Url} />
            </video>
            <audio ref={audioPlayerRef} preload={"auto"}>
                <source src={urls.find(url => url.StreamType === StreamType.Audio)?.Url} />
            </audio>
            <input type={"range"} onChange={changeVolume} />
        </div>
    )
}