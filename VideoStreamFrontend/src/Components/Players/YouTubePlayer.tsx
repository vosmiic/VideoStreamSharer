import YouTube from "react-youtube";

export default function YouTubePlayer({videoId}) {
    const opts = {
        playerVars: {
            autoplay: 0
        }
    }

    return <div className={"h-full"}>
        <YouTube videoId={videoId} opts={opts} />
    </div>
}