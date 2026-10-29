# Dtudo2026CSharp Workspace Guide

Treat this repository as one full-stack workspace, not as separate apps.

## Workspace boundary

- Repository root is the source of truth.
- Visual Studio may open `LibDtudo.Shared.slnx`, but `DtudoSite/`, `ApiMyAnimes/`, `ApiMyAnimeList/`, `LibDtudo.Shared/`, and `WinAppDtudo/` are all part of the same product.
- `ApiNode/` was removed from the repository; do not recreate it or reference it. The Discogs proxy was replaced by `ApiDiscogs` and `DtudoGateway`.
- Backend targets .NET 10.

## Main areas

### Frontend

- Path: `DtudoSite/`
- Stack: React 19 + Vite
- Entry: `DtudoSite/src/main.jsx`
- Router: `DtudoSite/src/router/DtudoRouter.jsx`
- Shared state: `DtudoSite/src/context_api/`
- Feature areas: `MyAnimes`, `Animes`, `Animex`, `MyMusicX`, `NinoTI`, `auth`

## Working rules

- Prefer existing components, contexts, providers, services, and routes.
- Do not assume a request belongs to only one stack.
- If a route or page changes, inspect frontend router and owning providers.
- If an endpoint or payload changes, inspect backend controller/service/model and frontend consumers.
- Treat `ApiMyAnimes` and `ApiMyAnimeList` as the preferred API owners for anime data.
- Keep changes minimal and local unless the feature clearly spans layers.
- `WinAppDtudo` is fully Dark Mode. Do not introduce light backgrounds or light-themed panels in WinForms screens; reuse `ThemeManager`, `DarkModeColors`, and the existing dark visual patterns.

## Useful commands

- `npm run serv`


## Persistence

This file and `.github/copilot-instructions.md` are intended to preserve repository context across reopened sessions in this folder.
