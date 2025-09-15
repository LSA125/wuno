import { useEffect, useState } from 'react';
import './App.css';

function App() {
    [data, setData] = useState(null);

    function createGame() {
        fetch('api/hotseat/new').then(res => setData(res));
    }

    return (
        <>
            <div className="card">
                <h1>WUNO Client</h1>
                <button onClick="createGame()">Create Game</button>
                <button>Join Game</button>
                <input type="text" placeholder="word" />
            </div>
            <div className="card">
                <h1>Res</h1>
            </div>
        </>
    )
}

export default App;