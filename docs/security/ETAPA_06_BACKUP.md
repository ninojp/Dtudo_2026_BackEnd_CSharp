# Etapa 06 - Backup e restauracao

## Implementacao

- `scripts/Invoke-DtudoBackup.ps1` executa `Backup`, `RestoreVerify` e `Prune`.
- Cada execucao de backup monta primeiro uma pasta temporaria e publica um snapshot UTC em `yyyyMMdd` somente depois de concluir as copias e hashes.
- Repetir o backup no mesmo dia substitui apenas o snapshot diario que possui manifesto valido. Um diretorio sem manifesto nao e removido.
- O manifesto guarda somente metadados, tamanhos e SHA-256. Nao guarda conteudo de arquivo, connection string, senha, token ou chave.
- Os bancos usam `sqlcmd` com autenticacao integrada, `COPY_ONLY`, `CHECKSUM` e `RESTORE VERIFYONLY`. O runner nao aceita usuario ou senha SQL.
- `scripts/Register-DtudoBackupTask.ps1` registra a tarefa diaria de forma idempotente, sem armazenar credenciais. A tarefa chama o runner e a propria execucao aplica a retencao.

## Escopo padrao

O runner inclui as fontes presentes no workspace:

- banco `Dtudo2026Db`;
- `ApiMyAnimes/App_Data`, que pode conter dados de autenticacao e exige ACL restrita no destino;
- configuracoes de `ApiMyAnimes`, `ApiMyAnimeList` e `WinAppDtudo`, incluindo `appsettings` e `launchSettings`;
- migrations, configuracoes de banco, projetos, o runner e este plano como material de recuperacao.

Raizes de midia escolhidas pelo operador nao sao inventadas pelo script. Devem ser informadas explicitamente, por exemplo com `-FileSource 'D:\MediaRoot'`. Material externo de recuperacao, como chaves ou certificados protegidos, deve ser informado com `-RecoveryMaterialPath` apontando para uma raiz ACL-protegida. O valor do material nunca deve ser colocado em argumento, log, manifesto ou status.

O `.env` e arquivos de segredo nao fazem parte da lista padrao. Quando um segredo for material necessario para recuperacao, o arquivo deve permanecer em uma raiz protegida e ser incluido somente pela operacao controlada de backup; o relatorio continua registrando apenas hash e resultado.

## Operacao diaria

Destino usado na validacao local: `D:\Dtudo2026-Backups`. O volume `D:` e separado do volume `C:` onde esta o workspace.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Invoke-DtudoBackup.ps1 `
  -Mode Backup `
  -BackupRoot 'D:\Dtudo2026-Backups' `
  -RepositoryRoot (Get-Location).Path `
  -SqlServer '(localdb)\MSSQLLocalDB' `
  -DatabaseName 'Dtudo2026Db'
```

O agendamento criado para este ambiente e `Dtudo2026-Etapa06-Backup`, diario as 02:00, com `StartWhenAvailable`, execucao limitada a oito horas e `MultipleInstances IgnoreNew`. O registrador usa a identidade Windows interativa atual e nivel limitado, sem senha armazenada. Antes de homologacao/producao, a Etapa 07 deve substituir essa identidade por conta dedicada e revisar ACL, BitLocker e permissao do SQL.

Para registrar ou atualizar a mesma tarefa:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Register-DtudoBackupTask.ps1 `
  -TaskName 'Dtudo2026-Etapa06-Backup' `
  -BackupRoot 'D:\Dtudo2026-Backups' `
  -RepositoryRoot (Get-Location).Path `
  -StartTime '02:00'
```

## Restauracao isolada

O modo de restauracao verifica o hash do manifesto, copia e re-hash os arquivos, restaura cada banco com nome temporario diferente da origem, usa `MOVE` para arquivos dentro de uma pasta de verificacao e executa `DBCC CHECKDB`. Por padrao, somente o banco temporario e removido ao final; os arquivos e o relatorio ficam na pasta de verificacao.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Invoke-DtudoBackup.ps1 `
  -Mode RestoreVerify `
  -BackupRoot 'D:\Dtudo2026-Backups\yyyyMMdd' `
  -RestoreRoot 'D:\Dtudo2026-RestoreVerification' `
  -SqlServer '(localdb)\MSSQLLocalDB'
```

