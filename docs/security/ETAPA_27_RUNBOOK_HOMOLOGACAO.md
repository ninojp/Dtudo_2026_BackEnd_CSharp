# Etapa 27 - Opcoes de Homologacao sem Custo

## Estado e decisao atual

A Etapa 27 permanece `Bloqueada`. O ambiente atual deve continuar em Development local e nenhum servico de Homologation ou Production deve ser publicado por enquanto.

A preparacao tecnica ja versionada inclui gateway/YARP catalog-only, rotas publicas filtradas, headers, HSTS, CORS, rate limiting, health liveness, build sem segredos e runner de IIS/ACME/firewall. O bloqueio restante e operacional: dominio real, host Windows, IIS, certificado, listeners, firewall e renovacao ACME.

## Opcoes

| Opcao | Custo recorrente | Velocidade | Conclui a Etapa 27? |
| --- | ---: | ---: | --- |
| Development local | R$ 0 | imediata | Nao; mantem a etapa bloqueada |
| IIS local no workstation | R$ 0 | algumas horas | Nao prova DNS publico, firewall externo ou ACME real |
| PC ou VM Windows separado | R$ 0, se o hardware ja existir | meio dia a um dia | Sim, se todos os testes externos passarem |
| Tunnel gratuito | geralmente R$ 0 | rapido | Serve para demonstracao, mas nao comprova IIS, portas publicas e firewall |

A opcao recomendada enquanto nao houver data de publicacao e Development local com a Etapa 27 bloqueada.

## Opcao gratuita completa

Use um PC ou VM separado para Homologation. Nao exponha o workstation principal.

### Host

Prepare no host separado:

- Windows e IIS com ASP.NET Core Hosting Bundle;
- SQL Server Express e bancos exclusivos de Homologation;
- contas separadas para Gateway, APIs e backup;
- BitLocker e ACLs conforme a baseline;
- nenhum banco, certificado, chave ou segredo de Production reutilizado.

Bindings esperados:

```text
Gateway:     16443
ApiMyAnimes: 127.0.0.1:16080
ApiIdentity: 127.0.0.1:16081
Seq:         127.0.0.1:15341
```

Externamente, somente o gateway deve responder.

### Dominio gratuito

Um DDNS gratuito, como DuckDNS, pode ser usado quando:

- a conexao possui IPv4 publico;
- o provedor nao usa CGNAT;
- o roteador permite encaminhamento da porta HTTPS;
- o ISP nao bloqueia entrada.

Com CGNAT, sera necessario um host externo, tunnel ou servico pago. O nome `example.invalid` versionado no repositorio e apenas placeholder e nunca deve ser usado para emitir certificado.

### TLS e renovacao

Use Let's Encrypt com win-acme. Instale o certificado em:

```text
Cert:\LocalMachine\My
```

A renovacao deve ser automatica por Task Scheduler e direcionada somente a Homologation. Credenciais de DNS ficam no host e nunca no repositorio, no frontend, nos argumentos registrados ou no chat.

Use primeiro o ambiente staging da ACME para validar o procedimento sem consumir limites. Depois emita o certificado real.

## Procedimento posterior

### 1. Gerar o build catalog-only

```powershell
Set-Location C:\2026MeusProjetos\Dtudo2026\DtudoSite
npm ci
npm run build:homologation
```

Copie o `dist` para a raiz `wwwroot` do gateway de Homologation.

O scan deve permanecer sem estas superficies ou dados:

```text
auth/login
bff/login
mymusicx
ninoti
apiLocal
swagger
access_token
refresh_token
connectionstring
Hentai +18
hentai
```

### 2. Configurar o host real

Nao coloque o dominio real no React nem em segredo versionado. Use variaveis de ambiente ou fonte protegida do host:

```powershell
$env:Gateway__PublicOrigin = "https://catalogo-homolog.duckdns.org/"
$env:Gateway__AllowedRedirectOrigins__0 = "https://catalogo-homolog.duckdns.org/"
$env:Gateway__AllowedCorsOrigins__0 = "https://catalogo-homolog.duckdns.org/"
```

Substitua o exemplo por um dominio realmente controlado. A autoridade OIDC, connection strings e certificados tambem devem vir de fontes externas ao repositorio.

### 3. Validar sem aplicar

