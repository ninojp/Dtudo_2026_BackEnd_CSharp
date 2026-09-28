# Monitoramento das colecoes locais

## Estado

Levantamento de requisitos e inventario de metadados em 2026-09-17.
Primeira etapa de persistencia implementada na ApiFileStorage: contexto EF Core,
modelo, consultas paginadas de entradas, gravacao de inventarios e migracao inicial.
O usuario autorizou o banco dedicado na mesma instancia do catalogo.

Banco `DtudoFileMonitoring` criado em `(localdb)\MSSQLLocalDB`; arquivos de dados
e log confirmados em `C:\Users\comer\DtudoFileMonitoring.mdf` e
`C:\Users\comer\DtudoFileMonitoring_log.ldf`. A conexao usa autenticacao Windows
e esta em User Secrets da ApiFileStorage, sob `ConnectionStrings:FileMonitoring`.
Na primeira etapa o catalogo nao foi alterado. Na integracao foi adicionada somente
a tabela `MyAnimeMonitoringLocations` ao `Dtudo2026Db`, pela migracao
`AddMyAnimeMonitoringLocation`, sem alteracao dos registros existentes de MyAnime.

O fluxo passivo esta implementado entre ApiFileStorage, ApiMyAnimes e WinAppDtudo.
`Monitoring:Enabled` agora e `true`, mas nao inicia coleta no startup. Leitor,
notificacoes e persistencia sao acionados somente por uma sessao aberta pela tela.
Nao houve escrita nas colecoes durante a implementacao; o inventario historico foi
obtido somente por leitura.
Os testes usam pastas temporarias fora das colecoes. Validacao visual com login e
escala de 200% deve ser realizada pelo usuario via Visual Studio 2026.

Validacao: 19 testes focados aprovados; geracao e consistencia do esquema SQL Server
verificadas. Uma transacao de validacao no banco real confirmou caminho de 340
caracteres, selecao do ultimo inventario completo e rejeicao de tamanho negativo.
A transacao foi revertida e as quatro tabelas permanecem sem dados sinteticos.
Testes de comportamento usam EF Core InMemory, que nao substitui testes relacionais.

## Arquitetura implementada

- ApiFileStorage: dona do inventario e historico no banco separado. O novo contexto
  usa o esquema `monitoring`, com `Locations`, `Snapshots`, `Entries` e `Observations`.
- ApiMyAnimes: dona do catalogo e do vinculo explicito MyAnimeId/raiz/caminho relativo.
  O vinculo e salvo em tabela separada, sem arvore ou arquivos no registro MyAnime.
- WinAppDtudo: abre/encerra sessoes pelas telas geral e individual. Um heartbeat renova
  a sessao; sem renovacao ela expira em 45 segundos, verificada a cada 5 segundos.
- LibDtudo.Shared: contem DTOs e a politica unica das 54 raizes/caminhos autorizados.
  Entidades EF internas nao sao expostas como contratos HTTP.
- Inventarios completos, parciais, indisponiveis e cancelados ficam distintos. A consulta
  ao ultimo inventario completo nao usa uma leitura parcial ou indisponivel como substituta.
- Caminhos sao relativos e sem limite artificial de 260 caracteres no banco. A validacao
  de entrada rejeita caminhos absolutos, travessia, streams alternativos e duplicatas.
  O leitor valida ancestrais e mantem handles de leitura durante a enumeracao de cada
  diretorio para evitar troca por juncoes. Nao abre conteudo de videos nem calcula hashes.
- O contexto EF rejeita alteracoes/exclusoes de registros; as FKs nao tem exclusao em
  cascata. Essa protecao nao e uma barreira contra comandos SQL diretos de administrador.
- Nao ha migrations automaticas na inicializacao nem acesso ao banco no startup pela
  nova camada. A conexao exige o banco dedicado e rejeita AttachDBFilename.
- Nao adicionar colecoes a `FileStorage:Roots`: essas raizes pertencem ao motor existente
  de escrita/reconciliacao. O leitor usa `MonitoringPathPolicy`, independente desse motor.

## Operacao e limites da entrega

