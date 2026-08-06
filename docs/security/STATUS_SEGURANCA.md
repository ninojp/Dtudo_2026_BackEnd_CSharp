# Status da Implementacao de Seguranca

## Estado geral

- Etapa atual: 10 (Concluida)
- Ultima etapa concluida: 10
- Proxima etapa permitida: 11 (abrir novo chat para executar exclusivamente a Etapa 11). Nao iniciar a Etapa 11 neste chat.
- Bloqueios globais: nenhum bloqueio na fundacao Development dentro do escopo aceito. O repositorio e pessoal, somente o proprietario faz commits, e regras administrativas do GitHub foram deliberadamente diferidas; a escolha de data/dominio e a promocao para servidor Windows proprio continuam diferidas e bloqueiam qualquer publicacao.

## Etapas

| Etapa | Estado | Evidencia principal | Data UTC |
| --- | --- | --- | --- |
| 01 | Concluida | Quatro documentos de seguranca consistentes e validacao executada | 2026-08-06 |
| 02 | Concluida | Varredura de conteudo/historico, fontes externas e falha fechada validadas | 2026-08-06 |
| 03 | Concluida | Workflow versionado, build/testes/lint e auditorias locais aprovados; controles administrativos do GitHub diferidos por decisao do proprietario | 2026-08-06 |
| 04 | Concluida | Serilog/Seq, correlacao, redacao e testes das duas APIs implementados | 2026-08-06 |
| 05 | Concluida | SecurityAuditEvents append-only, API interna, migration e 8 testes aprovados | 2026-08-06 |
| 06 | Concluida | Runner idempotente, snapshot real de 23 itens, restauracao isolada com `DBCC CHECKDB` e retencao de 30 dias validados | 2026-08-06 |
| 07 | Concluida | Development workstation aplicado e reaplicado sem elevacao: 18 Passed, 4 NotChecked, 0 Blocked, 0 Failed; baseline de promocao e rollback declarados sem segredos | 2026-08-06 |
| 08 | Concluida | Amostras locais de configuracao, pipeline, correlacao/redacao, auditoria, backup/restauracao e negativos aprovadas; escopo pessoal sem regras administrativas do GitHub aceito | 2026-08-06 |
| 09 | Concluida | ApiIdentity isolada com Identity/OpenIddict, migration e rollback LocalDB comprovados | 2026-08-06 |
| 10 | Concluida | Catalogo central de permissoes/papeis, maioridade, termos versionados, migration e 8 testes LocalDB aprovados | 2026-08-06 |
| 11 | Pendente | - | - |
| 12 | Pendente | - | - |
| 13 | Pendente | - | - |
| 14 | Pendente | - | - |
| 15 | Pendente | - | - |
| 16 | Pendente | - | - |
| 17 | Pendente | - | - |
| 18 | Pendente | - | - |
| 19 | Pendente | - | - |
| 20 | Pendente | - | - |
| 21 | Pendente | - | - |
| 22 | Pendente | - | - |
| 23 | Pendente | - | - |
| 24 | Pendente | - | - |
| 25 | Pendente | - | - |
| 26 | Pendente | - | - |
| 27 | Pendente | - | - |
| 28 | Pendente | - | - |
| 29 | Pendente | - | - |
| 30 | Pendente | - | - |

## Ultima execucao

