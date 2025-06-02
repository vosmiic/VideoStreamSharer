import {useContext, useState} from "react";
import {Button, Input} from "@headlessui/react";
import {AddToQueue} from "../../Helpers/ApiCalls.tsx";
import QueueAddBody from "../../Models/QueueAdd.tsx";
import {RoomContext} from "../../Contexts/RoomContext.tsx";
import {HubContext} from "../../Contexts/HubContext.tsx";

export default function QueueAdd() {
    const roomId : string = useContext(RoomContext);
    const [open, setOpen] = useState(false);
    const [input, setInput] = useState("");

    function handleOpen() {
        setOpen(!open);
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

    return (<div>
        {open ? (
            <div className={"flex flex-nowrap w-full"}>
                <Input type={"text"} onChange={(e) => setInput(e.target.value)} />
                <Button onClick={handleOnSubmit}>+</Button>
            </div>
        ) : (
            <></>
        )}
        <Button onClick={handleOpen}>{open ? "^" : "+"}</Button>
    </div>)
}