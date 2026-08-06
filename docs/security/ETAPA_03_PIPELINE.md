# Etapa 03 - Pipeline e cadeia de dependencias

## Implementacao

- `.github/workflows/ci.yml` executa em `pull_request` para `main` e em `push` para `main`.
- O job `Build, test and lint` restaura, compila a solucao .NET em `Release` com analyzers, executa os testes e valida `npm run lint` e `npm run build`.
- `CodeQL / C#` e `CodeQL / JavaScript` executam analise estatica com consultas `security-extended`.
- `Dependency audit` bloqueia vulnerabilidades NuGet e npm de severidade alta ou critica.
- `Dependency review` avalia dependencias alteradas em pull requests.
- `Secret scan` usa Gitleaks para bloquear segredos detectados no conteudo e no historico analisado.
- `Workflow hygiene` bloqueia referencias `uses` que nao estejam fixadas em um SHA completo de 40 caracteres.

Todas as actions sao referenciadas por SHA imutavel e mantem a tag apenas como comentario de manutencao. O workflow nao usa `pull_request_target`, nao faz deploy e nao concede acesso a ambiente ou segredo de producao.

## Isolamento de pull requests

- Pull requests, inclusive os provenientes de fork, executam somente em runners hospedados `windows-2025` ou `ubuntu-24.04`.
- O token automatico recebe apenas `contents: read`, exceto a permissao `security-events: write` necessaria para CodeQL; em pull requests o upload de resultados fica desabilitado.
- `actions/checkout` usa `persist-credentials: false`.
- Instalacao npm usa `--ignore-scripts`, reduzindo execucao de scripts de pacotes durante o CI.
- Nao ha referencia a secrets de producao, `environment: production` ou labels `self-hosted` neste workflow.

O runner autohospedado de producao e os secrets de producao devem permanecer fora deste workflow. Qualquer futuro deploy deve ser um workflow separado, disparado apenas por branch/tag protegida, com environment protegido, aprovacao manual e runner group exclusivo; nunca deve aceitar `pull_request` como origem.

## Verificacoes manuais no GitHub

1. Em **Settings > Actions > General**, restringir actions permitidas aos fornecedores necessarios, exigir SHA completo quando a politica da organizacao permitir e manter a politica padrao do `GITHUB_TOKEN` como somente leitura.
2. Em **Settings > Code security and analysis**, habilitar secret scanning, push protection e code scanning para o repositorio. Confirmar que os alertas CodeQL ficam visiveis apos um `push` em `main`.
3. Criar ruleset para `main` exigindo os checks `Build, test and lint`, `CodeQL / C#`, `CodeQL / JavaScript`, `Dependency audit`, `Secret scan` e `Workflow hygiene`, alem de revisao obrigatoria e bloqueio de push direto.
4. Em **Settings > Actions > Runners**, manter o runner group de producao restrito aos workflows de deploy aprovados. Nao compartilhar labels de producao com runners de CI.
5. Em **Settings > Environments**, restringir `production` a branch/tag de release e revisores obrigatorios. Nenhum secret de producao deve ser criado no escopo do CI.
6. Abrir pull request temporario com um valor sintetico sem validade, por exemplo uma chave de teste conhecida, e confirmar que `Secret scan` falha. Remover o branch sem fazer merge.
7. Em outro branch temporario, introduzir uma versao deliberadamente vulneravel de dependencia e confirmar que `Dependency audit` ou `Dependency review` falha. Nao usar credencial real nem fazer merge do branch de teste.
8. Confirmar que um pull request de fork executa apenas runners hospedados, nao recebe secrets de repositorio e nao cria deployment ou acesso ao runner de producao.

Essas verificacoes dependem das configuracoes da conta GitHub, das regras do repositorio e dos runners; nao podem ser comprovadas somente por um build local.

## Rollback e risco residual

Rollback: remover `.github/workflows/ci.yml` e este documento, ou restaurar a revisao anterior do workflow, sem alterar banco, aplicacao ou secrets.

O audit npm local encontrou 3 vulnerabilidades restantes no `DtudoSite` (1 baixa e 2 altas) depois da atualizacao semantica do lockfile; as altas exigem revisao de upgrade major do React Router ou da ferramenta de build. O pipeline foi configurado para bloquear esse estado e a atualizacao/remediacao permanece uma pendencia explicita. O estado nao deve ser contornado aumentando o limiar do audit.
