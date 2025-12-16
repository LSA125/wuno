import { createBrowserRouter, Navigate } from "react-router-dom";
import App from "./App";
import LandingPage from "./pages/LandingPage";
import LobbyPage from "./pages/LobbyPage";
import GamePage from "./pages/GamePage";
import StatsPage from "./pages/StatsPage";

const router = createBrowserRouter([
    {
        path: "/",
        element: <App />,
        children: [
            // Landing page (default)
            { index: true, element: <LandingPage /> },

            // Lobby page (user enters after creating / loading profile)
            { path: "lobby", element: <LobbyPage /> },

            // Stats page
            { path: "stats", element: <StatsPage /> },

            // Future pages
            { path: "game/:code", element: <GamePage /> },

            // Catch-all for unknown URLs
            { path: "*", element: <Navigate to="/" replace /> },
        ],
    },
]);

export default router;
