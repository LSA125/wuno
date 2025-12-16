import { Link } from "react-router-dom";

export default function StatsButton() {
    return (
        <Link to="/stats" className="btn btn-outline-secondary">
            View Stats
        </Link>
    );
}
