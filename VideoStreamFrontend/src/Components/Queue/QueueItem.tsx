import {useSortable} from "@dnd-kit/sortable";
import {CSS} from "@dnd-kit/utilities";
import {useState} from "react";
import {IQueue} from "../../Interfaces/IQueue.tsx";


export default function QueueItem(props) {
    const [queueItem, _] = useState<IQueue>(props.queueItem);
    const {
        attributes,
        listeners,
        setNodeRef,
        transform,
        transition
    } = useSortable({id: queueItem.Id});

    const style = {
        transform: CSS.Transform.toString(transform),
        transition
    }

    return (
        <div ref={setNodeRef} style={style} {...attributes} {...listeners} className={"grid grid-flow-row gap-2 min-h-32"}>
            <div className={"row-span-7"}>
                <img src={queueItem.ThumbnailLocation} />
            </div>
            <div className={"row-span-3 text-center"}>
                <p>{queueItem.Title}</p>
            </div>
        </div>
    )
}