1. Executar a solucao pelo Visual Studio 2026 e autenticar-se no WinApp.
2. Na tela principal, `Monitoramento local` abre o formulario geral.
3. Nos detalhes MyAnime, `Monitorar Estrutura` abre a aba individual. Na primeira
   abertura, escolher e confirmar a pasta existente. `Associar pasta` permite trocar
   explicitamente o vinculo no banco; nao modifica pastas/arquivos.
4. Consultar estrutura, ocorrencias recentes e historico paginado. `Pausar coleta`
   encerra a sessao; `Iniciar coleta` abre uma nova. Fechar a tela encerra sua coleta.
5. `Atualizar inventario` solicita uma nova enumeracao do escopo. Notificacoes em tempo
   real marcam o inventario como desatualizado; nao provocam varredura completa automatica.

- A interface consulta notificacoes a cada 3 segundos. A API verifica disponibilidade
  das localizacoes a cada 15 segundos, sem enumerar seus conteudos nessa verificacao.
- O inventario inicial e as atualizacoes sao sequenciais por raiz. A fila de eventos
  comporta 4096 notificacoes; overflow e registrado como lacuna, nunca ocultado como
  garantia de cobertura completa. A grade mostra ate 2000 registros por vez; o historico
  persistido permanece no banco e pode ser consultado em paginas.
- Sessoes simultaneas disjuntas sao permitidas, ate quatro. Escopos sobrepostos sao
  recusados com aviso: pausar/fechar a tela geral antes de iniciar a individual, ou o
  contrario. Isso nao bloqueia nenhuma operacao do catalogo ou do restante do WinApp.
- Encerramento normal descarta os watchers, cancela a enumeracao e tenta gravar os
  eventos pendentes. Se a API estiver inacessivel, o encerramento imediato nao pode ser
  confirmado; a sessao expira sem heartbeat. Uma chamada de I/O pendente depende tambem
  da resposta do Windows/disco; cancelamento e verificado entre as chamadas.
- Handles de leitura sao mantidos apenas durante a enumeracao de um diretorio e seus
  ancestrais. Eles nao alteram permissoes nem arquivos, mas podem impedir brevemente
  renomeacao/exclusao concorrente desses diretorios pelo compartilhamento do Windows.
- Falha de persistencia encerra a coleta dessa sessao e aparece no formulario. Nao ha
  spool em disco nem promessa de gravacao se o proprio banco estiver indisponivel.
- Alertas automaticos atuais: falha de leitura/acesso, indisponibilidade/retorno da
  localizacao, notificacoes perdidas, pastas vazias, arquivos de zero bytes e caminhos
  longos. Alertas nao executam reparos, movimentacoes ou exclusoes.
- Comparacao com o ultimo inventario completo registra inclusoes, remocoes e mudancas
  de tamanho/data/tipo; nao comprova integridade de conteudo. Nao detecta todas as
  alteracoes que preservem metadados e nao diagnostica defeitos fisicos silenciosos.
- Exportacao de relatorios, configuracao livre de raizes/limiares, auditoria de autoria
  e retencao automatica continuam fora do escopo implementado. Foram sugestoes futuras;
  nao ha limpeza automatica do historico. Apenas uma localizacao ativa por MyAnime.

## Regras confirmadas pelo usuario

- Monitorar exclusivamente as colecoes dentro das letras e de `.Dots`.
- Ignorar totalmente `H:\AnimeX\.ImportanteX` e seus descendentes. E uma area
  temporaria organizada posteriormente pelo usuario, de forma manual. Nao
  enumerar, monitorar, contabilizar ou gerar alertas sobre essa area.
- Ignorar pastas de sistema, lixeira e quaisquer outras areas fora das raizes
  explicitamente autorizadas. Nao seguir links ou juncoes implicitamente.
- Preservar os arquivos e as estruturas existentes: nao renomear, mover,
  sobrescrever, excluir ou corrigir automaticamente.
- A autorizacao mais recente limita a excecao de escrita a adicao de logs de
  monitoramento ou auditoria. Nao presumir autorizacao para manifestos JSON ou
  outros arquivos auxiliares dentro das colecoes. Historico e logs devem ficar
  fora de E: e H:, em armazenamento proprio da aplicacao. Destino exato,
  formato, acrescimo de registros e retencao ainda precisam ser definidos.
  Nao sobrescrever arquivos existentes nem escrever na area excluida.
