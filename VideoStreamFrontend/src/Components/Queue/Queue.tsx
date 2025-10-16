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

export default function Queue(params: {queueItems : IQueue[], setQueueItems: (queue: IQueue[]) => void}) {
    const roomId = useContext(RoomContext);
    const hub = useContext(HubContext);
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
            const queueItemsIds = params.queueItems.map(queueItem => queueItem.Id);
            const oldIndex = queueItemsIds.indexOf(beingMoved.id);
            const newIndex = queueItemsIds.indexOf(to.id);

            newQueue = arrayMove(params.queueItems, oldIndex, newIndex);
            for (var i = 0; i < newQueue.length; i++) {
                newQueue[i].Order = i;
            }
            params.setQueueItems(newQueue);
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


    useEffect(() => {
        hub.on("QueueAdded", (queueItem : IQueue) => {
            params.setQueueItems([...params.queueItems, queueItem]);
        });

        hub.on("DeleteQueue", (id : string) => {
            const newQueue = params.queueItems.filter(item => item.Id != id);
            params.setQueueItems(newQueue);
        });

        hub.on("QueueOrderChanged", (Queue : {Id : string, Order : number}[]) => {
            let newQueue : IQueue[] = [];
            for (let x = 0; x < Queue.length; x++) {
                let newItem = params.queueItems.find(item => item.Id == Queue[x].Id);
                if (!newItem) console.log("Error: cannot change queue order");
                newItem.Order = Queue[x].Order;
                newQueue.push(newItem);
            }
            params.setQueueItems(newQueue);
        })
    }, [hub, params.queueItems])

    return (
        <div>
        <QueueAdd />
        <DndContext
            sensors={sensors}
            collisionDetection={closestCenter}
            onDragEnd={handleDragEnd}>
            <div className={`grid grid-cols-1 gap-4 ${disableDragging ? "pointer-events-none" : ""}`}>
                <SortableContext items={params.queueItems} strategy={verticalListSortingStrategy}>
                    {params.queueItems.sort(sortQueueItems).map(item => <QueueItem key={item.Id} queueItem={item} />)}
                </SortableContext>
            </div>
        </DndContext>
        </div>
    )
}