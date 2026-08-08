# Etapa 28 - Painel de saude e alertas

## Estado

- Estado: Concluida no Development local.
- A Etapa 27 continua bloqueada por homologacao externa; nenhum resultado de IIS, TLS real, firewall ou dominio foi afirmado.
- A Etapa 29 nao foi iniciada.

## Implementacao

- `WinAppHealthMonitoringService` consulta com bearer e timeout independente:
  - `ApiIdentity/health/ready`;
  - `ApiMyAnimes/apiLocal/Health`;
  - `ApiMyAnimeList/ApiMyAnimeList/health`;
  - `ApiFileStorage/api/file-storage/health`.
- `health.read` foi incluido nos scopes do cliente WinApp e no seeder/configuracao do `ApiIdentity`. O health check anterior de `ApiMyAnimes` passou a reutilizar a sessao autenticada quando fornecida.
- `FileStorageHealthService` expõe somente estado operacional, bytes de `DriveInfo`, reserva minima, estado de scanner e contagens de journals da quarentena/lixeira. A rota exige `permission:health.read` e scope `health.read` e nao devolve caminhos fisicos.
- O painel `Frm_HealthDashboard` usa `ThemeManager`, `DarkModeColors`, `DataGridView` somente leitura, layout ancorado/dock e `AutoScaleMode.Dpi`. Exibe estados `Operacional`, `Aviso`, `Critico` e `Indisponivel`.
- O formulario principal habilita o painel somente apos autenticacao, atualiza sob demanda e por timer de 60 segundos, e mantem cada fonte independente. `WindowsHealthNotificationService` usa `NotifyIcon`, deduplica a assinatura dos estados criticos e nao inclui detalhes sensiveis na mensagem.
- O backup local usa `DTUDO_BACKUP_ROOT` ou configuracao externa, verifica manifesto, hash SHA-256, tipo e idade de 24 horas. Raiz ausente ou sem permissao e mostrada como indisponivel.

## Validacoes

- `dotnet test .\tests\WinAppDtudo.Tests\WinAppDtudo.Tests.csproj --no-restore`: `23/23` aprovados.
- `dotnet test .\tests\ApiFileStorage.Tests\ApiFileStorage.Tests.csproj --no-restore`: `36/36` aprovados.
- Os testes focados cobrem bearer em todas as consultas, timeout sem bloquear outras fontes, manifesto quebrado, sessao prestes a expirar, pouco espaco como estado critico, notificacao somente para estado critico, `401`/`403`, ausencia de caminho fisico e construcao/redimensionamento do painel com `AutoScaleMode.Dpi`.
- `dotnet build .\ApiIdentity\ApiIdentity.csproj --no-restore`: aprovado.
- Avisos mantidos e anteriores ao checkpoint: `NU1510` de `ProtectedData` e conflito de versoes `WindowsBase` associado ao WebView2.

## Riscos e operacao

- Alertas sao locais: nao aparecem se o WinApp ou o host estiverem indisponiveis.
- A verificacao de backup somente funciona quando `DTUDO_BACKUP_ROOT` estiver configurado fora do repositorio e acessivel ao processo do WinApp.
- Certificados reais, scanner/AMSI/Defender real, raiz de storage de homologacao, notificacao visual no Explorer e a integracao de rede dependem do ambiente externo ainda bloqueado pela Etapa 27.

## Rollback

- Parar o WinApp, remover o menu/polling/NotifyIcon e restaurar os arquivos de monitoramento, configuracao, endpoint de health e testes deste checkpoint.
- Nao remover automaticamente banco, raiz de storage, quarentena, lixeira, certificado, token ou segredo.

## Proxima etapa

Desbloquear e concluir a Etapa 27. A Etapa 29 permanece fora deste chat.