O runner recusa restauracao sobre a origem, recusa uma pasta de restauracao dentro do backup e nao executa `DROP DATABASE` no banco original. A limpeza existente no script atinge somente o nome temporario criado pela propria restauracao.

## Evidencias executadas em 2026-08-06 UTC

| Verificacao | Resultado |
| --- | --- |
| Backup SQL `Dtudo2026Db` | `COPY_ONLY` e `CHECKSUM` concluidos; `RESTORE VERIFYONLY` passou |
| Itens do snapshot | 23 itens, sendo 1 backup SQL e 22 arquivos/configuracoes/material |
| Duracao do backup | 1.357 s |
| Restauracao isolada | 1 banco temporario, 22 itens verificados por hash |
| Integridade restaurada | `DBCC CHECKDB passed` |
| Duracao da restauracao | 3.461 s |
| Banco original depois do teste | `Dtudo2026Db` permaneceu `ONLINE`, com 3 tabelas; nenhum banco `RestoreCheck` permaneceu |
| Retencao | Snapshot sintetico de 2020 removido com janela de 30 dias; snapshot atual preservado |
| Idempotencia | Duas execucoes no mesmo dia mantiveram um unico diretorio `yyyyMMdd` |
| Evidencia sem segredo | `SecretsLogged=False`; manifesto e relatorio possuem hashes proprios |
| Agendamento | Tarefa `Dtudo2026-Etapa06-Backup` em estado `Ready`; proxima execucao observada em 2026-08-07 02:00 |

Os relatorios de runtime foram mantidos no volume de backup/restauracao local. O status do projeto registra somente os resultados resumidos, sem copiar caminhos sensiveis, conteudo ou saida SQL completa.

## RPO, RTO e retencao

- O agendamento diario suporta RPO de 24 horas quando a tarefa conclui em cada janela.
- O tempo medido de backup foi inferior a dois segundos e a restauracao isolada foi inferior a quatro segundos neste banco de desenvolvimento; isso atende a meta de RTO de oito horas neste ensaio, sem prometer o mesmo tempo para producao ou bancos maiores.
- A retencao remove somente diretorios diarios com `manifest.json` e anteriores a 30 dias de calendario. A limpeza nao toca em diretorios sem manifesto.
- O snapshot do dia e publicado somente apos todos os hashes e verificacoes concluirem, evitando anunciar uma copia parcial como valida.

## Riscos e acoes manuais

- Carga e restauracao foram executadas no mesmo Windows Server. Os volumes `C:` e `D:` estao em discos fisicos diferentes neste host, mas isso nao protege contra roubo, incendio, ransomware, falha administrativa ou perda total do servidor.
- Ainda nao existe no workspace uma implementacao de `ApiIdentity`, Data Protection, OpenIddict ou certificados de producao. Nao ha chave/certificado novo para incluir automaticamente; quando existirem, devem entrar por `-RecoveryMaterialPath` em raiz protegida e ter backup/rotacao testados.
- A tarefa atual usa identidade interativa de desenvolvimento. A conta dedicada, ACL minima, BitLocker e permissao do processo SQL devem ser definidos na Etapa 07.
- Falha de disco, falta de espaco, erro SQL ou fonte obrigatoria ausente faz o runner falhar e remove somente o staging incompleto. O snapshot diario anterior nao e substituido antes da nova copia estar pronta.
- Prioridade futura: copiar backups de forma criptografada para local externo/offline ou NAS protegido e repetir a restauracao semestral fora do host de origem.

## Rollback

- O codigo pode ser revertido removendo os dois scripts e este runbook, sem migration ou alteracao no banco ativo.
- A tarefa pode ser removida somente como acao operacional explicita com `Unregister-ScheduledTask -TaskName 'Dtudo2026-Etapa06-Backup'`.
- Nao remover snapshots validos durante rollback. Preserve-os para restauracao e apague somente uma pasta de verificacao isolada apos confirmar a evidencia.

## Proxima etapa

A Etapa 06 foi executada e documentada. A proxima etapa permitida e a Etapa 07, mas ela nao foi iniciada neste chat.
