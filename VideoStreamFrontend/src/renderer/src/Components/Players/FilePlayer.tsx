import {useCallback, useContext, useEffect, useRef, useState} from "react";
import {HubContext} from "../../Contexts/HubContext.tsx";
import StreamUrl from "../../Models/StreamUrl.tsx";
import {StreamType} from "../../Models/Enums/StreamType.tsx";
import {VideoStatus} from "../../Constants/constants.tsx";
import Hls from "hls.js";

export default function FilePlayer(params: {
    videoId: string,
    streamUrls: Array<StreamUrl>,
    autoplay: boolean,
    startTime: number
}) {
    const hub = useContext(HubContext);
    const videoPlayerRef = useRef<HTMLVideoElement>();
    const audioPlayerRef = useRef<HTMLAudioElement>();
    const [videoLoaded, setVideoLoaded] = useState<boolean>(false);
    const [audioLoaded, setAudioLoaded] = useState<boolean>(false);
    const ignoreNextUpdate = useRef<boolean>(false);
    const hlsRef = useRef<Hls | null>(null);

    useEffect(() => {
        // getVideoSource();

        if (videoPlayerRef.current) {
            if (params.autoplay) {
                // videoPlayerRef.current.load();
                // audioPlayerRef.current.load();

                videoPlayerRef.current.play().catch(() => {
                    audioPlayerRef.current.volume = 0;
                    audioPlayerRef.current.play();
                });
                audioPlayerRef.current.play().then(() => {
                    // audioPlayerRef.current.volume = getVolumeCookie() ?? 50;
                }, () => {
                    audioPlayerRef.current.volume = 0;
                    audioPlayerRef.current.play();
                });
            } else {
                videoPlayerRef.current.load();
                audioPlayerRef.current.load();
            }

            setInterval(() => {
                if (videoPlayerRef.current && videoPlayerRef.current.currentTime > 0 && !videoPlayerRef.current.paused && !videoPlayerRef.current.ended && videoLoaded) {
                    updateRoomTime(false);
                    if (audioPlayerRef.current?.paused) {
                        audioPlayerRef.current.play().catch(() => {
                            // ignore errors caused by this so we don't fill the console
                        })
                    }
                }
            }, 1000);

            videoPlayerRef.current.addEventListener("loadedmetadata", () => {
                videoPlayerRef.current.currentTime = params.startTime;
                setVideoLoaded(true);
            });

            audioPlayerRef.current.addEventListener("loadedmetadata", () => {
                setAudioLoaded(true);
                audioPlayerRef.current.currentTime = params.startTime;
                audioPlayerRef.current.muted = false;
                cookieStore.get("volume").then((cookie) => {
                    if (cookie) {
                        console.log(`setting volume ${cookie.value} from cookie`);
                        audioPlayerRef.current.volume = cookie.value;
                        document.getElementById("volumeSlider").value = cookie.value * 100;
                    }
                });
            })

            audioPlayerRef.current.addEventListener("play", () => {
                if (videoPlayerRef.current.currentTime == 0 || videoPlayerRef.current.paused || videoPlayerRef.current.ended || videoPlayerRef.current.readyState <= 2)
                    audioPlayerRef.current.pause();
            });
        }
    });

    const updateRoomTime = useCallback((skipCounter: boolean) => {
        hub.send("UpdateRoomTime", videoPlayerRef.current.currentTime, skipCounter);
    }, [hub]);

    function onPlay(event : Event) {
        console.log("play")
        console.log(videoPlayerRef.current.seeking)
        if (!videoPlayerRef.current.seeking) {
            audioPlayerRef.current.play().catch((error) => {
                console.log("cant play audio, muting", error)
                audioPlayerRef.current.volume = 0;
                audioPlayerRef.current.play();
            });
            if (event.isTrusted && !ignoreNextUpdate.current)
                hub.send("PlayVideo");
            if (ignoreNextUpdate.current) {
                console.log("played from hub; not sending update to server");
                ignoreNextUpdate.current = false;
            }
        }
    }

    function onPause(event : Event) {
        console.log("pause");
        audioPlayerRef.current.pause();
        if (event.isTrusted && !ignoreNextUpdate.current)
            hub.send("PauseVideo");
        if (ignoreNextUpdate.current) {
            console.log("paused from hub; not sending update to server");
            ignoreNextUpdate.current = false;
        }
    }

    function onSeeked() {
        console.log("seeked")
        audioPlayerRef.current.currentTime = videoPlayerRef.current.currentTime;
        audioPlayerRef.current.play().catch(() => {
            audioPlayerRef.current.volume = 0;
            audioPlayerRef.current.play();
        });
        // forcibly update room time
        updateRoomTime(true);
    }

    function onRateChange() {
        console.log("ratechange")
        audioPlayerRef.current.playbackRate = videoPlayerRef.current.playbackRate;
    }

    function onSeeking() {
        console.log("seeking")
        audioPlayerRef.current.currentTime = videoPlayerRef.current.currentTime;
    }

    function onEnded() {
        console.log("ended");
        hub.send("FinishedVideo", params.videoId);
    }

    useEffect(() => {
        syncControl();

        hub.on("PauseVideo", () => {
            if (!videoPlayerRef.current.paused) {
                console.log("paused from hub");
                ignoreNextUpdate.current = true;
                videoPlayerRef.current.pause();
                audioPlayerRef.current.pause();
            }
        });

        hub.on("PlayVideo", () => {
            if (videoPlayerRef.current.paused) {
                console.log("played from hub");
                ignoreNextUpdate.current = true;
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

        hub.on("TimeUpdate", (time: number, status : number | undefined) => {
            if (videoPlayerRef.current.currentTime < time - 1 || videoPlayerRef.current.currentTime > time + 1) {
                videoPlayerRef.current.currentTime = time;
                audioPlayerRef.current.currentTime = time;
                console.log("out of sync, seeking")
            }
            if (status) {
                switch (status as VideoStatus) {
                    case VideoStatus.Paused:
                        if (!videoPlayerRef.current?.paused) {
                            ignoreNextUpdate.current = true;
                            videoPlayerRef.current?.pause();
                            audioPlayerRef.current?.pause();
                        }
                        break;
                    case VideoStatus.Playing:
                        if (videoPlayerRef.current?.paused) {
                            ignoreNextUpdate.current = true;
                            videoPlayerRef.current?.play().catch(() => {
                                audioPlayerRef.current.volume = 0;
                                audioPlayerRef.current?.play();
                            });
                            audioPlayerRef.current?.play().catch(() => {
                                audioPlayerRef.current.volume = 0;
                                audioPlayerRef.current?.play();
                            });                        }
                        break;
                }
            }
        });

        function syncControl() {
            setInterval(() => {
                if (videoPlayerRef.current && audioPlayerRef.current && (videoPlayerRef.current.currentTime > 0 && !videoPlayerRef.current.paused && !videoPlayerRef.current.ended) &&
                    audioPlayerRef.current.currentTime != videoPlayerRef.current.currentTime &&
                    (audioPlayerRef.current.currentTime < videoPlayerRef.current.currentTime - 0.25 || audioPlayerRef.current.currentTime > videoPlayerRef.current.currentTime + 0.25)) {
                    audioPlayerRef.current.currentTime = videoPlayerRef.current.currentTime;
                    console.log("video and audio out of sync, syncing");
                }
            }, 1000);
        }
    }, [hub, params.videoId, updateRoomTime]);

    function changeVolume(newVolume: number) {
        const scaledVolume = newVolume / 100;
        const paused = !audioPlayerRef.current.paused && videoPlayerRef.current.paused; // covers odd behavior where audio plays automatically when changing volume
        audioPlayerRef.current.volume = scaledVolume;
        audioPlayerRef.current.muted = false;
        cookieStore.set("volume", scaledVolume.toString());
        if (paused)
            audioPlayerRef.current.pause();
    }

    // function getVideoSource() {
    //     const url = params.streamUrls?.find(url => url.StreamType === StreamType.Video || url.StreamType === StreamType.VideoAndAudio)?.Url;
    //     if (!url) return;
    //     const mediaSource = new MediaSource();
    //     videoSource.current = URL.createObjectURL(mediaSource);
    //     console.log(videoSource.current);
    //     console.log(url);
    //
    //     mediaSource.addEventListener('sourceopen', async () => {
    //         const sourceBuffer = mediaSource.addSourceBuffer('video/mp4; codecs="avc1.640020, mp4a.40.2"');
    //
    //         // Fetch with custom headers
    //         const response = await fetch(url);
    //
    //         const videoData = await response.arrayBuffer();
    //         sourceBuffer.appendBuffer(videoData);
    //
    //         sourceBuffer.addEventListener('updateend', () => {
    //             if (!sourceBuffer.updating && mediaSource.readyState === 'open') {
    //                 mediaSource.endOfStream();
    //             }
    //         });
    //     });
    // }


    return <div id="player">
        <video ref={videoPlayerRef}
               className={"min-w-full"}
               controls={true}
               preload={"metadata"}
               muted={true}
               onPlay={(event) => onPlay(event.nativeEvent)}
               onPause={(event) => onPause(event.nativeEvent)}
               onSeeked={onSeeked}
               onRateChange={onRateChange}
               onSeeking={onSeeking}
               onEnded={onEnded}
        >
            <source src={params.streamUrls?.find(url => url.StreamType === StreamType.Video || url.StreamType === StreamType.VideoAndAudio)?.Url}/>
        </video>
        <audio className={"hidden"} ref={audioPlayerRef} preload={"auto"} controls={true}>
            <source src={params.streamUrls?.find(url => url.StreamType === StreamType.Audio)?.Url}/>
        </audio>
        <input type={"range"} id={"volumeSlider"} onChange={(element) => changeVolume(element.target.value)}/>
    </div>
}
