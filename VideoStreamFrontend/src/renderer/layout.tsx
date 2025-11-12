import React from "react";

export default function Layout({ children } : { children }) {
    return (
        <>
            <div className="max-w-screen-xl flex flex-wrap mx-auto items-center justify-between p-2">
                <a href="/" className="flex items-center">Home</a>
                <ul className="flex flex-col md:p-0">
                    <li>
                        Login
                    </li>
                </ul>
            </div>
            <div>
                {children}
            </div>
        </>
    )
}