Execute no host de Homologation, em PowerShell elevado quando necessario:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File C:\2026MeusProjetos\Dtudo2026\scripts\Invoke-DtudoHomologationEdge.ps1 `
  -Mode Validate `
  -Hostname catalogo-homolog.duckdns.org `
  -StaticDistPath C:\ProgramData\Dtudo2026\Homologation\Gateway\dist `
  -Json
```

Nao execute `Apply` enquanto houver bloqueios de dominio, IIS, certificado, listeners ou firewall.

### 4. Aplicar somente Homologation

Depois de um `Validate` limpo:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File C:\2026MeusProjetos\Dtudo2026\scripts\Invoke-DtudoHomologationEdge.ps1 `
  -Mode Apply `
  -Hostname catalogo-homolog.duckdns.org `
  -StaticDistPath C:\ProgramData\Dtudo2026\Homologation\Gateway\dist `
  -StatePath C:\ProgramData\Dtudo2026\Homologation\Edge\state.json `
  -ProvisionCertificate `
  -Confirm
```

O parametro `-ProvisionCertificate` exige win-acme, dominio real, email operacional fornecido diretamente no host e validacao DNS-01 preparada. Como alternativa, instale o certificado manualmente e use `-CertificateThumbprint`.

O runner e restrito a Homologation, cria backup antes da alteracao e nao possui caminho de Apply para Production.

### 5. Testar de outra rede

Execute a partir de uma rede diferente do host:

```powershell
Resolve-DnsName catalogo-homolog.duckdns.org
Test-NetConnection catalogo-homolog.duckdns.org -Port 443
Test-NetConnection catalogo-homolog.duckdns.org -Port 16080
Test-NetConnection catalogo-homolog.duckdns.org -Port 16081
Test-NetConnection catalogo-homolog.duckdns.org -Port 15341
```

Esperado:

- porta `443`: acessivel;
- portas `16080`, `16081` e `15341`: inacessiveis externamente.

Rotas e headers:

```powershell
curl.exe -I https://catalogo-homolog.duckdns.org/
curl.exe -i https://catalogo-homolog.duckdns.org/health/live
curl.exe -i https://catalogo-homolog.duckdns.org/bff/login
curl.exe -i https://catalogo-homolog.duckdns.org/swagger
curl.exe -i https://catalogo-homolog.duckdns.org/health/ready
curl.exe -i -X POST https://catalogo-homolog.duckdns.org/api/catalog/animes
curl.exe -i -H "Origin: https://evil.example.invalid" https://catalogo-homolog.duckdns.org/health/live
```

Comprove certificado e cadeia, HSTS, `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, CSP, CORS permitido/negado, rate limit `429`, rotas privadas negadas, escrita negada, ausencia de conteudo adulto e health detalhado privado.

### 6. Testar renovacao direcionada

Use somente o nome da renewal de Homologation:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File C:\2026MeusProjetos\Dtudo2026\scripts\Invoke-DtudoHomologationEdge.ps1 `
  -Mode Renew `
  -RenewalName Dtudo2026-Etapa27-Homologation `
  -Confirm
```

Nunca execute uma renovacao global do host sem identificar o certificado de Homologation.

## Tunnel gratuito

Cloudflare Tunnel ou alternativa semelhante pode demonstrar o catalogo externamente sem abrir portas no roteador. Isso nao conclui a Etapa 27, porque nao prova o binding IIS publico, as portas, o firewall de entrada ou a renovacao ACME do host.

## Rollback

No host de Homologation, pare o site/processo e use o mesmo estado criado no Apply:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File C:\2026MeusProjetos\Dtudo2026\scripts\Invoke-DtudoHomologationEdge.ps1 `
  -Mode Rollback `
  -StatePath C:\ProgramData\Dtudo2026\Homologation\Edge\state.json `
  -Confirm
```

O rollback remove somente regras criadas pela etapa, restaura o backup IIS e o `wwwroot` anterior. Nao apaga bancos, dados, certificados, chaves ou Production automaticamente.

## Proibicoes

- Nao executar `Apply` no workstation Development.
- Nao usar dados, chaves ou certificados de Production.
- Nao colocar dominio real, senha, token, API key, connection string ou credencial DNS no Git.
- Nao expor SQL, Seq, Swagger, health detalhado ou APIs internas.
- Nao marcar a Etapa 27 como concluida sem testes externos reproduziveis.
- Nao iniciar a Etapa 28 enquanto a Etapa 27 estiver bloqueada.
