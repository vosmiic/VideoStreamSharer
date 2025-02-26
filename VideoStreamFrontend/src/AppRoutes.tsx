import Home from "./Components/Home.tsx";
import Register from "./Components/Auth/Register.tsx";
import Login from "./Components/Auth/Login.tsx";
import Room from "./Components/Room.tsx";

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
    }
];

export default AppRoutes;