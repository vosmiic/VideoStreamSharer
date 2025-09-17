import {useSortable} from "@dnd-kit/sortable";
import {CSS} from "@dnd-kit/utilities";
import {useContext} from "react";
import {IQueue} from "../../Interfaces/IQueue.tsx";
import {TrashIcon} from "@heroicons/react/24/solid";
import {HubContext} from "../../Contexts/HubContext.tsx";

export default function QueueItem(props: { key : string, queueItem : IQueue}) {
    const hub = useContext(HubContext);
    const {
        attributes,
        listeners,
        setNodeRef,
        transform,
        transition
    } = useSortable({id: props.queueItem.Id});

    const style = {
        transform: CSS.Transform.toString(transform),
        transition
    }

    function handleOnClick() {
        hub.send("DeleteVideo", props.queueItem.Id);
    }

    return (
        <div style={style} className={"grid grid-flow-row gap-2 min-h-32"}>
            <div className={"row-span-7 flex relative"}>
                <div ref={setNodeRef} {...attributes} {...listeners}>
                    <img className={"z-0"} src={props.queueItem.ThumbnailLocation} />
                </div>
                <button className={"absolute z-10 right-1 bottom-1"} onClick={handleOnClick}><TrashIcon className={"size-5"} /></button>
            </div>
            <div className={"row-span-3 text-center"}>
                <p>{props.queueItem.Title}</p>
            </div>
        </div>
    )
}