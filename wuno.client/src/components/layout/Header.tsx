import { Link, useLocation } from "react-router-dom";

export default function Header() {
    const { pathname } = useLocation();
    return (
        <header className="w-full border-b bg-white/70 backdrop-blur sticky top-0 z-20">
            <div className="container mx-auto px-4 py-3 flex items-center justify-between">
                <Link to="/lobby" className="navbar-brand text-2xl">Wuno</Link>
                <nav className="flex items-center gap-4">
                    <Link
                        to="/lobby"
                        className={`nav-link ${pathname.startsWith("/lobby") ? "active" : ""}`}
                    >
                        Lobby
                    </Link>
                    {/* future */}
                    {/* <Link to="/game/123" className="nav-link">Game</Link> */}
                    {/* <Link to="/stat/me" className="nav-link">Stats</Link> */}
                </nav>
            </div>
        </header>
    );
}
