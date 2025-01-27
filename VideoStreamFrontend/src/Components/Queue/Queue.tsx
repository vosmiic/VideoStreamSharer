import {closestCenter, DndContext, KeyboardSensor, PointerSensor, useSensor, useSensors} from "@dnd-kit/core";
import {useState} from "react";
import {
    arrayMove,
    SortableContext,
    sortableKeyboardCoordinates,
    verticalListSortingStrategy
} from "@dnd-kit/sortable";
import QueueItem from "./QueueItem.tsx";


export default function Queue({queueItems}) {
    const [items, setItems] = useState(queueItems);
    const sensors = useSensors(
        useSensor(PointerSensor),
        useSensor(KeyboardSensor, {
            coordinateGetter: sortableKeyboardCoordinates
        })
    );

    function handleDragEnd(event) {
        const beingMoved = event.active;
        const to = event.over;

        if (beingMoved.id !== to.id) {
            setItems((items) => {
                const queueItemsIds = items.map(queueItem => queueItem.Id);
                const oldIndex = queueItemsIds.indexOf(beingMoved.id);
                const newIndex = queueItemsIds.indexOf(to.id);

                return arrayMove(items, oldIndex, newIndex);
            })
        }
    }

    return (
        <DndContext
            sensors={sensors}
            collisionDetection={closestCenter}
            onDragEnd={handleDragEnd}>
            <div className={"grid grid-cols-1 gap-4"}>
                <SortableContext items={items} strategy={verticalListSortingStrategy}>
                    {items.map(item => <QueueItem key={item.Id} id={item.Id} title={item.Title} />)}
                </SortableContext>
            </div>
        </DndContext>
    )
}