# Etapa 26 - Resiliencia da ApiMyAnimeList

## Estado

- Estado: Concluida no Development local.
- Escopo executado: timeout, retry seguro com jitter, circuit breaker, 429/5xx/504, cancelamento, correlacao e egress/SSRF.
- Nova API externa: nenhuma.
- A Etapa 27 nao foi iniciada.

## Implementacao

- `MyAnimeListClient` deixou de manter um loop de retry proprio. A chamada continua sendo somente `GET` e o cliente apenas valida a resposta, desserializa o JSON e preserva o cache existente.
- `Microsoft.Extensions.Http.Resilience` 10.0.0 fornece a pipeline oficial com timeout, retry exponencial com jitter e circuit breaker.
- Retry e circuit breaker tratam somente metodos idempotentes (`GET`, `HEAD`, `OPTIONS`, `PUT` e `DELETE`) e falhas transitorias: `408`, `429`, `5xx`, falhas de transporte e timeout da propria pipeline. `Retry-After` e respeitado com limite de oito segundos.
- `MaxRetries = 0` omite a estrategia de retry, evitando uma configuracao Polly invalida e mantendo o circuito/timeout ativos.
- O timeout total (`TotalTimeoutSeconds`) e o timeout por tentativa (`TimeoutSeconds`) ficam na pipeline; `HttpClient.Timeout` fica infinito para evitar dois timeouts concorrentes. Cancelamento recebido do chamador e propagado sem retry.
- `BrokenCircuitException` retorna `503`; timeout interno ou demora upstream retorna `504`; cancelamento do chamador nao e convertido em erro HTTP.
- `CorrelationIdDelegatingHandler` permanece na cadeia HTTP e foi validado em todas as tentativas sinteticas.

## Egress e SSRF

- `BaseUrl` exige HTTPS na porta 443, host listado, caminho `/v2/`, ausencia de credenciais na URL e configuracao validada no startup.
- `AllowedHosts` e `AllowedPathPrefix` formam uma allowlist explicita. Hosts, caminhos fora da allowlist, redirects, userinfo e destinos privados sao rejeitados.
- `SocketsHttpHandler` nao usa proxy e nao segue redirects. O `ConnectCallback` resolve o host permitido, recusa loopback, redes privadas, link-local, multicast e blocos reservados, e conecta somente a endereco publicamente roteavel.
- A excecao de egress nao e considerada falha transitoria, portanto nao gera retry nem alimenta o circuito.

## Evidencias

- `dotnet test .\tests\ApiMyAnimeList.Tests\ApiMyAnimeList.Tests.csproj --no-restore --filter "FullyQualifiedName~MyAnimeListResilienceTests"`: 8/8 aprovados.
- `runTests` nos cinco arquivos da suite: 21/21 aprovados, 0 falhas, 0 ignorados.
- As simulacoes locais cobrem `429` seguido de `504` e sucesso, retry somente para `GET`, circuito aberto e recuperado apos o break duration, timeout interno, cancelamento do chamador, correlacao e rejeicao de host/caminho fora da allowlist.
- Nenhuma chamada foi feita para a API MyAnimeList real; nenhuma credencial ou resposta externa foi registrada.

## Riscos residuais e operacao

- O circuito e mantido em memoria por processo. Nao ha estado distribuido entre replicas, o que deve ser decidido somente quando houver hospedagem com mais de um processo.
- Homologacao ainda precisa comprovar DNS, firewall de saida, certificado TLS, limites reais da MAL, respostas `Retry-After` e comportamento de `429/5xx/504` com dados de ambiente controlado.
- Os parametros de circuito, timeout e retry devem ser ajustados com metricas do Seq antes de uma eventual promocao; a configuracao atual e Development.

## Rollback

- Parar a `ApiMyAnimeList`, restaurar os arquivos de codigo/configuracao e remover a referencia de pacote da Etapa 26.
- Restaurar o registro anterior de `MyAnimeListOptions`, pipeline HTTP e tratamento do controller sem executar migration ou apagar cache/banco.
- Reexecutar a suite da API antes de religar o servico. Nenhuma raiz de arquivo, banco ou segredo e alterada pelo rollback.

## Acoes manuais antes da homologacao

- Confirmar a allowlist de DNS/egress e a resolucao publica de `api.myanimelist.net` no host escolhido.
- Executar um proxy de teste autorizado ou handler de homologacao para simular `429`, `504`, timeout, recuperacao e circuit breaker, sem usar segredo real em logs.
- Observar correlacao, contagem de tentativas, `503/504`, latencia e abertura/fechamento do circuito no Seq.
