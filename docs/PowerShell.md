# Ambiente PowerShell

## Padrao adotado

PowerShell 7.6.6 (Core), distribuicao estavel oficial da Microsoft, instalado sem
administrador em `%LOCALAPPDATA%\Programs\PowerShell\7.6.6\pwsh.exe`.
O SHA-256 do ZIP foi comparado com o manifesto oficial e a assinatura Authenticode
do executavel foi validada como Microsoft Corporation antes da execucao.

Release: <https://github.com/PowerShell/PowerShell/releases/tag/v7.6.6>

A pasta foi adicionada ao PATH do usuario. Nas configuracoes de usuario do VS Code:

- Perfil padrao de terminal: `PowerShell 7`.
- Perfil de automacao: mesmo executavel, com `-NoLogo -NoProfile`.
- Extensao PowerShell: executavel adicional e versao padrao `PowerShell 7`.
- Preferencias nao relacionadas foram preservadas.

Nenhuma politica de execucao ou seguranca foi alterada. O Windows PowerShell 5.1
permanece instalado para compatibilidade. `powershell.exe` continua significando
5.1; `pwsh.exe` e o executavel do 7. Nao renomear nem substituir executaveis do Windows.

Essa instalacao por ZIP nao recebe atualizacoes automaticamente. Uma atualizacao
futura deve usar uma release estavel oficial, verificar hash e assinatura e atualizar
os caminhos dos perfis e do PATH somente apos validacao.

## Ativar no editor

Terminais existentes nao trocam de processo quando o perfil padrao muda. Fechar os
terminais antigos e abrir um novo terminal `PowerShell 7`. Para renovar tambem o PATH
herdado e a sessao da extensao, fechar todas as janelas do VS Code e reabrir o editor.
O Visual Studio 2026 nao precisa ficar aberto durante esse procedimento; seu backend
continua sendo executado por ele normalmente.

```powershell
$PSVersionTable.PSVersion
$PSVersionTable.PSEdition
Get-Command pwsh
```

Esperado: versao `7.6.6`, edicao `Core` e caminho da instalacao do usuario.
O perfil do terminal do VS Code nao altera automaticamente o terminal do Visual
Studio, Windows Terminal, atalhos do sistema ou tarefas agendadas ja existentes.

A ferramenta de terminal do chat atual usa Windows PowerShell 5.1 por contrato.
Essa configuracao nao e substituida pelas preferencias do editor. Para scripts do
projeto, executar explicitamente o PowerShell 7, sem depender do shell hospedeiro:

```powershell
& "$env:LOCALAPPDATA\Programs\PowerShell\7.6.6\pwsh.exe" -NoLogo -NoProfile -File .\scripts\Test-DtudoPowerShell.ps1
```

## Correcoes e verificacao

- A rotina de associacoes usa chaves SQL reconhecidas pelo SqlConnectionStringBuilder
  e conversao explicita para `int[]` ao ler arrays JSON do banco. Esses eram erros de
  compatibilidade do script, nao defeitos no banco nem evidencia de Windows corrompido.
- Registro de tarefa de backup e validacao MSIX agora selecionam o executavel do host
  atual, em vez de presumir `powershell.exe` dentro de `$PSHOME` ou recair no 5.1.
- A validacao MSIX aceita separadores ZIP `/` e pacotes antigos com `\`.
- Textos JSON externos em UTF-8 devem ser lidos com codificacao explicita no 5.1.
  Downloads binarios com BOM exigem leitura com deteccao de codificacao, nao cast
  implicito para string. Nao desabilitar verificacoes de hash para contornar esse caso.

```powershell
pwsh -NoLogo -NoProfile -File .\scripts\Test-DtudoPowerShell.ps1
pwsh -NoLogo -NoProfile -File .\scripts\Test-DtudoEtapa29.ps1
```

Verificacao realizada em 7.6.6 e 5.1:

- Sintaxe dos 10 scripts PowerShell verificada sem executa-los.
- SQL builder sem conexao, arrays JSON vazios/unitarios/multiplos e UTF-8 aprovados.
- 11 testes sinteticos de associacoes aprovados em cada versao.
- 10 testes MSIX com pacotes temporarios e planos sem instalacao aprovados em cada versao.
- Registro do backup validado no PowerShell 7 somente com `-WhatIf`; nenhuma tarefa
  foi criada, substituida ou executada.

Nao foram executados backups reais, deploy, hardening ou alteracoes administrativas
para validar a migracao. Analise sintatica nao comprova que todo modulo ou recurso
externo de todas as rotinas funciona. Problemas futuros devem ser reproduzidos e
testados no host correto, sem prometer ausencia universal de erros.

Os HDs das colecoes e o banco nao foram alterados durante esta configuracao.
