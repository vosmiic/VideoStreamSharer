import {Button} from "@headlessui/react";
import {hasAuthParams, useAuth} from "react-oidc-context";
import React from "react";

export default function Layout({ children } : { children: any }) {
    const auth = useAuth();

    function Login() {
        if (auth.isAuthenticated) {
            return <div>
                Welcome {auth.user?.profile.sub}
                <Button onClick={() => auth.signoutRedirect()}>
                    Logout
                </Button>
            </div>
        } else {
            return <div>
                <Button onClick={() => void auth.signinRedirect()}>Login</Button>
            </div>
        }
    }
    
    return (
        <>
            <div className="max-w-screen-xl flex flex-wrap mx-auto items-center justify-between p-2">
                <a href="/" className="flex items-center">Home</a>
                <ul className="flex flex-col md:p-0">
                    <li>
                        {Login()}
                    </li>
                </ul>
            </div>
            <div>
                {children}
            </div>
        </>
    )
}