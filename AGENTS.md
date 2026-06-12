# Dtudo2026CSharp Workspace Guide

Treat this repository as one full-stack workspace, not as separate apps.

## Workspace boundary

- Repository root is the source of truth.
- Visual Studio may open only `ApiCSharp.slnx`, but `DtudoSite/`, `ApiNode/`, and `ApiCSharp/` are all part of the same product.
- Backend targets .NET 10.

## Main areas

### Frontend

- Path: `DtudoSite/`
- Stack: React 19 + Vite
- Entry: `DtudoSite/src/main.jsx`
- Router: `DtudoSite/src/router/DtudoRouter.jsx`
- Shared state: `DtudoSite/src/context_api/`
- Feature areas: `MyAnimes`, `Animes`, `Animex`, `MyMusicX`, `NinoTI`, `auth`

### Node utilities

- Path: `ApiNode/`
- Local JSON server data: `ApiNode/db/animacoes.json`
- Music proxy: `ApiNode/mymusicx/discogsProxy.js`
- Helper scripts are part of the development workflow and may support frontend features directly.

### C# backend

- Path: `ApiCSharp/`
- Startup: `ApiCSharp/Program.cs`
- Controllers: `ApiCSharp/Controllers/`
- Services: `ApiCSharp/Services/`
- Models: `ApiCSharp/Models/`
- Anime API integration: `AnimeController`, `IJikanService`, `JikanService`

## Working rules

- Prefer existing components, contexts, providers, services, and routes.
- Do not assume a request belongs to only one stack.
- If a route or page changes, inspect frontend router and owning providers.
- If an endpoint or payload changes, inspect backend controller/service/model and frontend consumers.
- If data or proxy behavior changes, inspect `ApiNode/` before assuming the C# API owns it.
- Keep changes minimal and local unless the feature clearly spans layers.

## Useful commands

- `npm run serv`
- `npm run dev`
- `npm run start`
- `npm run proxy`
- `npm run build`
- `dotnet build ApiCSharp/ApiCSharp.csproj`

## Persistence

This file and `.github/copilot-instructions.md` are intended to preserve repository context across reopened sessions in this folder.
