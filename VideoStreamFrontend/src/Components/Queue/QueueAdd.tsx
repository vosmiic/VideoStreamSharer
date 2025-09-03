import {useContext, useRef, useState} from "react";
import {Button, Input} from "@headlessui/react";
import {AddToQueue, Lookup} from "../../Helpers/ApiCalls.tsx";
import {RoomContext} from "../../Contexts/RoomContext.tsx";
import {ILookup} from "../../Interfaces/ILookup.tsx";
import {IQueueAdd} from "../../Interfaces/IQueueAdd.tsx";

export default function QueueAdd() {
    const roomId: string = useContext(RoomContext);
    const [input, setInput] = useState<string>("");
    const [lookup, setLookup] = useState<ILookup>();
    const [loading, setLoading] = useState<boolean>(true);
    const [displayPreview, setDisplayPreview] = useState<boolean>(false);
    const [videoFormatId, setVideoFormatId] = useState<string>();
    const [audioFormatId, setAudioFormatId] = useState<string>();
    const modal = useRef<HTMLDialogElement>(null);

    async function handleOnLookup() {
        setDisplayPreview(true);
        setLoading(true);
        await Lookup(input)
            .then((result) => {
                if (result.ok) {
                    result.json().then((json: ILookup) => {
                        setLookup(json);
                        setLoading(false);
                        setVideoFormatId(json.VideoFormats[0].Id);
                        setAudioFormatId(json.AudioFormats[0].Id);
                    })
                    // todo alert user of success using toast
                } else {
                    // todo alert user of failure using toast
                }
            })
    }

    async function handleOnSubmit() {
        await AddToQueue(new QueueAddBody(roomId, input))
            .then((result) => {
                if (result.ok) {
                    // todo alert user of success using toast
                } else {
                    // todo alert user of failure using toast
                }
            })
    }

    function handleOpenModel() {
        modal.current.showModal();
    }

    function handleCloseModel() {
        setLoading(true);
        setDisplayPreview(false);
    }

    return (<div>
        <div className={"w-full"}>
            <div className={"flex flex-row"}>
                <Button onClick={handleOpenModel}>+</Button>
            </div>
        </div>
        <dialog id="my_modal_1" className={"modal"} ref={modal} onClose={handleCloseModel}>
            <div className={"modal-box"}>
                <h3 className={"font-bold text-lg"}>Add video</h3>
                <div className={"flex w-full"}>
                    <Input className={"grow"} type={"text"} onChange={(e) => setInput(e.target.value)}/>
                    <Button onClick={handleOnLookup}>⌕</Button>
                </div>
                {displayPreview ?
                    loading ?
                        <span className={"loading loading-spinner loading-xl"}></span>
                        :
                        <div className={"grid grid-flow-col grid-rows-3 grid-cols-3 gap-3 pt-3"}>
                            <div className={"row-span-2 col-span-2"}><img src={lookup?.ThumbnailUrl} alt={"Thumbnail"}/>
                            </div>
                            <div className={"col-span-2"}>
                                <div>ጸ {lookup?.Viewcount} | ⏱︎ {lookup?.Duration}</div>
                                <div>{lookup?.Title}</div>
                            </div>
                            <div className={""}>
                                <div>Video Format</div>
                                <select value={videoFormatId} onChange={e => setVideoFormatId(e.target.value)}>
                                    {lookup?.VideoFormats.map(format => (
                                        <option key={format.Id} value={format.Id}>{format.Value}</option>
                                    ))}
                                </select>
                            </div>
                            <div className={""}>
                                <div>Audio Format</div>
                                <select value={audioFormatId} onChange={e => setAudioFormatId(e.target.value)}>
                                    {lookup?.AudioFormats.map(format => (
                                        <option key={format.Id} value={format.Id}>{format.Value}</option>
                                    ))}
                                </select>
                            </div>
                            <div className={""}>
                                <button onClick={handleOnSubmit}>Submit</button>
                            </div>
                        </div>
                    : <></>}
                <div className={"modal-action"}>
                    <form method="dialog">
                        {/* if there is a button in form, it will close the modal */}
                        <button className={"btn"}>Close</button>
                    </form>
                </div>
            </div>
        </dialog>
    </div>)
}