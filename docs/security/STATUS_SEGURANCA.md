# Status da Implementacao de Seguranca

## Estado geral

- Etapa atual: 08
- Ultima etapa concluida: 07
- Proxima etapa permitida: 08 (gate independente; nao iniciar a Etapa 09)
- Bloqueios globais: a publicacao continua bloqueada por falhas existentes de lint/audit npm e pelas verificacoes manuais de configuracao do GitHub ainda pendentes. A escolha de data/dominio e a promocao para servidor Windows proprio foram diferidas; os controles de servidor da Etapa 07 nao bloqueiam a fundacao local, mas bloqueiam qualquer publicacao.

## Etapas

| Etapa | Estado | Evidencia principal | Data UTC |
| --- | --- | --- | --- |
| 01 | Concluida | Quatro documentos de seguranca consistentes e validacao executada | 2026-08-06 |
| 02 | Concluida | Varredura de conteudo/historico, fontes externas e falha fechada validadas | 2026-08-06 |
| 03 | Concluida | Workflow Release/testes/analise/auditoria/secret scan implementado e validado localmente | 2026-08-06 |
| 04 | Concluida | Serilog/Seq, correlacao, redacao e testes das duas APIs implementados | 2026-08-06 |
| 05 | Concluida | SecurityAuditEvents append-only, API interna, migration e 8 testes aprovados | 2026-08-06 |
| 06 | Concluida | Runner idempotente, snapshot real de 23 itens, restauracao isolada com `DBCC CHECKDB` e retencao de 30 dias validados | 2026-08-06 |
| 07 | Concluida | Development workstation aplicado e reaplicado sem elevacao: 18 Passed, 4 NotChecked, 0 Blocked, 0 Failed; baseline de promocao e rollback declarados sem segredos | 2026-08-06 |
| 08 | Pendente | - | - |
| 09 | Pendente | - | - |
| 10 | Pendente | - | - |
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

- Objetivo: realinhar a Etapa 07 ao objetivo de desenvolvimento local, sem custo recorrente e sem publicacao, preservando uma baseline de promocao futura sem segredos.
- Arquivos alterados/atualizados: `PLANO_SEGURANCA_DTUDO2026.md`; `docs/security/MATRIZ_ACESSO.md`; `docs/security/ETAPA_07_HARDENING.md`; este status. Evidencias tecnicas existentes da Etapa 07 permanecem em `scripts/DtudoInfrastructureBaseline.psd1` e `scripts/Invoke-DtudoInfrastructureHardening.ps1`.
- Testes executados: evidencias existentes da Etapa 07: importacao da baseline com tres ambientes, bancos e portas unicas; parser PowerShell; cinco negativos para traversal, banco duplicado, colisao de porta, porta interna publicada e credencial na baseline; listeners internos/SQL; consulta de `Dtudo2026Db`; `Apply` Development sem elevacao; reaplicacao idempotente. Nesta revisao: `git diff --check`, diagnostico dos documentos alterados e nova execucao de `Invoke-DtudoInfrastructureHardening.ps1 -Mode Validate -Environment Development -Json`.
- Resultado: a reclassificacao nao aplicou controles novos. A revalidacao registrou 18 checks aprovados, 4 `NotChecked`, 0 bloqueados, 0 falhos e `SecretsLogged=False`; o perfil Development possui raizes e ACLs locais, e os controles de servidor continuam apenas declarados e reversiveis. Nenhum servico foi iniciado ou publicado.
- Decisoes: Development local e o unico ambiente ativo. A primeira publicacao futura sera um catalogo publico estatico do DtudoSite, sem login, conteudo adulto ou dados pessoais; contas sao pre-criadas por procedimento administrativo e nunca compartilhadas com o React. IIS, Windows Authentication, SQL Express, BitLocker, firewall e contas Windows sao o perfil futuro do servidor Windows proprio, nao um pre-requisito para a fundacao atual. Identidade, maioridade, autenticacao, autorizacao, protecao proporcional de dados, Seq/auditoria e monitoramento local sao prioridades antes da publicacao.
- Riscos residuais: nao existe homologacao ou producao provisionada; contas Windows, IIS/certificados, SQL Express, firewall, Schannel e BitLocker ainda nao foram aplicados; bloqueios anteriores da Etapa 03 permanecem. Qualquer publicacao continua bloqueada ate a execucao do Gate 08, das etapas aplicaveis ao catalogo e da Etapa 27 no servidor Windows escolhido.
- Rollback: nenhuma alteracao de infraestrutura foi aplicada neste realinhamento. Para a baseline existente, `Invoke-DtudoInfrastructureHardening.ps1 -Mode Rollback` restaura SDDL/Schannel, remove apenas regras criadas, restaura backup IIS e remove diretorios vazios criados; `-RollbackSql` e obrigatorio para remover logins registrados como novos; nenhum dado e apagado automaticamente.
- Acoes manuais futuras: executar somente o Gate da Etapa 08 em chat independente; nao iniciar a Etapa 09 neste chat. Antes da Etapa 27, decidir data/dominio, preparar o servidor Windows, contas, SQL Express, IIS, certificados e BitLocker, aplicar a baseline elevada e executar as validacoes de hospedagem.

## Decisoes posteriores ao plano

- Etapa 02: `ApiMyAnimeList` e `ApiMyAnimes` falham no startup sem configuracao obrigatoria; valores de ambiente nao ficam em `appsettings` versionado.
- Escopo operacional atual: a solucao permanece em Development e nao sera promovida para producao antes da conclusao integral; homologacao/producao ficaram deliberadamente fora desta execucao.
- Etapa 05: a trilha de auditoria de seguranca e separada dos logs tecnicos do Seq e deve ser gravada exclusivamente pelo contrato interno `ISecurityAuditWriter`.
- Etapa 06: backups validos ficam em snapshots UTC de `yyyyMMdd` no volume separado `D:` do ambiente de validacao; a retencao e de 30 dias e a restauracao deve ocorrer em banco/nome e diretorio isolados.
- Etapa 06: o agendamento criado e `Dtudo2026-Etapa06-Backup`; a identidade interativa atual e provisoria e deve ser substituida antes de homologacao/producao.
- Etapa 07: a baseline operacional fica em `scripts/DtudoInfrastructureBaseline.psd1`; o runner seleciona Development por padrao, e o perfil local foi aplicado sem publicar servicos; Homologation/Production permanecem diferidos e a Etapa 08 nao foi iniciada.
- Realinhamento 2026-08-06: a solucao permanece em desenvolvimento local e sem data de publicacao. A Etapa 07 e concluida no escopo Development; controles de hospedagem permanecem declarados ate a promocao. A fundacao prioriza identidade com contas pre-criadas, declaracao de maioridade, autenticacao/autorizacao no servidor, protecao proporcional de dados, Seq/auditoria e monitoramento local. O primeiro lancamento sera somente catalogo publico, sem login ou conteudo adulto, e nenhuma credencial de servico podera ser compartilhada com o React.