- Iniciar a coleta somente ao abrir a tela de monitoramento correspondente e
  interrompe-la ao fechar essa tela. Apenas executar WinAppDtudo ou manter uma API
  aberta nao autoriza coleta em segundo plano. Fechar o WinApp encerra a coleta.
- Manter historico das mudancas e consulta ao ultimo estado conhecido, distinguindo
  esse estado do estado atual confirmado.
- Os dois HDs normalmente devem estar conectados e disponiveis. Se houver falha,
  apenas notificar, alertar e registrar o incidente: NENHUM BLOQUEIO de outras
  operacoes ou funcionalidades do aplicativo deve ser introduzido pelo monitor.
- Exibir alertas somente no novo formulario de monitoramento em tempo real.
  O requisito anterior de alertas imediatos na abertura do WinApp foi revogado.
- Registrar passivamente o que mudou nos arquivos e pastas, sem identificar usuario
  ou programa responsavel nesta etapa. A auditoria detalhada foi adiada pelo usuario.
- Nao habilitar auditoria de seguranca do Windows, configurar SACL, alterar politicas
  ou permissoes, nem ativar telemetria de autoria nesta etapa. O usuario explicitamente
  nao autorizou essas intervencoes.
- Minimizar impacto no Windows 11, CPU, memoria e especialmente I/O dos HDs.
  Evitar varreduras completas repetitivas e leituras extensas de conteudo por padrao.
- Uma falha de acesso ou desconexao nunca deve converter o ultimo inventario valido
  em uma colecao vazia nem ser registrada como exclusao em massa.
- Centralizar a identificacao e o acompanhamento de problemas, inconsistencias
  e oportunidades de otimizacao para resolucao gradual pelo usuario.
- Suportar caminhos longos desde o inicio do monitor. A investigacao das falhas
  atuais com esses caminhos pode ocorrer em uma etapa posterior.
- WinAppDtudo e a interface principal. O usuario executa o backend pelo
  Visual Studio 2026. A arquitetura proposta e o banco dedicado foram autorizados;
  a implementacao esta sendo feita em etapas validadas.

## Fluxo atual informado pelo usuario

1. Busca Externa consulta a ApiMyAnimeList pelo WinAppDtudo.
2. Salvar Como MyAnime, nos detalhes do anime, cadastra a colecao e os animes
   relacionados no banco local via ApiMyAnimes.
3. Salvar Estrutura, nos detalhes MyAnime, solicita um destino e cria pastas e
   arquivos nesse local.
4. O usuario localiza a colecao pelo nome no Windows Explorer, confere a estrutura
   e adiciona os videos manualmente nas pastas correspondentes.

Esse relato descreve o fluxo anterior por nome. Agora a aba individual solicita uma
associacao explicita, persistida pela ApiMyAnimes; nao associa por semelhanca de titulo.
Salvar Estrutura permanece autorizado quando acionado pelo usuario com confirmacao
do destino, exclusivamente para adicionar pastas e arquivos sem modificar ou excluir
os existentes. Esse e o contrato informado pelo usuario, nao uma verificacao do codigo
atual. Nao alterar esse comando nesta etapa nem aciona-lo automaticamente pelo monitor.

## Interface de monitoramento

- Monitoramento geral: novo botao na tela principal do WinAppDtudo abre um formulario
  dedicado, em Dark Mode, para todas as estruturas de colecoes autorizadas. Abrir
  inicia a coleta geral; fechar encerra essa coleta. Nao inclui areas excluidas.
- Monitorar Estrutura: acesso nos detalhes MyAnime para acompanhar exclusivamente
  a estrutura da colecao selecionada, com sua pasta raiz, descendentes e estatisticas.
  Abrir apenas essa tela nao deve iniciar uma varredura geral dos outros acervos.
- Os dois escopos sao distintos; nao tratar a tela individual apenas como um filtro
  visual sobre uma coleta global obrigatoria. Ambos reutilizam a UC de monitoramento.
