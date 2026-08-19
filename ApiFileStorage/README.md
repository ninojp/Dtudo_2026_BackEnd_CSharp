# ApiFileStorage

Serviço interno responsável por importar, validar, promover e excluir arquivos apenas dentro de raízes autorizadas.

## Exportação de MyAnime

O WinApp consulta `GET /api/file-storage/export/destinations` e apresenta os destinos configurados ao operador. A escolha envia somente o `DestinationId`; caminhos físicos, UNC ou relativos livres não fazem parte do contrato.

Cada destino combina uma raiz autorizada com um prefixo controlado pelo servidor:

```json
{
  "FileStorage": {
    "ExportDestinations": [
      {
        "Id": "my-animes",
        "DisplayName": "Pasta MyAnimes da ApiFileStorage",
        "RootId": "media",
        "PathPrefix": "my-animes"
      }
    ]
  }
}
```

A estrutura resultante é:

```text
<raiz autorizada>\<prefixo>\<título do MyAnime>\
└── <ano> <título do anime> - <tipo>\
    └── <MalId>.jpg
```

Os nomes são sanitizados no servidor, nomes reservados do Windows são recusados ou ajustados, colisões recebem sufixo e todos os destinos continuam sujeitos à validação canônica, quarentena, Defender/AMSI e promoção da ApiFileStorage.

## Configurar uma raiz local

A pasta física deve existir antes da inicialização da API. Em Development, configure-a no User Secrets sem versionar o caminho da máquina:

```powershell
New-Item -ItemType Directory -Force "D:\Dtudo\Media"
dotnet user-secrets set "FileStorage:Roots:0:Id" "media" --project .\ApiFileStorage\ApiFileStorage.csproj
dotnet user-secrets set "FileStorage:Roots:0:Path" "D:\Dtudo\Media" --project .\ApiFileStorage\ApiFileStorage.csproj
```

Para oferecer outra pasta na janela de seleção, cadastre uma segunda raiz e um segundo destino:

```powershell
New-Item -ItemType Directory -Force "E:\Animes"
dotnet user-secrets set "FileStorage:Roots:1:Id" "animes-e" --project .\ApiFileStorage\ApiFileStorage.csproj
dotnet user-secrets set "FileStorage:Roots:1:Path" "E:\Animes" --project .\ApiFileStorage\ApiFileStorage.csproj
dotnet user-secrets set "FileStorage:ExportDestinations:1:Id" "animes-e" --project .\ApiFileStorage\ApiFileStorage.csproj
dotnet user-secrets set "FileStorage:ExportDestinations:1:DisplayName" "Animes no disco E" --project .\ApiFileStorage\ApiFileStorage.csproj
dotnet user-secrets set "FileStorage:ExportDestinations:1:RootId" "animes-e" --project .\ApiFileStorage\ApiFileStorage.csproj
dotnet user-secrets set "FileStorage:ExportDestinations:1:PathPrefix" "my-animes" --project .\ApiFileStorage\ApiFileStorage.csproj
```

Reinicie a ApiFileStorage depois de alterar raízes ou destinos. A conta do processo da API precisa de ACL na raiz; o WinApp não precisa de permissão direta nela.
