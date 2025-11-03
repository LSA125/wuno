# Wuno

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

- Play instantly as a Guest or register for an account.  
- Create and join games via short room codes.  
- Lobby system with ready states; the match begins when all players are ready.  
- Real-time gameplay powered by SignalR with synchronized turns, timers, and state updates.  
- Deterministic word and effects logic for fairness and consistency.  
- Auto-rejoin support to recover from disconnections.  
- Server-side timeouts and turn advancement.  
- Rate limiting, CORS policies, and secure cookie authentication.  

---

## Screenshots

Screenshots are available under the `/screenshots` directory:

- Landing (`Landing.png`)  
- Create or Join (`CreateModal.png`)  
- Lobby (`lobby.png`)  
- Waiting Room (`WaitingRoom.png`)  

Each image shows different parts of the game flow, from landing to gameplay setup.

---

## Architecture

### Solution Layout

```
wuno/
├─ Wuno.Api            # ASP.NET Core API + SignalR Hub + Middleware
├─ Wuno.Application    # Application layer (services, orchestrations)
├─ Wuno.Domain         # Entities, enums, and rules (pure domain)
├─ Wuno.Infrastructure # EF Core DbContext, migrations, timers, adapters
└─ wuno.client         # React/Vite SPA (TypeScript + Tailwind)
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
**Frontend:** React 18, Vite, TypeScript, Tailwind CSS  
**Infrastructure:** Local SQL Server or containerized database, HTTPS via .NET developer certificates  

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

- `Wuno.Api/Hubs/GameHub.cs` – SignalR orchestration and real-time messaging.  
- `Wuno.Application/Games/GameService.cs` – Core transactional game logic and deterministic rules.  
- `Wuno.Infrastructure/AppDbContext.cs` – EF Core context and persistence layer.  
- `wuno.client/src/pages/GamePage.tsx` – Game UI and SignalR integration on the client side.  

---

## Security and Reliability

- Secure cookie authentication with HttpOnly and SameSite flags.  
- CORS limited to trusted front-end origins.  
- Rate limiting for critical API endpoints.  
- Persistent data protection keys for cookie encryption.  
- Defensive SignalR event handling with connection validation.  

---

## Roadmap

- Add spectator and replay features.  
- Cloud deployment (Azure App Service + SQL).  
- Unit and integration testing for rules and effects.  
- AI/bot/single player support.
- Accessibility and mobile layout enhancements.  

---

## License

MIT License  

---

## Author

Developed by **LSA125**  
Focused on scalable real-time web systems, .NET backend architecture, and interactive front-end experiences.  