- Se as duas telas coexistirem, a coordenacao das sessoes deve evitar coleta e registros
  duplicados para o mesmo caminho. A entrega recusa escopos sobrepostos com aviso.
- Pausar/iniciar coleta e consultar historico estao implementados. Configuracao livre
  de alertas, exportacao de relatorios e selecao geral de arquivos continuam futuras.
- A eventual selecao de pastas nao autoriza incluir areas explicitamente excluidas.

## Objetivo de acompanhamento e limites tecnicos

- Acompanhar mudancas e inconsistencias prontamente, sem alterar as estruturas.
  A frequencia discutida diz respeito apenas a leituras, nunca a modificacoes.
- Registrar mudancas observadas e falhas de monitoramento. Uma notificacao de
  sistema de arquivos nao equivale a uma auditoria completa de todas as operacoes:
  eventos podem ser perdidos, agrupados ou repetidos e nao identificam necessariamente
  o usuario ou processo responsavel. Essa identificacao esta fora do escopo atual.
- O registro deve distinguir horario da observacao, metadados do arquivo e momento
  real da operacao quando desconhecido. Nao atribuir autoria ao usuario apenas porque
  ele e o unico operador habitual do computador.
- Auditoria detalhada futura depende de nova avaliacao e autorizacao. Nao usar SACL,
  ETW ou outro mecanismo de autoria como substituicao implicita ao escopo passivo.
- Nao prometer deteccao instantanea de toda inconsistencia ou defeito fisico. Falhas
  silenciosas e corrupcao de conteudo nao sao verificadas apenas por nomes e tamanhos.
- Com a tela de monitoramento fechada nao ha coleta ativa de sua sessao, mesmo que
  WinApp continue aberto. Uma comparacao ao reabrir pode apontar diferencas de estado,
  mas nao reconstruir todas as operacoes intermediarias ou seus horarios exatos.
  O intervalo sem coleta deve ficar explicito no historico; diferencas encontradas
  na reabertura nao devem ser apresentadas como eventos observados em tempo real.
- O monitor e observacional: nao bloqueia operacoes no WinApp, Explorer ou outros
  programas. Nao alterar permissoes do Windows ou acesso de terceiros nesta etapa.
- Falhas do proprio monitor, do banco ou do registro de auditoria devem ser visiveis
  no formulario, sem bloquear outras funcionalidades. Nao prometer persistencia de
  todos os incidentes quando o proprio armazenamento de auditoria estiver indisponivel;
  a entrega usa fila limitada, registra lacunas e interrompe a sessao em falha de gravacao.

## Raizes autorizadas

| Local | Rotulo | Pastas monitoradas |
| --- | --- | --- |
| E: | ANIMEs | .Dots, A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q |
| H: | ANIMEs2 | R, S, T, U, V, W, X, Y, Z |
| H:\AnimeX | AnimeX | .Dots e as letras de A a Z |

Isso representa 54 raizes de enumeracao permitidas, nunca `H:\AnimeX` inteiro.
ANIMEs e ANIMEs2 sao rotulos de volume, nao subpastas. `.Dots` substitui o nome
antigo `#Dots` na nova disposicao.

### Atualizacao dos apontamentos existentes

O script `scripts/Sync-MyAnimeMonitoringLocations.ps1` usa as raizes acima para
descobrir pastas e associacoes. Ele traduz as chaves da disposicao anterior antes
de calcular o plano:

| Chave anterior | Chave atual |
| --- | --- |
| `H_#Dots` e `H_A` a `H_Q` | `E_.Dots` e `E_A` a `E_Q` |
| `G_S`, `G_V`, `G_W`, `G_X`, `G_Y`, `G_Z` | `H_S`, `H_V`, `H_W`, `H_X`, `H_Y`, `H_Z` |
| `J_T` | `H_T` |
| `X_#Dots` | `X_.Dots` |

`H_R` permanece `H_R` porque R continua fisicamente em `H:\R`. Qualquer
apontamento intermediario `E_R` deve ser convertido para `H_R`.

As chaves `X_A` a `X_Z` permanecem as mesmas, mas agora resolvem para
`H:\AnimeX`. O caminho relativo da pasta da colecao nao e alterado.

