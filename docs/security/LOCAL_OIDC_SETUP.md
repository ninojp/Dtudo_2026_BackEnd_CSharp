# Configuracao OIDC local do DtudoSite

## O que e o ClientSecret

O `ClientSecret` nao e a senha de uma pessoa. Ele e uma credencial interna usada pelo `DtudoGateway` para provar a identidade dele para a `ApiIdentity` durante o fluxo OpenID Connect.

O par local e:

- `ClientId`: `dtudo-gateway`. Identifica o gateway e pode aparecer nas URLs OIDC.
- `ClientSecret`: valor aleatorio privado compartilhado somente entre o gateway e a ApiIdentity.

Ele nao e o Client ID da MyAnimeList, nao e a senha do usuario do site e nao deve ser colocado no React, em `localStorage`, em `appsettings.json` ou no Git.

## Onde fica

O mesmo valor precisa existir nos dois projetos, usando User Secrets:

| Projeto | Chave |
| --- | --- |
| `DtudoGateway` | `OpenIdConnect:ClientSecret` |
| `ApiIdentity` | `OpenIddict:Gateway:ClientSecret` |

No Windows, os arquivos ficam fora do repositorio em:

```text
%APPDATA%\Microsoft\UserSecrets\dtudo2026-gateway\secrets.json
%APPDATA%\Microsoft\UserSecrets\dtudo2026-apiidentity\secrets.json
```

## Criar ou substituir o segredo

Execute na raiz da solucao. O valor nao aparece no terminal:

```powershell
$bytes = New-Object byte[] 48
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
try {
    $rng.GetBytes($bytes)
    $secret = [Convert]::ToBase64String($bytes).TrimEnd('=').Replace('+','-').Replace('/','_')
    dotnet user-secrets set "OpenIdConnect:ClientSecret" $secret --project .\DtudoGateway\DtudoGateway.csproj
    dotnet user-secrets set "OpenIddict:Gateway:ClientSecret" $secret --project .\ApiIdentity\ApiIdentity.csproj
}
finally {
    $secret = $null
    $bytes = $null
    $rng.Dispose()
}
```

Ao iniciar a `ApiIdentity`, o cliente `dtudo-gateway` e registrado ou atualizado no banco `DtudoIdentity.Development` com esse valor, o callback `https://localhost:51376/signin-oidc` e o callback de logout.

## Iniciar o ambiente

O comando integrado inicia a `ApiIdentity`, as APIs do catalogo, o gateway, o proxy legado e o Vite:

```powershell
Push-Location .\DtudoSite
npm run serv
Pop-Location
```

O fluxo de login comeca em `http://localhost:5173/`, passa pelo aviso de maioridade, pelo gateway e chega a `https://localhost:7243/account/login`. Depois do callback OIDC, o gateway devolve o navegador ao frontend Vite.

## Regra geral de inicializacao

O `WinAppDtudo` e o gestor local. A regra simples para qualquer operacao que dependa de um servico e:

1. Verificar o endpoint de health do servico.
2. Se estiver disponivel, reutilizar a instancia existente.
3. Se estiver indisponivel em Development, iniciar o projeto local correspondente.
4. Aguardar o health check confirmar que o servico ficou pronto.
5. Somente depois abrir a tela, o navegador ou executar a operacao.
6. Se o servico nao ficar pronto, nao executar a operacao e mostrar qual dependencia falhou.

Em producao o WinApp nao inicia processos de desenvolvimento; ele apenas verifica a disponibilidade e informa a falha.

Ao abrir o DtudoSite pelo WinApp, a ordem e:

```text
ApiIdentity -> DtudoGateway -> ApiMyAnimes -> proxy MyMusicX -> Vite -> abrir o site
```

O `npm run serv` usa health checks idempotentes para nao criar uma segunda instancia quando o servico ja estiver ativo. Operacoes administrativas do WinApp tambem garantem o `ApiMyAnimes` e o `ApiFileStorage` antes de consumir essas APIs.
