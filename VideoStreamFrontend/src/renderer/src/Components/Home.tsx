import {useEffect, useState} from "react";
import {Button, Input} from "@headlessui/react";
import * as constants from "../Constants/constants.tsx";
import {useNavigate} from "react-router-dom";
import {IHome, IRoomName} from "../Interfaces/IHome.tsx";
import {GetHomeInfo} from "../Helpers/ApiCalls.tsx";
import {RecentRoomsCookieName} from "../Constants/constants.tsx";

export default function Home() {
    const navigation = useNavigate();
    const [roomName, setRoomName] = useState('');
    const [joinRoomId, setJoinRoomId] = useState('');
    const [home, setHome] = useState<IHome | null>(null);
    const [recentRooms, setRecentRooms] = useState<Array<IRoomName> | null>(null);

    useEffect(() => {
        let getRecentRoomsFromServer = false;
        cookieStore.get(RecentRoomsCookieName).then((recentRoomsCookieItem) => {
            if (recentRoomsCookieItem == null) {
                getRecentRoomsFromServer = true;
            } else {
                if (recentRoomsCookieItem.value) {
                    setRecentRooms(JSON.parse(recentRoomsCookieItem.value) as Array<IRoomName>);
                }
            }
        })

        GetHomeInfo(getRecentRoomsFromServer)
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
                <div className={"flex-auto w-1/2"}>
                    <div className={"bg-blue-800"}>
                        <p>Join room:</p>
                        <input type={"text"} placeholder={"Enter Room Name"} list={"rooms"} onChange={(e) => setJoinRoomId(e.target.value)}/>
                        <datalist id={"rooms"}>
                            {home != null ? home?.RoomNames.map((roomName) => <option value={roomName.Id} label={roomName.Name} key={roomName.Id}></option> ) : <></>}
                        </datalist>
                        <Button onClick={handleGo}>Go</Button>
                    </div>
                    <div className={"bg-lime-600"}>
                        Recent rooms:
                        {recentRooms ? recentRooms.sort((a, b) => a.VisitDateTime.getTime() - b.VisitDateTime.getTime()).map((roomName) => <div key={roomName.Id}>
                            <a onClick={() => navigation(`./room/${roomName.Id}`)}>{roomName.Name}</a>
                        </div>) : <></>}
                    </div>
                </div>
            </div>
        </div>
    )
}
