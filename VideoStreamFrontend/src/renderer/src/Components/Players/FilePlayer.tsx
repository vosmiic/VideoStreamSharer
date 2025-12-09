import {useCallback, useContext, useEffect, useRef, useState} from "react";
import {HubContext} from "../../Contexts/HubContext.tsx";
import StreamUrl from "../../Models/StreamUrl.tsx";
import {StreamType} from "../../Models/Enums/StreamType.tsx";
import {VideoStatus} from "../../Constants/constants.tsx";
import Hls from "hls.js";
import {Protocol} from "../../Models/Enums/Protocol.tsx";

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
    const initialSeek = useRef<boolean>(true);

    const updateRoomTime = useCallback((skipCounter: boolean) => {
        if (videoLoaded)
            hub.send("UpdateRoomTime", videoPlayerRef.current.currentTime, skipCounter);
    }, [hub, videoLoaded]);

    useEffect(() => {
        if (!videoPlayerRef.current || !audioPlayerRef.current) return;

        const videoPlayer = videoPlayerRef.current;
        const audioPlayer = audioPlayerRef.current;

        const videoStream = params.streamUrls?.find(url => url.StreamType === StreamType.Video || url.StreamType === StreamType.VideoAndAudio);
        if (videoStream?.Protocol == Protocol.Playlist) {
            if (Hls.isSupported()) {
                const hls = new Hls();
                hls.loadSource(videoStream.Url);
                hls.attachMedia(videoPlayerRef.current);
            } else {
                console.log("HLS is unsupported in this browser");
            }
        } else if (videoStream?.Protocol == Protocol.Raw) {
            videoPlayerRef.current.src = videoStream.Url;
        }

        const onVideoLoadedMetadata = () => {
            videoPlayer.currentTime = params.startTime;
            initialSeek.current = true;
            setVideoLoaded(true);
        };

        const onAudioLoadedMetadata = () => {
            setAudioLoaded(true);
            audioPlayer.currentTime = params.startTime;
            audioPlayer.muted = false;
            cookieStore.get("volume").then((cookie) => {
                if (cookie) {
                    console.log(`setting volume ${cookie.value} from cookie`);
                    audioPlayer.volume = parseFloat(cookie.value);
                    const volumeSlider = document.getElementById("volumeSlider") as HTMLInputElement;
                    if (volumeSlider) {
                        volumeSlider.value = (parseFloat(cookie.value) * 100).toString();
                    }
                }
            });
        };

        const onAudioPlay = () => {
            if (videoPlayer.currentTime == 0 || videoPlayer.paused || videoPlayer.ended || videoPlayer.readyState <= 2)
                audioPlayer.pause();
        };

        videoPlayer.addEventListener("loadeddata", onVideoLoadedMetadata);
        audioPlayer.addEventListener("loadedmetadata", onAudioLoadedMetadata);
        audioPlayer.addEventListener("play", onAudioPlay);

        // Autoplay logic
        if (params.autoplay && videoPlayer.paused) {
            videoPlayer.play().catch(() => {
                audioPlayer.volume = 0;
                audioPlayer.play();
            });
            audioPlayer.play().then(() => {
                // audioPlayer.volume = getVolumeCookie() ?? 50;
            }, () => {
                audioPlayer.volume = 0;
                audioPlayer.play();
            });
        }

        // Sync check interval
        const syncInterval = setInterval(() => {
            if (videoPlayer.currentTime > 0 && !videoPlayer.paused && !videoPlayer.ended && videoLoaded) {
                updateRoomTime(false);
                if (audioPlayer.paused) {
                    audioPlayer.play().catch(() => {
                        // ignore errors caused by this so we don't fill the console
                    });
                }
            }
        }, 1000);

        return () => {
            videoPlayer.removeEventListener("loadedmetadata", onVideoLoadedMetadata);
            audioPlayer.removeEventListener("loadedmetadata", onAudioLoadedMetadata);
            audioPlayer.removeEventListener("play", onAudioPlay);
            clearInterval(syncInterval);
        };

    }, [params.autoplay, params.startTime, updateRoomTime, videoLoaded]);

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
        // dont send initial seek to server
        if (!initialSeek.current) {
            updateRoomTime(true);
        }
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
        const handlePauseVideo = () => {
            if (!videoPlayerRef.current.paused) {
                console.log("paused from hub");
                ignoreNextUpdate.current = true;
                videoPlayerRef.current.pause();
                audioPlayerRef.current.pause();
            }
        };

        const handlePlayVideo = () => {
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
        };

        const handleTimeUpdate = (time: number, status : number | undefined) => {
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
                            });
                        }
                        break;
                }
            }
        };

        hub.on("PauseVideo", handlePauseVideo);
        hub.on("PlayVideo", handlePlayVideo);
        hub.on("TimeUpdate", handleTimeUpdate);

        // Audio/Video sync interval
        const syncInterval = setInterval(() => {
            if (videoPlayerRef.current && audioPlayerRef.current &&
                (videoPlayerRef.current.currentTime > 0 && !videoPlayerRef.current.paused && !videoPlayerRef.current.ended) &&
                audioPlayerRef.current.currentTime != videoPlayerRef.current.currentTime &&
                (audioPlayerRef.current.currentTime < videoPlayerRef.current.currentTime - 0.25 ||
                    audioPlayerRef.current.currentTime > videoPlayerRef.current.currentTime + 0.25)) {
                audioPlayerRef.current.currentTime = videoPlayerRef.current.currentTime;
                console.log("video and audio out of sync, syncing");
            }
        }, 1000);

        return () => {
            hub.off("PauseVideo", handlePauseVideo);
            hub.off("PlayVideo", handlePlayVideo);
            hub.off("TimeUpdate", handleTimeUpdate);
            clearInterval(syncInterval);
        };
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
        </video>
        <audio className={"hidden"} ref={audioPlayerRef} preload={"auto"} controls={true}>
            {/*<source src={params.streamUrls?.find(url => url.StreamType === StreamType.Audio)?.Url}/>*/}
        </audio>
        <input type={"range"} id={"volumeSlider"} onChange={(element) => changeVolume(element.target.value)}/>
    </div>
}
