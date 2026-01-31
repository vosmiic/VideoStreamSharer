import Home from "./Components/Home";
import Register from "./Components/Auth/Register";
import Login from "./Components/Auth/Login";
import Room from "./Components/Room";
import Index from "./Components/Stream/Index";

const AppRoutes = [
    {
        index: true,
        element: <Home />,
        requireAuth: false
    },
    {
        element: <Register />,
        path: "/register",
    },
    {
        element: <Login />,
        path: "/login",
    },
    {
        element: <Room />,
        path: "/room/:roomId",
    },
    {
        element: <Index />,
        path: "/stream/:userId"
    }
];

export default AppRoutes;