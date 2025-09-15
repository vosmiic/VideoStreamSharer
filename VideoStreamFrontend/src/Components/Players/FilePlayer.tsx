import {useContext, useEffect, useRef} from "react";
import {HubContext} from "../../Contexts/HubContext.tsx";
import StreamUrl from "../../Models/StreamUrl.tsx";
import {StreamType} from "../../Models/Enums/StreamType.tsx";

export default function FilePlayer(params: { videoId: string, streamUrls: Array<StreamUrl>, autoplay: boolean, startTime: number }) {
    const hub = useContext(HubContext);
    const videoPlayerRef = useRef<HTMLVideoElement>(null);
    const audioPlayerRef = useRef<HTMLAudioElement>(null);

    useEffect(() => {
        if (params.autoplay) {
            videoPlayerRef.current.load();
            audioPlayerRef.current.load();
            videoPlayerRef.current.play().catch(() => {
                audioPlayerRef.current.volume = 0;
                audioPlayerRef.current.play();
            });
            audioPlayerRef.current.play().catch(() => {
                audioPlayerRef.current.volume = 0;
                audioPlayerRef.current.play();
            });
        }

        setInterval(() => {
            if (videoPlayerRef.current && videoPlayerRef.current.currentTime > 0 && !videoPlayerRef.current.paused && !videoPlayerRef.current.ended) {
                updateRoomTime(false);
            }
        }, 1000);

        videoPlayerRef.current.addEventListener("loadedmetadata", () => {
            videoPlayerRef.current.currentTime = params.startTime;
        });

        audioPlayerRef.current.addEventListener("loadedmetadata", () => {
            audioPlayerRef.current.currentTime = params.startTime;
        })

        audioPlayerRef.current.addEventListener("play", () => {
            if (videoPlayerRef.current.currentTime == 0 || videoPlayerRef.current.paused || videoPlayerRef.current.ended || videoPlayerRef.current.readyState <= 2)
                audioPlayerRef.current.pause();
        });
    });

    function updateRoomTime(skipCounter: boolean) {
        hub.send("UpdateRoomTime", videoPlayerRef.current.currentTime, skipCounter);
    }

    useEffect(() => {
        syncControl();

        hub.on("LoadVideo", (urlsOfNextVideo: StreamUrl[] | null) => {
            if (urlsOfNextVideo != null) {
                params.streamUrls = urlsOfNextVideo;
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
        });

        hub.on("TimeUpdate", (time: number) => {
            if (videoPlayerRef.current.currentTime < time - 1 || videoPlayerRef.current.currentTime > time + 1) {
                videoPlayerRef.current.currentTime = time;
                audioPlayerRef.current.currentTime = time;
                console.log("out of sync, seeking")
            }
        });

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
                // forcibly update room time
                updateRoomTime(true);
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

            videoPlayerRef.current.addEventListener("ended", () => {
                console.log("ended");
                hub.send("FinishedVideo", params.videoId);
            })

            setInterval(() => {
                if (videoPlayerRef.current && audioPlayerRef.current && (videoPlayerRef.current.currentTime > 0 && !videoPlayerRef.current.paused && !videoPlayerRef.current.ended) &&
                    audioPlayerRef.current.currentTime != videoPlayerRef.current.currentTime &&
                    (audioPlayerRef.current.currentTime < videoPlayerRef.current.currentTime - 0.25 || audioPlayerRef.current.currentTime > videoPlayerRef.current.currentTime + 0.25)) {
                    audioPlayerRef.current.currentTime = videoPlayerRef.current.currentTime;
                    console.log("video and audio out of sync, syncing");
                }
            }, 1000);
        }
    }, [hub]);

    function changeVolume(element) {
        var paused = !audioPlayerRef.current.paused && videoPlayerRef.current.paused; // covers odd behavior where audio plays automatically when changing volume
        audioPlayerRef.current.volume = element.target.value / 100;
        if (paused)
            audioPlayerRef.current.pause();
    }


    return params.streamUrls ? (
        <div id="player">
            <video ref={videoPlayerRef} controls={true} preload={"auto"}>
                <source src={params.streamUrls?.find(url => url.StreamType === StreamType.Video)?.Url} />
            </video>
            <audio ref={audioPlayerRef} preload={"auto"}>
                <source src={params.streamUrls?.find(url => url.StreamType === StreamType.Audio)?.Url} />
            </audio>
            <input type={"range"} onChange={changeVolume}/>
        </div>
    ) : <></>
}