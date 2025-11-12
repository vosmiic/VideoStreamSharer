import {BrowserRouter, Route, Routes} from "react-router-dom";
import AppRoutes from "./AppRoutes.tsx";

export default function Router() {
    return (
        <BrowserRouter>
            <Routes>
                {AppRoutes.map((route, index) => {
                    const { element, requireAuth, ...rest } = route;
                    return <Route key={index} {...rest} element={element} />;
                })}
            </Routes>
        </BrowserRouter>
    )
}