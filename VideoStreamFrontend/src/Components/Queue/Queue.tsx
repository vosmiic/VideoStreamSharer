import {closestCenter, DndContext, KeyboardSensor, PointerSensor, useSensor, useSensors} from "@dnd-kit/core";
import {useContext, useState} from "react";
import {
    arrayMove,
    SortableContext,
    sortableKeyboardCoordinates,
    verticalListSortingStrategy
} from "@dnd-kit/sortable";
import QueueItem from "./QueueItem.tsx";
import QueueAdd from "./QueueAdd.tsx";
import {IQueue} from "../../Interfaces/IQueue.tsx";
import {ChangeQueueOrder} from "../../Helpers/ApiCalls.tsx";
import {RoomContext} from "../../Contexts/RoomContext.tsx";

export default function Queue({queueItems}) {
    const roomId = useContext(RoomContext);
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
        let newQueue = [];

        if (beingMoved.id !== to.id) {
            setItems((items) => {
                const queueItemsIds = items.map(queueItem => queueItem.Id);
                const oldIndex = queueItemsIds.indexOf(beingMoved.id);
                const newIndex = queueItemsIds.indexOf(to.id);

                newQueue = arrayMove(items, oldIndex, newIndex);
                for (var i = 0; i < newQueue.length; i++) {
                    newQueue[i].Order = i;
                }
                return newQueue;
            })
        }

        const changedItems = newQueue.filter((queueItem, i) => queueItem != items[i]);
        ChangeQueueOrder(roomId, changedItems)
            .catch((error) => {
                console.log(error); // todo needs to be properly handled
        });
    }

    function sortQueueItems(a : IQueue, b : IQueue) {
        if (a.Order > b.Order) {
            return 1;
        } else if (a.Order < b.Order) {
            return -1;
        } else {
            return 0;
        }
    }

    return (
        <div>
        <QueueAdd />
        <DndContext
            sensors={sensors}
            collisionDetection={closestCenter}
            onDragEnd={handleDragEnd}>
            <div className={"grid grid-cols-1 gap-4"}>
                <SortableContext items={items} strategy={verticalListSortingStrategy}>
                    {items.sort(sortQueueItems).map(item => <QueueItem key={item.Id} id={item.Id} title={item.Title} />)}
                </SortableContext>
            </div>
        </DndContext>
        </div>
    )
}