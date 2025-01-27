import {useSortable} from "@dnd-kit/sortable";
import {CSS} from "@dnd-kit/utilities";


export default function QueueItem(props) {
    const {
        attributes,
        listeners,
        setNodeRef,
        transform,
        transition
    } = useSortable({id: props.id});

    const style = {
        transform: CSS.Transform.toString(transform),
        transition
    }

    return (
        <div ref={setNodeRef} style={style} {...attributes} {...listeners} className={"grid grid-flow-row gap-2 min-h-32"}>
            <div className={"row-span-7"}>

            </div>
            <div className={"row-span-3 text-center"}>
                <p>{props.title}</p>
            </div>
        </div>
    )
}