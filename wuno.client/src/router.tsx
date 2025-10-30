import { createBrowserRouter, Navigate } from "react-router-dom";
import App from "./App";
import LobbyPage from "./pages/LobbyPage";

const router = createBrowserRouter([
    { path: "/", element: <Navigate to="/lobby" replace /> },
    {
        path: "/",
        element: <App />,
        children: [
            { path: "lobby", element: <LobbyPage /> },
            // ready for expansion:
            // { path: "game/:id", element: <GamePage /> },
            // { path: "stat/:user", element: <UserStatsPage /> },
        ],
    },
]);

export default router;
