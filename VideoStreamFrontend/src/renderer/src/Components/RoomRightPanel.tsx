import {useRef} from "react";
import Users from "./Users.tsx";
import Chat from "./Chat.tsx";

export default function RoomRightPanel(prop : { users: string[] }) {
    const selectedItem = useRef<string>("chat");

    function onClick(event: React.MouseEvent<HTMLAnchorElement>) {
        const elementId = event.currentTarget.id;
        selectedItem.current = elementId;
        if (elementId == "chat") {
            document.getElementById("users")?.classList.remove("menu-active");
            document.getElementById(elementId)?.classList.add("menu-active");
        } else {
            document.getElementById("chat")?.classList.remove("menu-active");
            document.getElementById(elementId)?.classList.add("menu-active");
        }
    }

    return <div className={"h-full w-full"}>
        <div>
            <ul className="menu menu-vertical lg:menu-horizontal bg-base-200 rounded-box">
                <li><a onClick={onClick} id={"chat"}>Chat</a></li>
                <li><a onClick={onClick} id={"users"}>Users</a></li>
            </ul>
        </div>
        <div className={"flex h-full"}>
            {selectedItem.current === "chat" ? <Chat /> : <Users users={prop.users} />}
        </div>
    </div>
}