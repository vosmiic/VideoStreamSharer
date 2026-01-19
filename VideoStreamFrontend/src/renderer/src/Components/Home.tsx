import {useEffect, useState} from "react";
import {Button, Input} from "@headlessui/react";
import * as constants from "../Constants/constants.tsx";
import {useNavigate} from "react-router-dom";
import {IHome} from "../Interfaces/IHome.tsx";
import {GetHomeInfo} from "../Helpers/ApiCalls.tsx";

export default function Home() {
    const navigation = useNavigate();
    const [roomName, setRoomName] = useState('');
    const [joinRoomId, setJoinRoomId] = useState('');
    const [home, setHome] = useState<IHome | null>(null);

    useEffect(() => {
        GetHomeInfo()
            .then((result) => {
                if (result.ok) {
                    result.json().then((homeInfo : IHome) => {
                        setHome(homeInfo);
                    });
                }
        });
    }, [])

    function handleSubmit(): void {
        if (home?.RoomNames.find(name => name.Name == roomName)) {
            alert("Room with that name already exists");
            return;
        }
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
                        const roomId: string = result.replaceAll('"', '');
                        navigation(`./room/${roomId}`);
                    })
                } else {
                    // todo display error
                    console.error("Error creating room", response);
                }
            }
        )
    }

    function handleGo() {
        navigation(`./room/${joinRoomId}`);
    }

    return (
        <div>
            <h1>Home</h1>
            <div className={"flex"}>
                <div className={"flex-auto w-1/2 bg-purple-700"}>
                    <p>Create new room:</p>
                    <Input value={roomName} onChange={(e) => setRoomName(e.target.value)} type="text"/>
                    <Button onClick={handleSubmit}>Submit</Button>
                </div>
                <div className={"flex-auto w-1/2 bg-blue-800"}>
                    <p>Join room:</p>
                    <input type={"text"} placeholder={"Enter Room Name"} list={"rooms"} onChange={(e) => setJoinRoomId(e.target.value)}/>
                    <datalist id={"rooms"}>
                        {home != null ? home?.RoomNames.map((roomName) => <option value={roomName.Id} label={roomName.Name} key={roomName.Id}></option> ) : <></>}
                    </datalist>
                    <Button onClick={handleGo}>Go</Button>
                </div>
            </div>
        </div>
    )
}
