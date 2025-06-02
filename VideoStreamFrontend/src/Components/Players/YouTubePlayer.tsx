import YouTube, {YouTubePlayer as youtubePlayer, YouTubeProps} from "react-youtube";
import {useContext, useEffect, useState} from "react";
import {HubContext} from "../../Contexts/HubContext.tsx";
import * as hubConstants from "../../Constants/HubFunctionNames.ts";
import {VideoStatus} from "../../Constants/constants.tsx";
import {RoomContext} from "../../Contexts/RoomContext.tsx";

export default function YouTubePlayer({queueId, videoId}) {
    const hub = useContext(HubContext);
    const roomId = useContext(RoomContext);
    const [player, setPlayer] = useState<youtubePlayer>(null);

    function onPlayerReady(event)  {
        setPlayer(event.target);
    }

    const opts = {
        playerVars: {
            autoplay: 0,

        }
    }

    useEffect(() => {
        hub.on(hubConstants.StatusChange, (status : VideoStatus) => {
            switch (status) {
                case VideoStatus.Paused:
                    player.pauseVideo();
                    break;
                case VideoStatus.Playing:
                    player.playVideo();
                    break;
            }
        })
    })

    function onPause() {
        console.log("pausing...")
        hub.invoke(hubConstants.StatusChange, VideoStatus.Paused).catch((value) => console.log(value));
    }

    return <div className={"h-full"}>
        <YouTube videoId={videoId} opts={opts} onPause={onPause} onReady={onPlayerReady}/>
    </div>
}