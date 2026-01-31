import {useEffect, useState} from "react";
import {GetLoginInfo, Logout} from "./src/Helpers/ApiCalls";
import {ILoggedInUser} from "./src/Interfaces/ILoggedInUser";

export default function Layout({ children } : { children }) {
    const [username, setUsername] = useState<string | null>(null);

    useEffect(() => {
        GetLoginInfo()
            .catch(() => {
                setUsername(null);
            })
            .then((result) => {
                if (result && result.ok) {
                    result.json()
                        .then((json : ILoggedInUser | undefined) => {
                            if (json) {
                                setUsername(json.email);
                            }
                        })
                        .catch(() => {
                            setUsername(null);
                        })
                }
            });
    }, [])

    function handleLogin() {
        window.location.href = "/login?return=" + window.location.pathname;
    }

    function handleLogout() {
        Logout()
            .then((result) => {
                if (result.ok) {
                    setUsername(null);
                }
        });
    }

    return (
        <>
            <div className="max-w-screen-xl flex flex-wrap mx-auto items-center justify-between p-2">
                <a href="/" className="flex items-center">Home</a>
                <div className="flex flex-row md:p-0">
                    {username != null ?
                        <>
                            <p>Welcome {username}</p>
                            <button onClick={handleLogout}>Logout</button>
                        </>
                    :
                        <button onClick={handleLogin}>Login</button>
                    }
                </div>
            </div>
            <div>
                {children}
            </div>
        </>
    )
}