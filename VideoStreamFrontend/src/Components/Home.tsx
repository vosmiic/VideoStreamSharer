import {useState} from "react";
import {Button, Input} from "@headlessui/react";

export default function Home() {
    const [roomName, setRoomName] = useState('');
    
    function handleSubmit() : void {
        fetch("https://localhost:7074/room", {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: '"' + roomName + '"',
            credentials: "include"
        }).then(
            (response) => {console.log(response)}
        )
    }
    
    return (
        <>
            <h1>Home</h1>
            <Input value={roomName} onChange={(e) => setRoomName(e.target.value)} type="text"/>
            <Button onClick={handleSubmit}>Submit</Button>
        </>
    )
}