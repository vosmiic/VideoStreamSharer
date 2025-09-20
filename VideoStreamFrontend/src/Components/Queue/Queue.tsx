import {closestCenter, DndContext, KeyboardSensor, PointerSensor, useSensor, useSensors} from "@dnd-kit/core";
import {useContext, useEffect, useState} from "react";
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
import {HubContext} from "../../Contexts/HubContext.tsx";
import {QueueOrder} from "../../Models/QueueOrder.tsx";

export default function Queue({queueItems}) {
    const roomId = useContext(RoomContext);
    const hub = useContext(HubContext);
    const [items, setItems] = useState<IQueue[]>(queueItems || []);
    const [disableDragging, setDisableDragging] = useState<boolean>(false);
    const sensors = useSensors(
        useSensor(PointerSensor),
        useSensor(KeyboardSensor, {
            coordinateGetter: sortableKeyboardCoordinates
        })
    );

    function handleDragEnd(event) {
        setDisableDragging(true);
        const beingMoved = event.active;
        const to = event.over;
        let newQueue = [];

        if (beingMoved.id !== to.id) {
            const queueItemsIds = items.map(queueItem => queueItem.Id);
            const oldIndex = queueItemsIds.indexOf(beingMoved.id);
            const newIndex = queueItemsIds.indexOf(to.id);

            newQueue = arrayMove(items, oldIndex, newIndex);
            for (var i = 0; i < newQueue.length; i++) {
                newQueue[i].Order = i;
            }
            setItems(newQueue);
        }

        ChangeQueueOrder(roomId, newQueue.map((item) => {return {Id : item.Id, Order : item.Order} as QueueOrder}))
            .catch((error) => {
                console.log(error); // todo needs to be properly handled
        }).finally(() => setDisableDragging(false));
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

    // Update internal state when props change
    useEffect(() => {
        if (queueItems) {
            setItems(queueItems);
        }
    }, [queueItems]);


    useEffect(() => {
        hub.on("QueueAdded", (queueItem) => {
            const item : IQueue = {
                Title : queueItem.title,
                ThumbnailLocation : queueItem.thumbnailLocation,
                Order : queueItem.order,
                Id : queueItem.id,
                Type : queueItem.type
            };
            setItems([...items, item]);
        });

        hub.on("DeleteQueue", (id : string) => {
            const newQueue = items.filter(item => item.Id != id);
            setItems(newQueue);
        });

        hub.on("QueueOrderChanged", (queue : {id : string, order : number}[]) => {
            let newQueue : IQueue[] = [];
            for (let x = 0; x < queue.length; x++) {
                let newItem = items.find(item => item.Id == queue[x].id);
                if (!newItem) console.log("Error: cannot change queue order");
                newItem.Order = queue[x].order;
                newQueue.push(newItem);
            }
            setItems(newQueue);
        })
    }, [hub, items])

    return (
        <div>
        <QueueAdd />
        <DndContext
            sensors={sensors}
            collisionDetection={closestCenter}
            onDragEnd={handleDragEnd}>
            <div className={`grid grid-cols-1 gap-4 ${disableDragging ? "pointer-events-none" : ""}`}>
                <SortableContext items={items} strategy={verticalListSortingStrategy}>
                    {items.sort(sortQueueItems).map(item => <QueueItem key={item.Id} queueItem={item} />)}
                </SortableContext>
            </div>
        </DndContext>
        </div>
    )
}