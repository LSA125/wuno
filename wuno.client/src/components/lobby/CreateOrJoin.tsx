import CreateGameModal from "./CreateGameModal";
import JoinGameCard from "./JoinGameCard";
import ProfileCard from "./ProfileCard";
import StatsButton from "./StatsButton";
import { useState } from "react";

export default function CreateOrJoin() {
    const [showCreate, setShowCreate] = useState(false);

    return (
        <div className="grid gap-6 md:grid-cols-3 lg:grid-cols-4">
            <div className="md:col-span-2 lg:col-span-3">
                <div className="card shadow-xl">
                    <div className="card-body">
                        <h3 className="card-title text-3xl mb-2">Lobby</h3>
                        <p className="opacity-70 mb-4">
                            Create a new game or join an existing one. You can manage your temporary
                            or registered account at any time.
                        </p>
                        <div className="flex flex-wrap gap-3">
                            <button className="btn btn-primary" onClick={() => setShowCreate(true)}>
                                Create Game
                            </button>
                            <StatsButton />
                        </div>
                    </div>
                </div>

                <div className="mt-6">
                    <JoinGameCard />
                </div>
            </div>

            <aside>
                <ProfileCard />
            </aside>

            <CreateGameModal open={showCreate} onClose={() => setShowCreate(false)} />
        </div>
    );
}
