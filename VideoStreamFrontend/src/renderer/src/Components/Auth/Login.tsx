import {useState} from "react";
import {RegisterBody} from "./Register.tsx";
import {Button} from "@headlessui/react";
import * as constants from "../../Constants/constants.tsx";

export default function Login() {
    const [registerBody, setRegisterBody] = useState(new RegisterBody());

    function handleUsernameChange(e) {
        const existingRegisterBody = registerBody;
        existingRegisterBody.email = e.target.value;
        setRegisterBody(existingRegisterBody);
    }

    function handlePasswordChange(e) {
        const existingRegisterBody = registerBody;
        existingRegisterBody.password = e.target.value;
        setRegisterBody(existingRegisterBody);
    }

    function handleLogin() {
        fetch(`${constants.API_URL}/login?useCookies=true`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify(registerBody),
            credentials: "include"
        }).then(result => {
            const returnPath = new URL(window.location.href).searchParams.get('return')
            if (returnPath != null) {
                window.location.href = returnPath;
            } else {
                window.location.href = window.location.origin;
            }
            console.log(result);
        }).catch(error => {
            console.log(error);
        })
    }

    return <>
        <p>Username</p><b/>
        <input type="text" value={registerBody.email} onChange={handleUsernameChange}/><b/>
        <p>Password</p><b/>
        <input type="password" value={registerBody.password} onChange={handlePasswordChange}/><b/>
        <Button onClick={() => handleLogin()}>Login</Button>
    </>
}