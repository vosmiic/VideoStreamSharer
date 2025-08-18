import {useParams} from "react-router-dom";
import {useEffect, useRef} from "react";
import {GetStream} from "../../Helpers/ApiCalls.tsx";
import MediaMTXWebRTCReader from "../../Helpers/MediaMTXWebRTCReader.tsx";

export default function Index() {
    const params = useParams();
    const videoPlayerRef = useRef<HTMLVideoElement>(null);

    useEffect(() => {
        GetStream(params.userId).then((response : Response) => {
            if (response.ok) {
                response.json().then(json => {
                    new MediaMTXWebRTCReader({
                        url: json.OutputUrl,
                        onError: (err) => {
                            console.log(err);
                        },
                        onTrack: (evt) => {
                            videoPlayerRef.current.srcObject = evt.streams[0];
                        },
                    });
                })
            }
        }).catch(() => {

        });
    }, []);

    return <>
        <video ref={videoPlayerRef} controls={true} />
    </>
}