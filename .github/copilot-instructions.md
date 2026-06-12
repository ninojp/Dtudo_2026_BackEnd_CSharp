# Copilot Instructions

## Repository Guidance

This repository is a single product workspace named `Dtudo2026CSharp`, treating it as a full-stack workspace covering React (`DtudoSite`), Node (`ApiNode`), and ASP.NET Core (`ApiCSharp`). Always preserve this context in the repository instructions whenever possible.

Always treat these sibling folders as first-class parts of the same solution context:

- `DtudoSite/`: React + Vite frontend.
- `ApiNode/`: Node scripts, local JSON server data, and proxy/utilities.
- `ApiCSharp/`: ASP.NET Core backend API targeting .NET 10.

Do not treat `ApiCSharp/` as an isolated application just because it is the only project in the Visual Studio solution. If a request mentions a feature, route, page, endpoint, data source, or integration, consider whether the frontend, Node utilities, and C# backend all participate in that behavior.

## Workspace identity and architecture

- Repository root is the real workspace boundary.
- Visual Studio solution file: `ApiCSharp.slnx`
- Root workspace scripts: `package.json`
- Frontend bootstrap: `DtudoSite/src/main.jsx`
- Frontend router: `DtudoSite/src/router/DtudoRouter.jsx`
- Frontend global styles: `DtudoSite/src/main.css`
- C# startup: `ApiCSharp/Program.cs`

## Stack ownership

### Frontend (`DtudoSite/`)

- Uses React 19 + Vite.
- Main app composition is in `DtudoSite/src/main.jsx`.
- Routing is centralized in `DtudoSite/src/router/DtudoRouter.jsx`.
- Shared state is heavily provider-based under `DtudoSite/src/context_api/`.
- Main visible feature areas include `MyAnimes`, `Animes`, `Animex`, `MyMusicX`, `NinoTI`, and `auth` pages.

### Node utilities (`ApiNode/`)

- Holds local data, scripts, and supporting services.
- JSON server data source is `ApiNode/db/animacoes.json` and runs on port `3666` through `npm run start`.
- Music proxy entry point is `ApiNode/mymusicx/discogsProxy.js` through `npm run proxy`.
- Repository-level scripts such as `populateAnimeDetails.js` and `DiscografiaPastas.js` are part of the development workflow.

### C# backend (`ApiCSharp/`)

- Uses ASP.NET Core minimal hosting in `ApiCSharp/Program.cs`.
- Controllers live in `ApiCSharp/Controllers/`.
- Services live in `ApiCSharp/Services/`.
- Models live in `ApiCSharp/Models/`.
- Current anime integration is centered on `AnimeController`, `IJikanService`, and `JikanService`, which call the external Jikan API.
- Static file hosting and fallback to `wwwroot/index.html` are enabled in the backend startup.

## Domain map

- `MyAnimes`: frontend pages and providers for anime browsing and details.
- `Animes`: broader anime listing area in the frontend.
- `Animex`: protected frontend area with detail routes and providers.
- `MyMusicX`: music feature area that may depend on Node proxy behavior.
- `NinoTI`: educational/content section with nested routes.
- `auth`: login, register, and logout routes in the frontend.

## Working conventions

- Prefer reusing existing components, contexts, hooks, services, and routes before creating new ones.
- Keep file and folder names consistent with the current project structure.
- Preserve the current architecture unless the user explicitly asks for a refactor.
- If a request touches route behavior, inspect the router and the owning page/provider before editing.
- If a request touches API behavior, inspect controller, service, model, and frontend consumer contracts.
- If a request touches local data, mock data, or third-party proxy behavior, inspect `ApiNode/` before assuming the backend owns it.
- When changing API contracts, consider whether React pages, providers, or Node scripts also need coordinated updates.

## Useful commands

- Frontend and local services: `npm run serv`
- Frontend only: `npm run dev`
- Node JSON server: `npm run start`
- Node proxy: `npm run proxy`
- Workspace build: `npm run build`
- C# build: `dotnet build ApiCSharp/ApiCSharp.csproj`

## Scope reminder

If a task touches domain data already present in the repository, inspect the existing feature folders first:

- `DtudoSite/src/components/`
- `DtudoSite/src/pages/`
- `DtudoSite/src/context_api/`
- `ApiNode/`
- `ApiCSharp/Controllers/`
- `ApiCSharp/Services/`
- `ApiCSharp/Models/`

Keep changes minimal and local to the owning layer unless the requested behavior clearly spans the full stack.

## Persistence expectation

These repository instructions are intended to persist workspace context across Visual Studio restarts and future sessions opened from this same folder. Always use this repository-level context first when working inside `Dtudo2026CSharp`.

## Visual Studio Visibility

- Ensure that `ApiCSharp`, `DtudoSite`, and `ApiNode` are visible in Visual Studio without moving or reorganizing folders in the repository.
