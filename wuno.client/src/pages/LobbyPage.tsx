import UserGate from "@/components/Lobby/UserGate";
import CreateOrJoin from "@/components/Lobby/CreateOrJoin";

export default function LobbyPage() {
    return (
        <div className="container mx-auto px-4">
            <UserGate />
            <section className="py-10">
                <CreateOrJoin />
            </section>
        </div>
    );
}
