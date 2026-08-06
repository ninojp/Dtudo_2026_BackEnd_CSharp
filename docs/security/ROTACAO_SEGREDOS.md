# Lista de Rotacao de Segredos

## Politica aplicada na Etapa 02

Nenhum segredo de ambiente ou de producao e armazenado em `appsettings*.json`, no codigo-fonte ou em artefatos versionados. Fixtures sinteticos de testes permanecem somente nos testes. O escopo atual e Development; homologacao e producao serao configuradas somente quando esses ambientes entrarem no ciclo de validacao. Nenhum valor real e registrado neste documento.

## Tipos e procedimentos

| Tipo | Chave ou material | Fonte por ambiente | Rotacao |
| --- | --- | --- | --- |
| Client ID da API MyAnimeList | `MyAnimeList:ClientId` | Development: .NET User Secrets; homologacao/producao: configurar futuramente no provedor seguro do ambiente | Revogar no MyAnimeList, emitir novo Client ID, atualizar a fonte do ambiente e reiniciar o servico quando o ambiente estiver em uso; fazer imediatamente sob suspeita |
| Conexao do banco local | `ConnectionStrings:LocalDbConnection` | Development: .NET User Secrets; homologacao/producao: configurar futuramente na fonte protegida do host | Trocar a conta de servico ou senha no banco e atualizar a fonte do ambiente quando o ambiente estiver em uso; validar conectividade sem registrar a connection string |
| Hashes de senha da autenticacao legada | `App_Data/auth-users*.json` | Arquivo local ignorado pelo Git, com ACL restrita ao servico | Recriar ou redefinir as contas afetadas; apagar o arquivo antigo com procedimento operacional; a migracao para identidade esta prevista em etapa posterior |

## Evidencia da varredura

- Conteudo versionado: os candidatos nao vazios encontrados estao limitados a fixtures sinteticos de testes, documentacao e nomes de propriedades; nenhum segredo de ambiente ou de producao foi identificado.
- Historico Git: 30 literais candidatos foram classificados como fixtures de teste/exemplo; nenhum literal fora de testes foi encontrado na varredura redigida do escopo ativo.
- Foram encontrados valores de connection string de LocalDB em configuracoes e artefatos versionados. Eles nao continham senha, mas foram removidos para que a configuracao de banco venha de fonte externa ao repositorio.
- `App_Data/*.json`, arquivos `.env` e materiais de certificado nao estao versionados no escopo atual.
- `ApiNode` foi mantida fora da etapa conforme o plano.

## Acoes futuras, fora do escopo atual

1. Development local: concluido; `MyAnimeList:ClientId` e `ConnectionStrings:LocalDbConnection` estao no User Secrets dos projetos corretos.
2. Quando homologacao for iniciada: configurar `MyAnimeList__ClientId` e `ConnectionStrings__LocalDbConnection` no provedor seguro do ambiente, sem usar `appsettings.Staging.json` versionado.
3. Quando producao for autorizada: configurar as mesmas chaves no provedor seguro do host; usar Windows Authentication e a conta de servico autorizada para o SQL.
4. Antes da primeira promocao: confirmar no provedor MyAnimeList se existe algum Client ID antigo fora do repositorio e revoga-lo caso a origem nao seja conhecida.
5. Antes da primeira promocao: restringir a leitura das fontes de segredo as contas dos servicos e registrar a rotacao operacionalmente, sem incluir valores em logs ou tickets.
