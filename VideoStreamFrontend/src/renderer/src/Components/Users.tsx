import {useContext, useEffect, useState} from "react";
import {HubContext} from "../Contexts/HubContext.tsx";

export default function Users(prop : { users: string[] }) {
    const hub = useContext(HubContext);
    const [users, setUsers] = useState<string[]>(prop.users);

    useEffect(() => {
        hub.on("AddUser", (username : string) => {
            setUsers([...users, username]);
        });

        hub.on("RemoveUser", (username : string) => {
            setUsers(users.filter(user => user !== username));
        });
    }, [hub, users]);

    return <>
        {users.map(name => (
            <p key={name}>{name}</p>
        ))}
    </>
}