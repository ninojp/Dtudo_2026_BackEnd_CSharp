# ApiFileStorage

Serviço interno responsável por validar, promover e excluir arquivos apenas dentro de raízes autorizadas, com quarentena, verificação de magic bytes/MIME, scanner Defender/AMSI e reconciliação de diários.

## Superfície atual (2026-09-07)

Os endpoints de importação (`import`), destinos/plano de exportação (`export/destinations`, `export/plan`) e exclusão (`delete`, `delete/preview`, `delete/batch`) foram removidos: o WinApp não envia mais arquivos pela API — o salvamento de estruturas de MyAnime é feito diretamente no disco local, na pasta escolhida pelo operador via diálogo nativo do Windows, sem passar pela ApiFileStorage.

Endpoints anteriores ao monitoramento:

- `POST /api/file-storage/resolve`: metadados lógicos de um `ObjectId`, sem devolver caminho físico.
- `POST /api/file-storage/reconcile`: retomada de diários de quarentena/lixeira e purge autorizado (também executado automaticamente no startup).
- `GET /api/file-storage/health` e `GET /api/file-storage/startup`: sondas operacionais usadas pelo painel de saúde e pela inicialização do WinApp.

O motor interno de quarentena/scanner/promoção/lixeira (`FileStorageLifecycleService`) permanece implementado e coberto por testes, pronto para um futuro fluxo de ingestão, mas hoje não é alcançável por endpoint de escrita.

## Monitoramento passivo

O modulo `Monitoring/Data` e independente do motor de escrita existente. Usa EF Core
10.0.9 e SQL Server, com banco dedicado `DtudoFileMonitoring` e esquema `monitoring`.
Armazena localizacoes, inventarios, entradas e observacoes. Caminhos sao relativos,
sem truncamento em 260 caracteres; consultas de entradas usam paginacao por ID.

`Monitoring:Enabled=true` disponibiliza o monitor, mas nao inicia coleta. A migracao
nao e aplicada automaticamente e o registro dos servicos nao abre conexoes no startup.
Leitura e watchers existem somente enquanto houver sessao aberta pela interface.

Endpoints sob `/api/file-storage/monitoring`, autenticados com permissao e escopo
`filesystem.command`:

- `GET roots`: lista das 54 raizes autorizadas, sem enumeracao.
- `POST sessions`: abre escopo geral ou raiz/caminho relativo.
- `POST sessions/{id}/heartbeat`: renova prazo de 45 segundos e retorna progresso.
- `POST sessions/{id}/refresh`: solicita novo inventario.
- `DELETE sessions/{id}`: encerra coleta; nao exclui arquivos nem historico.
- `GET sessions/{id}/events`: notificacoes/historico com cursor, limite e modo recente.
- `GET sessions/{id}/entries/{locationId}`: entradas paginadas de um inventario.

Cada sessao pertence ao sujeito autenticado que a abriu. Escopos sobrepostos nao sao
abertos simultaneamente; isso evita duplicacao de coleta e nao bloqueia o catalogo.
Inventario e feito ao abrir ou atualizar explicitamente. Eventos marcam os dados como
desatualizados. Disponibilidade e verificada a cada 15 segundos; nao e diagnostico SMART.
Fila limitada e notificacoes de lacunas evitam apresentar cobertura falsa em overflow.

WinApp: menu `Monitoramento local` na tela principal e `Monitorar Estrutura` nos detalhes
MyAnime. A primeira abertura individual pede associacao explicita com pasta existente,
persistida pela ApiMyAnimes. Ambos usam a mesma UC Dark Mode; fechar encerra a sessao.

A conexao fica em `ConnectionStrings:FileMonitoring`, nos User Secrets ou ambiente,
e deve apontar exclusivamente para `DtudoFileMonitoring`. Nao usar a conexao do
catalogo. Os arquivos fisicos de dados/log devem ficar fora dos volumes monitorados.
No ambiente local autorizado, o banco ja foi criado na instancia LocalDB existente,
com arquivos em C:. Nao ha dados de colecoes persistidos ainda.

A fabrica EF suporta `--offline` para geracao de migrations sem banco. Para atualizar
o banco, utiliza User Secrets/ambiente e exige a conexao dedicada. Antes de provisionar
em outra maquina, verificar os caminhos fisicos padrao do SQL Server. A validacao de
nome da conexao nao verifica os caminhos dos arquivos do servidor.

```powershell
dotnet test tests/ApiFileStorage.Tests/ApiFileStorage.Tests.csproj -c Release --filter FullyQualifiedName~Monitoring
dotnet ef migrations has-pending-model-changes --project ApiFileStorage --context MonitoringDbContext --configuration Release -- --offline
```

O contexto impede atualizacoes/exclusoes via EF; isso nao impede SQL direto por um
administrador. Inventarios parciais/indisponiveis nao substituem o ultimo completo.
Os testes automatizados usam provedor SQL Server sem conexao para verificar o modelo
e EF InMemory para comportamento; nao sao substitutos de testes no SQL Server real.

**Nao configurar E:, H: ou suas colecoes em `FileStorage:Roots` para monitoramento.**
Essa configuracao pertence ao motor abaixo, que inclui reconciliacao com escrita.
A politica do leitor e separada, em `MonitoringPathPolicy`. O leitor usa handles
de leitura temporarios para estabilizar diretorios, sem modificar ACLs ou conteudo;
esses handles podem impedir brevemente renomeacao/exclusao concorrente do diretorio.
Requisitos, limites e progresso completos em
[MonitoramentoColecoes](../docs/MonitoramentoColecoes.md).

## Configurar uma raiz do motor existente

A pasta física deve existir antes da inicialização da API. Em Development, configure-a no User Secrets sem versionar o caminho da máquina:

```powershell
New-Item -ItemType Directory -Force "D:\Dtudo\Media"
dotnet user-secrets set "FileStorage:Roots:0:Id" "media" --project .\ApiFileStorage\ApiFileStorage.csproj
dotnet user-secrets set "FileStorage:Roots:0:Path" "D:\Dtudo\Media" --project .\ApiFileStorage\ApiFileStorage.csproj
```

Reinicie a ApiFileStorage depois de alterar raízes. A conta do processo da API precisa de ACL na raiz; o WinApp não precisa de permissão direta nela.
