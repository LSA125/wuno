import React from "react";
import ReactDOM from "react-dom/client";
import { RouterProvider } from "react-router-dom";
import router from "./router";
import "@/styles/index.css";
import { UserProvider } from "@/context/UserContext";
import { ToastProvider } from "@/context/ToastContext";

ReactDOM.createRoot(document.getElementById("root")!).render(
    <React.StrictMode>
        <UserProvider>
            <ToastProvider>
                <RouterProvider router={router} />
            </ToastProvider>
        </UserProvider>
    </React.StrictMode>
);
