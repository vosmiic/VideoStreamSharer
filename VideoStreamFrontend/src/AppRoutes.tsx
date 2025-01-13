import Home from "./Components/Home.tsx";
import Register from "./Components/Auth/Register.tsx";
import Login from "./Components/Auth/Login.tsx";

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
    }
];

export default AppRoutes;