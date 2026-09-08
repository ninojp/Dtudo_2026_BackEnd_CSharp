# ApiFileStorage

Serviço interno responsável por validar, promover e excluir arquivos apenas dentro de raízes autorizadas, com quarentena, verificação de magic bytes/MIME, scanner Defender/AMSI e reconciliação de diários.

## Superfície atual (2026-09-07)

Os endpoints de importação (`import`), destinos/plano de exportação (`export/destinations`, `export/plan`) e exclusão (`delete`, `delete/preview`, `delete/batch`) foram removidos: o WinApp não envia mais arquivos pela API — o salvamento de estruturas de MyAnime é feito diretamente no disco local, na pasta escolhida pelo operador via diálogo nativo do Windows, sem passar pela ApiFileStorage.

Permanecem expostos somente:

- `POST /api/file-storage/resolve`: metadados lógicos de um `ObjectId`, sem devolver caminho físico.
- `POST /api/file-storage/reconcile`: retomada de diários de quarentena/lixeira e purge autorizado (também executado automaticamente no startup).
- `GET /api/file-storage/health` e `GET /api/file-storage/startup`: sondas operacionais usadas pelo painel de saúde e pela inicialização do WinApp.

O motor interno de quarentena/scanner/promoção/lixeira (`FileStorageLifecycleService`) permanece implementado e coberto por testes, pronto para um futuro fluxo de ingestão, mas hoje não é alcançável por endpoint de escrita.

## Configurar uma raiz local

A pasta física deve existir antes da inicialização da API. Em Development, configure-a no User Secrets sem versionar o caminho da máquina:

```powershell
New-Item -ItemType Directory -Force "D:\Dtudo\Media"
dotnet user-secrets set "FileStorage:Roots:0:Id" "media" --project .\ApiFileStorage\ApiFileStorage.csproj
dotnet user-secrets set "FileStorage:Roots:0:Path" "D:\Dtudo\Media" --project .\ApiFileStorage\ApiFileStorage.csproj
```

Reinicie a ApiFileStorage depois de alterar raízes. A conta do processo da API precisa de ACL na raiz; o WinApp não precisa de permissão direta nela.