Executar primeiro a pre-visualizacao e revisar o relatorio gerado fora dos discos
monitorados. Somente depois aplicar:

```powershell
pwsh -NoLogo -NoProfile -File .\scripts\Sync-MyAnimeMonitoringLocations.ps1
pwsh -NoLogo -NoProfile -File .\scripts\Sync-MyAnimeMonitoringLocations.ps1 -Apply
```

No modo `-Apply`, o script exige a descoberta completa, atualiza apenas
`dbo.MyAnimeMonitoringLocations` dentro de uma transacao serializavel e verifica
cada remapeamento antes do commit. Nao altera `MyAnimes`, arquivos ou pastas. Se
alguma raiz nova estiver indisponivel ou houver erro de leitura, a aplicacao e
recusada e a transacao nao e confirmada.

### Sincronizacao pelo formulario

O formulario `Monitoramento das colecoes locais` possui o botao `Sincronizar
colecoes` no escopo geral. Ao clicar, a coleta ativa e pausada temporariamente,
as 54 raizes sao descobertas por leitura e a coleta e retomada ao final.

As imagens numericas de todas as subpastas sao a evidencia principal. Somente
IDs positivos presentes no catalogo sao considerados; a colecao com a maior
quantidade de IDs correspondentes vence quando o resultado e unico. Empates,
IDs compartilhados e colecoes disputadas ficam pendentes. O nome da pasta e
usado como fallback ou desempate comparando somente as quatro primeiras
palavras normalizadas, tolerando truncamento e quantidades diferentes de
palavras. Pastas sem colecao, raizes indisponiveis e caminhos invalidos nao
recebem associacao arbitraria.

A descoberta percorre recursivamente as subpastas autorizadas e reconhece
imagens `.jpg`, `.jpeg`, `.png`, `.webp`, `.gif` e `.bmp`, sem ler o conteudo
dos arquivos.

Cada execucao e persistida em `MyAnimeMonitoringSyncRuns` e cada raiz, pasta,
alteracao, pendencia ou erro em `MyAnimeMonitoringSyncEvents`. A aba
`Sincronizacao` mostra todas as ocorrencias retornadas, e a API permite consultar
uma execucao pelo seu identificador mesmo depois de fechar o formulario.

As linhas podem ser clicadas para abrir a aba `Estrutura My` da colecao quando o
evento possui `MyAnimeId`. Erros ficam destacados em vermelho, alteracoes de
apontamento em azul e inclusoes/vinculos confirmados em verde. O filtro permite
exibir erros, alteracoes ou atualizacoes, e `Exportar TXT`/`Exportar CSV` grava
exatamente as linhas atualmente visiveis. Ocorrencias de raiz indisponivel ou de
pasta ainda sem MyAnime associado permanecem clicaveis, mas nao possuem uma aba
de estrutura especifica para abrir.

Estados possiveis: `Completed` quando todas as raizes foram processadas,
`Partial` quando uma ou mais raizes ficaram indisponiveis mas as demais foram
aplicadas, `Blocked` quando existe erro estrutural que impede qualquer alteracao,
`Failed` quando houve falha inesperada e `Cancelled` quando a operacao foi
cancelada. Os eventos persistidos sao
append-only. Nenhuma etapa de sincronizacao cria, move, renomeia, exclui ou
altera arquivos e pastas locais.

Antes do primeiro uso do botao, aplicar a migration do catalogo pelo Visual
Studio ou pelo comando equivalente:

```powershell
dotnet ef database update --project .\ApiMyAnimes\ApiMyAnimes.csproj --startup-project .\ApiMyAnimes\ApiMyAnimes.csproj
```

## Inventario de referencia, com exclusoes aplicadas

Os dados abaixo sao o inventario historico da disposicao anterior em H:, G: e J:;
nao representam uma nova leitura apos a redistribuicao para E: e H:. Nao executar
uma nova enumeracao apenas para atualizar estes numeros.
Tamanhos sao somas logicas de arquivos, em bytes; TB e GB usam base decimal.

