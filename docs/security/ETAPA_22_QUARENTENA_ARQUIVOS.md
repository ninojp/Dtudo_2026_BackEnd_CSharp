# Etapa 22 - Quarentena e ciclo de vida dos arquivos

## Estado

**Concluida no Development local.** A dependencia da Etapa 21 esta concluida, os controles de quarentena e ciclo de vida foram validados localmente e a Etapa 23 nao foi iniciada.

## Implementacao

- `FileStorageOptions` agora possui limites de tamanho, nome, espaco minimo, chave de idempotencia e timeout de scanner.
- A inicializacao falha fechada para raizes ou allowlist vazias, limites invalidos, tamanhos incompatíveis com AMSI e scanner nao configurado.
- A allowlist de importacao exige extensao, MIME normalizado e magic bytes configurados no servidor. O destino logico precisa ter a mesma extensao; nome de arquivo com caminho, controle ou tamanho acima do limite e recusado.
- O corpo e lido em streaming para quarentena, com limite de tamanho, contagem exata, SHA-256 em hexadecimal, `WriteThrough` e `Flush` em disco. O espaco livre e verificado antes do staging, incluindo a reserva minima configurada.
- Cada chave `Idempotency-Key` e validada, derivada para SHA-256 e registrada sem guardar o segredo bruto. O diario JSON usa estados `staging`, `awaiting-scan`, `scanning`, `ready-to-promote`, `promoting`, `awaiting-promotion`, `completed` e `rejected`. Semaforo local e lock de arquivo exclusivo cobrem concorrencia no processo e entre processos no mesmo volume.
- O staging fica em `.dtudo-quarantine/operations/<hash>/payload.bin`. O caminho e criado e validado dentro da raiz permitida; os diretórios internos sao reservados e nao podem ser acessados por `ObjectId` publico.
- A promocao ocorre somente depois de Defender e AMSI retornarem limpo. O hash e o tamanho do payload sao conferidos novamente imediatamente antes do `File.Move` atomico, e destino existente nunca e sobrescrito.
- `CompositeFileScanner` usa `MpCmdRun.exe` sem shell, argumentos estruturados e timeout, e a API nativa AMSI. Somente os codigos `0` (limpo) e `2` (ameaca) do Defender sao aceitos; executavel ausente, timeout, erro de processo, codigo desconhecido, HRESULT AMSI ou configuracao sem scanner obrigatorio falham fechado. Resultado diferente de limpo nunca promove.
- A exclusao move o arquivo para `.dtudo-trash/operations/<hash>/payload.bin` e grava `PurgeAtUtc` em exatamente sete dias. A reconciliacao remove somente payload expirado e conserva um tombstone do diario para impedir reutilizacao ambigua da chave.
- A reconciliacao e executada uma vez no startup e tambem por `POST /api/file-storage/reconcile`. Ela retoma journals parciais, revalida o payload, rescaneia antes de promover, reconhece uma promocao ja concluida por hash e deixa a operacao em quarentena quando o scanner nao esta disponivel.

## Endpoints internos

- `POST /api/file-storage/import`: multipart com `objectId`, `file` e header `Idempotency-Key`.
- `POST /api/file-storage/delete`: recebe somente `ObjectId` e exige `Idempotency-Key`.
- `POST /api/file-storage/reconcile`: executa a reconstrucao autorizada dos diarios.

Todos os endpoints continuam sob a policy `permission:filesystem.command`. Respostas e problemas nao devolvem raiz fisica, caminho temporario ou caminho canonico.

## Validacao

- `dotnet test .\tests\ApiFileStorage.Tests\ApiFileStorage.Tests.csproj --no-restore --filter FullyQualifiedName~FileStorageLifecycleTests`: **9/9**.
- `dotnet test .\tests\ApiFileStorage.Tests\ApiFileStorage.Tests.csproj --no-restore`: **31/31**, incluindo os 29 testes da fundacao da Etapa 21.
- `dotnet build .\ApiFileStorage\ApiFileStorage.csproj --no-restore`: aprovado.
- `get_errors` nos arquivos alterados: nenhum erro. O build exibiu somente os dois avisos CA1416 ja existentes nos testes de ACL.
- O teste de malware usa somente um marcador sinteticamente seguro e um scanner falso; nenhum malware real foi criado ou executado. O teste prova que um resultado de ameaca fica na quarentena e nao chega ao destino.
- Os testes cobrem magic bytes/MIME falso, arquivo acima do limite, falta de espaco, scanner indisponivel e veredito desconhecido, concorrencia da chave, reutilizacao indevida de idempotency key, falha parcial de promocao, hash final, lixeira de sete dias e purge por reconciliacao.

## Riscos residuais e acoes manuais

- A suite nao invoca o Defender/AMSI real para nao depender de deteccao do host durante o build. Antes de homologacao, e necessario configurar o caminho/conta do processo e executar um arquivo limpo de teste com Defender e AMSI reais, sem usar malware real.
- As raizes de Development permanecem vazias por configuracao; a homologacao deve definir raizes locais absolutas, ACL minima, allowlist revisada e reserva de espaco.
- O lock entre processos pressupoe que as operacoes compartilham o mesmo volume local autorizado; distribuicao entre hosts exige um coordenador persistente antes da promocao.

## Rollback

Parar a `ApiFileStorage`, preservar os diarios e payloads de quarentena/lixeira para revisao operacional e reverter os arquivos de codigo, configuracao, testes e documento desta etapa. Nao ha migration ou banco alterado. Nao apagar as raizes operacionais automaticamente; qualquer limpeza de payload deve respeitar o diario e a janela de sete dias.

## Proxima etapa

A Etapa 23 e a proxima etapa permitida, deve ser executada em chat separado e permanece nao iniciada.
