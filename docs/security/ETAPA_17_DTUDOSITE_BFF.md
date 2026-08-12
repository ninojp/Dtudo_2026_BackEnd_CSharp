# Etapa 17 - DtudoSite com BFF

## Estado

- Estado: `Concluida no escopo de implementacao e validacao local Development`.
- O React usa o gateway/BFF para sessao e para as rotas publicas do catalogo.
- Nenhum access token ou refresh token e lido, persistido ou enviado pelo JavaScript.
- O catalogo permanece anonimo e usa somente rotas GET allowlisted do gateway.

## Alteracoes

- `src/services/bffClient.js` centraliza requisicoes BFF com `credentials: include`, tratamento de `401`/`403`, allowlist local de `returnUrl` e erro tipado. O unico token manipulado pelo cliente e o request token antiforgery, mantido apenas em memoria durante o logout.
- `useAuth` consulta `/bff/me` no carregamento, mantem somente o usuario em memoria, inicia o login por `/bff/login`, busca antiforgery e executa `POST /bff/logout`. Falha de autorizacao limpa a sessao local e publica o evento de expiracao/revogacao.
- O router nao possui mais cadastro publico. A tela de login nao coleta e-mail ou senha; ela redireciona para o fluxo OIDC do gateway. A protecao de rota conserva a URL local de retorno.
- O catalogo trocou `ApiMyAnimes` direto por `/api/catalog/animes`, `/api/catalog/animes/search`, `/api/catalog/animes/{id}` e `/api/catalog/collections`, sem `withCredentials`.
- A leitura de `VITE_DISCOGS_TOKEN` foi removida do React; a credencial pertence somente ao `ApiDiscogs` via User Secrets.
- Os arquivos de cadastro legado foram removidos por nao existir rota BFF de registro nem cadastro publico previsto no plano.

## Evidencias

Comandos executados:

```text
Push-Location DtudoSite; npm run lint; npm run build; Pop-Location
dotnet test .\tests\DtudoGateway.Tests\DtudoGateway.Tests.csproj --no-restore
```

Resultados:

- `npm run lint`: sucesso, sem erros.
- `npm run build`: sucesso, 244 modulos transformados.
- `DtudoGateway.Tests`: `10/10` testes aprovados, `0` falhas e `0` ignorados.
- `get_errors`: nenhum erro nos arquivos frontend tocados.
- Scan PowerShell de `src` e `dist/assets`: nenhuma referencia a `localStorage`, `sessionStorage`, `auth_token`, `auth_user`, `Bearer`, `accessToken`, `refreshToken`, `VITE_DISCOGS_TOKEN`, rotas `apiLocal` de anime, cadastro ou URLs de autenticacao legada.

Os testes do gateway cobrem o contrato consumido pelo frontend: `/bff/me`, challenge de login, allowlist de redirect, antiforgery, logout, atributos de cookie, ausencia de tokens na resposta da sessao, rotas de catalogo somente GET e bloqueio de APIs internas.

## Decisoes

- O usuario autenticado nao e reconstruido de armazenamento do navegador; a fonte e `/bff/me` e o estado exibido fica somente em memoria React.
- O login nao aceita credenciais no site. O provedor OIDC e responsavel pela autenticacao e o gateway e responsavel pelo cookie `HttpOnly`.
- `401` e `403` em requisicoes BFF limpam o estado de sessao e sinalizam expiracao/revogacao sem tentar refresh token no browser.
- A origem BFF pode ser configurada por `VITE_BFF_BASE_URL`; sem essa variavel, o site usa a propria origem, adequado ao deployment same-origin previsto.

## Riscos residuais e acoes manuais

- O OIDC live nao foi executado nesta sessao: nao havia processos escutando nas portas locais do gateway/Identity/catalogo e nao havia User Secrets configurados. E necessario fornecer `OpenIdConnect:ClientSecret` por fonte externa, iniciar os servicos e exercitar login, callback, logout, expiracao e revogacao antes da promocao.
- O desenvolvimento com Vite separado precisa usar uma origem BFF/gateway configurada e allowlisted; o deployment previsto deve servir o frontend na origem publica do gateway ou configurar uma borda equivalente sem abrir APIs internas.
- O modulo MyMusicX usa os contratos `ApiMusicX` e `ApiDiscogs` publicados pelo gateway; o proxy Node legado foi retirado.

## Rollback

Restaurar `useAuth`, `Login`, `Logout`, router, cliente de anime e provider para as versoes anteriores, reverter `bffClient.js`, restaurar os arquivos de cadastro e retirar as rotas `/api/catalog` consumidas pelo frontend. Nao ha migration nem alteracao de banco.

## Proxima etapa

A Etapa 18 e a unica proxima etapa permitida. Ela nao foi iniciada neste chat.
