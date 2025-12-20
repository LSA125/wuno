# Wuno

🎮 **Play Now:** [https://w-uno.xyz](https://w-uno.xyz)

> ⚠️ **Note:** The app is hosted on Azure free tier. Initial load may take 30-60 seconds while the services wake up from cold start.

Wuno is a fast, real-time party word game you can spin up and play with friends.  
The back end is built with ASP.NET Core, SignalR, and EF Core.  
The front end is a React SPA developed using Vite and Tailwind CSS.  

The app focuses on quick joins (guest mode), deterministic gameplay logic, robust real-time infrastructure, and clean, production-style engineering practices.

---

## Table of Contents

- [Features](#features)  
- [Screenshots](#screenshots)  
- [Architecture](#architecture)  
- [Tech Stack](#tech-stack)  
- [Local Setup](#local-setup)  
- [How to Play](#how-to-play)  
- [Notable Code](#notable-code)  
- [Security and Reliability](#security-and-reliability)  
- [Roadmap](#roadmap)  
- [License](#license)  
- [Author](#author)  

---

## Features

- **Play instantly** as a Guest or register for an account  
- **Quick matchmaking** - find a game automatically or create private rooms with codes  
- **Real-time gameplay** powered by SignalR with synchronized turns, timers, and state updates  
- **Mobile-friendly** - responsive design with touch-optimized controls  
- **Word chain scoring** - match letters from previous words for bonus points and time  
- **Lobby system** with ready states; the match begins when all players are ready  
- **Auto-rejoin** support to recover from disconnections  
- **Server-side turn timers** that advance the game even if players disconnect  
- **Crash recovery** if the server crashes, the game will resume seamlessly.
- **Rate limiting**, CORS policies, and secure cookie authentication

---

## Screenshots

Screenshots are available under the `/screenshots` directory:

![landing](https://github.com/LSA125/wuno/blob/master/screenshots/CreateModal.png)

Each image shows different parts of the game flow, from landing to gameplay setup.

---

## Architecture

### Solution Layout

```
wuno/
├─ wuno.api            # ASP.NET Core API + SignalR Hub + Middleware
├─ wuno.application    # Application layer (services, orchestrations)
├─ wuno.domain         # Entities, enums, and rules (pure domain)
├─ wuno.infrastructure # EF Core DbContext, migrations, background jobs
├─ Wuno.Client         # React/Vite SPA (TypeScript + Tailwind + Bootstrap)
└─ *.Tests             # Unit test projects
```

### Core Flows

- **SignalR Hub:** The `GameHub` manages real-time communication and broadcasts updates to player groups.  
- **Server Timers:** Background turn timers handle automatic timeouts and transitions.  
- **EF Core Transactions:** Used for atomic game state updates and concurrency safety.  
- **Authentication:** Cookie-based for registered users; guest cookies for anonymous play.  
- **CORS:** Restricts access to trusted SPA origins.  

---

## Tech Stack

**Backend:** ASP.NET Core 8, SignalR, EF Core (SQL Server), ASP.NET Rate Limiting, Data Protection, Swagger  
**Frontend:** React 18, Vite, TypeScript, Tailwind CSS, Bootstrap  
**Production:** Azure App Service, Azure SQL Database, Azure SignalR Service, Azure Static Web Apps

---

## Local Setup

### Prerequisites

- .NET 8 SDK  
- Node.js 20+  
- SQL Server (LocalDB or container)  

### Steps

1. Clone the repository  
   ```
   git clone https://github.com/LSA125/wuno.git
   cd wuno
   ```

2. Configure your connection string in `Wuno.Api/appsettings.Development.json`:  
   ```
   {
     "ConnectionStrings": {
       "Default": "Server=(localdb)\\MSSQLLocalDB;Database=Wuno;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
     }
   }
   ```

3. Build, migrate, and start the backend:  
   ```
   dotnet build
   dotnet ef database update --project Wuno.Infrastructure --startup-project Wuno.Api
   dotnet run --project Wuno.Api
   ```

4. Start the frontend:  
   ```
   cd wuno.client
   npm install
   npm run dev
   ```

The frontend runs at `https://localhost:5173` and proxies requests to the backend.

---

## How to Play

1. Open the SPA and choose **Play as Guest**, **Sign In**, or **Create Account**.  
2. Create a game or join using a shared game code.  
3. In the lobby, toggle **Ready** to indicate you are prepared.  
4. When all players are ready, the match begins automatically.  
5. Type a valid word during your turn that meets the given constraints.  
6. Special effects modify rules and timers for the next player.  
7. The last active player wins the round; the first to reach the target number of wins takes the match.  

---

## Notable Code

- `wuno.api/Hubs/GameHub.cs` – SignalR orchestration and real-time messaging  
- `wuno.application/Games/Implementation/GameService.cs` – Core game logic and turn processing  
- `wuno.domain/Rules/Effects.cs` – Deterministic scoring and turn time calculations  
- `wuno.infrastructure/AppDbContext.cs` – EF Core context with Data Protection key storage  
- `Wuno.Client/src/components/game/LiveGame.tsx` – Game UI with mobile input support

---

## Security and Reliability

- Secure cookie authentication with HttpOnly and SameSite flags.  
- CORS limited to trusted front-end origins.  
- Rate limiting for critical API endpoints.  
- Persistent data protection keys for cookie encryption.  
- Defensive SignalR event handling with connection validation.  

---

## Roadmap

**Completed:**
- ✅ Cloud deployment (Azure App Service + Azure SQL + Azure SignalR)
- ✅ Quick matchmaking system
- ✅ Mobile-responsive gameplay

**Planned:**
- Spectator and replay features
- Friend lists and invites
- AI/bot opponents for single player practice
- Leaderboards and statistics tracking

---

## License

MIT License  

---

## Author

Developed by **LSA125**  
Focused on scalable real-time web systems, .NET backend architecture, and interactive front-end experiences.  
