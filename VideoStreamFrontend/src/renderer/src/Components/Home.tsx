import {useState} from "react";
import {Button, Input} from "@headlessui/react";
import * as constants from "../Constants/constants.tsx";
import {useNavigate} from "react-router-dom";

export default function Home() {
    const navigation = useNavigate();
    const [roomName, setRoomName] = useState('');

    function handleSubmit() : void {
        fetch(`${constants.API_URL}/room`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: '"' + roomName + '"',
            credentials: "include"
        }).then(
            (response) => {
                if (response.ok) {
                    response.text().then((result) => {
                        const roomId : string = result.replaceAll('"', '');
                        navigation(`./room/${roomId}`);
                    })
                } else {
                    // todo display error
                    console.error("Error creating room", response);
                }
            }
        )
    }

    return (
        <>
            <h1>Home</h1>
            <Input value={roomName} onChange={(e) => setRoomName(e.target.value)} type="text"/>
            <Button onClick={handleSubmit}>Submit</Button>
            <a href={"/room/4a64f807-fda8-4121-bc25-de5f8072e2a5"}>CLICK</a>
        </>
    )
}
