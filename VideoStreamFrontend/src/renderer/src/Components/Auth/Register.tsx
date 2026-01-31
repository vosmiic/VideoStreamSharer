import {useState} from "react";
import {Button} from "@headlessui/react";

export default function Register() {
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

    function handleRegister() {
        fetch("https://localhost:7074/register", {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify(registerBody)
        }).then(result => {
            console.log(result);
        }).catch(error => {
            console.log(error);
        })
    }

    return <>
        <p>Username</p><b/>
        <input type="text" value={registerBody.email} onChange={handleUsernameChange} /><b/>
        <p>Password</p><b/>
        <input type="password" value={registerBody.password} onChange={handlePasswordChange} /><b/>
        <Button onClick={() => handleRegister()}>Register</Button>
    </>
}

export class RegisterBody {
    email : string | undefined;
    password : string | undefined;
}