| Volume no inventario historico | Arquivos | Bytes das colecoes |
| --- | ---: | ---: |
| H: | 29.617 | 7.584.425.992.790 |
| G: | 14.813 | 2.540.868.638.966 |
| J: | 2.965 | 849.472.261.986 |
| Total | 47.395 | 10.974.766.893.742 |

- 1.886 pastas candidatas a colecao: 1.423 principais e 463 em AnimeX.
  Nao foram cruzadas com os registros MyAnime do banco.
- 8.029 pastas abaixo das 54 raizes autorizadas; as raizes nao entram nesse total.
- 35.094 arquivos classificados como video pela extensao, nao pelo conteudo.
- 5.604 arquivos JPG com nome numerico; 49 nomes numericos aparecem mais de uma vez.
- Padrao predominante: letra -> colecao -> ano/titulo/tipo -> arquivos ou
  subdivisoes. Temporadas, versoes e materiais extras exigem profundidade variavel.
- A enumeracao original nao registrou erros nem encontrou pontos de redirecionamento.

O espaco livre observado foi de 415,69 GB (5,20%) em H:, 177,74 GB (5,92%) em G:
e 150,54 GB (15,05%) em J:, na disposicao historica. Espaco livre e uma medida do volume inteiro, nao das
colecoes: reflete indiretamente qualquer uso do disco, mesmo fora do escopo.
Nao atribuir essa ocupacao a pastas excluidas nem inventariar essas pastas.

## Registro inicial de alertas

Todos os itens abaixo estao pendentes de analise. Nao houve correcao automatica.

| ID | Observacao | Quantidade | Interpretacao |
| --- | --- | ---: | --- |
| CAMINHO-LONGO | Caminho com 260 caracteres ou mais | 82 | Maximo de 330; risco de incompatibilidade com ferramentas, nao prova de arquivo invalido |
| PASTA-VAZIA | Diretorio sem entradas na leitura | 5 | Pode ser estrutura preparada para conteudo futuro |
| ARQUIVO-ZERO | Arquivo com zero bytes | 6 | Tres legendas, dois videos e um TXT; avaliar individualmente |
| SEM-VIDEO | Colecao principal candidata sem extensoes de video verificadas | 25 | Nao significa pasta vazia ou colecao incompleta confirmada |
| JPG-NUMERICO-REPETIDO | Nome numerico JPG repetido | 49 nomes | Nao prova duplicacao de conteudo ou associacao incorreta |
| ESPACO-LIVRE | Volume com menos de 6% livre na observacao historica | 2 volumes | H: e G: na disposicao anterior; limite de alerta definitivo ainda nao escolhido |

O total anterior de 87 caminhos longos incluia itens fora do escopo atual e foi
substituido por 82. Os totais anteriores de arquivos, bytes e JPG numericos
tambem foram substituidos pelos valores deste documento.

## Limites e decisoes pendentes

- Este documento registra os achados agregados e algumas ocorrencias; nao e um
  inventario persistente completo de todos os caminhos. A lista integral usada
  na analise permanece apenas na memoria da sessao do terminal.
- A leitura nao foi um snapshot transacional. Alteracoes externas durante a
  enumeracao podem afetar os resultados. Ausencia de erro nao comprova integridade.
- Nao houve leitura de conteudo, hashes, validacao de reproducao, consulta ao
  banco ou diagnostico fisico aprofundado dos discos.
- Retencao do historico e destino adicional de logs tecnicos ainda precisam ser
  definidos. Associacao pasta/MyAnime, intervalos e persistencia SQL Server foram
  implementados. A API usa as permissoes atuais, sem alterar ACLs. Historico fora
  de E: e H:, ausencia
  de bloqueios, coleta vinculada a abertura/fechamento da tela e separacao entre
  escopo geral e por colecao ja sao requisitos confirmados. Identificacao de autoria
  nao faz parte desta etapa.
- Um caminho longo nao se resolve universalmente com uma unica configuracao:
  Windows, runtime, bibliotecas e ferramentas consumidoras precisam ser avaliados.
  Nao encurtar nomes nem alterar politicas do Windows sem autorizacao especifica.
- A ApiFileStorage possui capacidades de escrita/reconciliacao preexistentes;
  o futuro monitor deve ser isolado delas antes de receber acesso as colecoes.
