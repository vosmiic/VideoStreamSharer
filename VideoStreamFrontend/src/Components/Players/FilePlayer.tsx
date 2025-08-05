import {useContext, useEffect, useRef, useState} from "react";
import {HubContext} from "../../Contexts/HubContext.tsx";
import StreamUrl from "../../Models/StreamUrl.tsx";
import {StreamType} from "../../Models/Enums/StreamType.tsx";

export default function FilePlayer(params: {streamUrls : Array<StreamUrl>, autoplay : boolean}) {
    const hub = useContext(HubContext);
    const videoPlayerRef = useRef<HTMLVideoElement>(null);
    const audioPlayerRef = useRef<HTMLAudioElement>(null);
    const [urls, setUrls] = useState(params.streamUrls);

    useEffect(() => {
        if (params.autoplay) {
            videoPlayerRef.current.play().catch(() => {
                audioPlayerRef.current.volume = 0;
                audioPlayerRef.current.play();
            });
            audioPlayerRef.current.play().catch(() => {
                audioPlayerRef.current.volume = 0;
                audioPlayerRef.current.play();
            });
        }
    });

    useEffect(() => {
        syncControl();

        hub.on("LoadVideo", (urlsOfNextVideo : StreamUrl[] | null) => {
            if (urlsOfNextVideo != null) {
                setUrls(urlsOfNextVideo);
            }
        });

        hub.on("PauseVideo", () => {
            if (!videoPlayerRef.current.paused) {
                videoPlayerRef.current.pause();
                audioPlayerRef.current.pause();
            }
        });

        hub.on("PlayVideo", () => {
            if (videoPlayerRef.current.paused) {
                videoPlayerRef.current.play().catch(() => {
                    audioPlayerRef.current.volume = 0;
                    audioPlayerRef.current.play();
                });
                audioPlayerRef.current.play().catch(() => {
                    audioPlayerRef.current.volume = 0;
                    audioPlayerRef.current.play();
                });
            }
        })

        function syncControl() {
            videoPlayerRef.current.addEventListener("play", (event) => {
                console.log("play")
                console.log(videoPlayerRef.current.seeking)
                if (!videoPlayerRef.current.seeking) {
                    audioPlayerRef.current.play().catch(() => {
                        audioPlayerRef.current.volume = 0;
                        audioPlayerRef.current.play();
                    });
                    if (event.isTrusted)
                        hub.send("PlayVideo");
                }
            });

            videoPlayerRef.current.addEventListener("pause", (event) => {
                console.log("pause");
                audioPlayerRef.current.pause();
                if (event.isTrusted)
                    hub.send("PauseVideo");
            });

            videoPlayerRef.current.addEventListener("seeked", () => {
                console.log("seeked")
                audioPlayerRef.current.currentTime = videoPlayerRef.current.currentTime;
                audioPlayerRef.current.play().catch(() => {
                    audioPlayerRef.current.volume = 0;
                    audioPlayerRef.current.play();
                });
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
        var paused = !audioPlayerRef.current.paused && videoPlayerRef.current.paused; // covers odd behavior where audio plays automatically when changing volume
        audioPlayerRef.current.volume = element.target.value / 100;
        if (paused)
            audioPlayerRef.current.pause();
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