- Objetivo: executar exclusivamente a Etapa 10, modelando maioridade, termos versionados, papeis, permissoes, politicas e contratos sem provisionar contas, MFA ou UI.
- Arquivos alterados/atualizados: `ApiIdentity/Models/`, `ApiIdentity/Authorization/AuthorizationCatalog.cs`, `ApiIdentity/Data/IdentityDbContext.cs`, `ApiIdentity/Program.cs`, a migration `AddIdentityGovernance`, `LibDtudo.Shared/Dtos/Auth/IdentityGovernanceContracts.cs`, `tests/ApiIdentity.Tests/` e este status.
- Testes executados: `dotnet build ApiIdentity/ApiIdentity.csproj --no-restore` foi aprovado. `dotnet test tests/ApiIdentity.Tests/ApiIdentity.Tests.csproj --no-restore` aprovou 8 testes: inicializacao segura, ausencia de cadastro publico, banco isolado, falha fechada sem conexao, politicas para cada permissao, catalogo semeado, constraints de maioridade, duplicidade de aceite, FK de permissao e rollback LocalDB para `0`. O rollback e a limpeza ocorreram em bancos LocalDB temporarios com nomes aleatorios; nenhum banco persistente foi alterado.
- Resultado: `IdentityAccount` armazena somente a declaracao de maioridade e o instante UTC, sem data de nascimento. Termos mantem tipo, versao, conteudo, hash SHA-256, publicacao UTC e estado ativo; cada aceite referencia o documento exato e nao pode duplicar o par conta/documento. O catalogo central contem somente os papeis `Superadministrador` e `Usuario do Site`, as permissoes previstas na matriz e suas atribuicoes; permissoes de servico continuam sem papel humano. As politicas nomeadas exigem a claim `permission` correspondente. A migration cria constraints, chaves estrangeiras, indices e seeds, sem contas nem clientes.
- Decisoes: novos papeis continuam proibidos sem decisao documentada. A emissao de claims a partir das atribuicoes papel-permissao, autenticacao de clientes e aplicacao das politicas em endpoints pertencem as etapas de sessao e protecao de APIs; esta etapa apenas cria o contrato e nega quando uma politica for usada sem a claim exigida. Nenhum texto juridico foi semeado: antes de qualquer aceite real, o operador deve cadastrar e revisar o conteudo e hash da versao de termos aplicavel.
- Riscos residuais: ainda nao ha provisionamento de contas, emissao de claims, MFA, sessao, cliente OpenIddict, UI ou endpoint protegido pelas novas politicas. A configuracao segura da conexao Development e os certificados/chaves nao se qualificam para homologacao/producao. O conteudo juridico e a revisao LGPD dos termos ainda precisam ocorrer antes de criar um documento de termos ativo.
- Rollback: em banco Development com configuracao segura, revisar a migration e executar `dotnet ef database update InitialIdentityFoundation --project ApiIdentity/ApiIdentity.csproj --startup-project ApiIdentity/ApiIdentity.csproj`; o teste tambem comprovou a reversao completa para `0` em banco temporario e a limpeza posterior. Para remover codigo ainda nao aplicado, remover a migration `AddIdentityGovernance` e os modelos/catalogo/contratos/testes desta etapa; nao houve alteracao em banco existente.
- Acoes manuais: antes de aplicar em banco persistente, configurar `ConnectionStrings:IdentityDb` em fonte segura, validar o catalogo de permissoes contra a matriz quando endpoints forem protegidos e aprovar o conteudo/hash da primeira versao juridica de termos. Abrir outro chat para a Etapa 11; ela nao foi iniciada neste chat.

## Decisoes posteriores ao plano

- Etapa 02: `ApiMyAnimeList` e `ApiMyAnimes` falham no startup sem configuracao obrigatoria; valores de ambiente nao ficam em `appsettings` versionado.
- Escopo operacional atual: a solucao permanece em Development e nao sera promovida para producao antes da conclusao integral; homologacao/producao ficaram deliberadamente fora desta execucao.
- Etapa 05: a trilha de auditoria de seguranca e separada dos logs tecnicos do Seq e deve ser gravada exclusivamente pelo contrato interno `ISecurityAuditWriter`.
- Etapa 06: backups validos ficam em snapshots UTC de `yyyyMMdd` no volume separado `D:` do ambiente de validacao; a retencao e de 30 dias e a restauracao deve ocorrer em banco/nome e diretorio isolados.
- Etapa 06: o agendamento criado e `Dtudo2026-Etapa06-Backup`; a identidade interativa atual e provisoria e deve ser substituida antes de homologacao/producao.
- Etapa 07: a baseline operacional fica em `scripts/DtudoInfrastructureBaseline.psd1`; o runner seleciona Development por padrao, e o perfil local foi aplicado sem publicar servicos; Homologation/Production permanecem diferidos e a Etapa 08 nao foi iniciada.
- Realinhamento 2026-08-06: a solucao permanece em desenvolvimento local e sem data de publicacao. A Etapa 07 e concluida no escopo Development; controles de hospedagem permanecem declarados ate a promocao. A fundacao prioriza identidade com contas pre-criadas, declaracao de maioridade, autenticacao/autorizacao no servidor, protecao proporcional de dados, Seq/auditoria e monitoramento local. O primeiro lancamento sera somente catalogo publico, sem login ou conteudo adulto, e nenhuma credencial de servico podera ser compartilhada com o React.
- Realinhamento Etapa 03 2026-08-06: por decisao do proprietario, o repositorio publico permanece de uso pessoal e sem colaboradores, runner proprio ou publicacao. O workflow versionado e as validacoes locais comprovadas atendem esta fase; controles administrativos avancados do GitHub ficam diferidos e devem ser reavaliados antes de mudar esse contexto. Segredos reais continuam proibidos no repositorio.
- Etapa 09: ApiIdentity possui banco, DbContext e conta proprios. A configuracao de banco e obrigatoria, externa ao repositorio e validada contra o nome do banco esperado; Development usa somente DPAPI e certificados OpenIddict de desenvolvimento fora do diretorio da aplicacao.
- Etapa 10: o catalogo de autorizacao possui somente `Superadministrador` e `Usuario do Site`. Permissoes humanas sao atribuicoes papel-permissao persistidas e permissao de servico permanece sem papel humano; todas as politicas usam o formato `permission:{chave}` e a claim `permission`.
- Etapa 10: maioridade e declarada por booleano e instante UTC, sem nascimento completo. Termos sao versionados por documento com tipo, versao, conteudo, hash e publicacao; aceite e unico por conta/documento e referencia o documento exato.
