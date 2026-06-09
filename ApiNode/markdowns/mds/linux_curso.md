# Linux gerenciando diretórios, arquivos, permissões e processos

## Aula 01 - Explorando o terminal Linux

### Aula 01 - Apresentação - Vídeo 1

Essa aula é a porta de entrada para o mundo do Linux, te mostrando como ele é importante para quem quer ser desenvolvedor(a) ou especialista em operação de sistemas.

Vamos começar explorando o terminal, aprendendo a navegar entre diretórios, entender a estrutura do sistema e como organizar seus projetos.

Depois, vamos aprender a gerenciar o sistema, atualizando pacotes, instalando novos, analisando processos e até mesmo criando VMs na nuvem!

E o melhor de tudo? Você vai aprender na prática, com exercícios e suporte da comunidade!

### Aula 01 -Preparando o ambiente: terminal Linux diretamente no Windows com WSL

Para nossa jornada de aprendizado no mundo Linux é fundamental que você tenha um ambiente Linux preparado em seu computador. Não será necessário instalar um outro sistema operacional (realizar um dual-boot) ou utilizar um software de virtualização como o VirtualBox para criação de máquinas virtuais com Linux. Vamos adotar uma alternativa simples: utilização do Windows Subsystem for Linux (WSL - Subsistema Linux para Windows).

Com o WSL, você pode instalar distribuições Linux (como Ubuntu, Debian e outras) e utilizá-las como se fossem aplicativos nativos do Windows. Todos os passos e configurações que faremos aqui são compatíveis com o WSL.

Para começar a usar o WSL, siga os passos abaixo:

1 - Abra o PowerShell como administrador e execute o comando wsl --install.

2 - Após a instalação inicial, você pode instalar outras distribuições disponíveis na Microsoft Store. Assim, basta escolher a distribuição Ubuntu (a mesma que iremos utilizar no curso).

3 - Para acessar o WSL, basta procurar pela distribuição instalada no menu iniciar (pesquise, por exemplo, "Ubuntu"). Com alguns poucos passos, você terá um terminal Linux pronto para dar sequência aqui no curso!

É bem simples mesmo! Depois de seguir estes passos, basta reiniciar o seu computador para que o WSL fique prontinho para iniciarmos nossa jornada.

Caso tenha alguma dúvida em relação ao processo de configuração e funcionamento do WSL, recomendamos a leitura do artigo [“WSL: como executar programas e comandos Linux no Windows?”](https://www.alura.com.br/artigos/wsl-executar-programas-comandos-linux-no-windows).

Além disso, você também pode consultar diretamente o guia oficial da Microsoft com os passos recomendados para a instalação do WSL no Windows: [“Como instalar o Linux no Windows com o WSL”](https://learn.microsoft.com/pt-br/windows/wsl/install).

### Aula 01 - Usando o Terminal - Vídeo 2

Olá! Essa aula te introduz ao terminal Linux, um ambiente essencial para quem trabalha com desenvolvimento web. Você aprendeu a usar o comando git clone para importar projetos do GitHub para o seu ambiente local, e como navegar por diretórios usando os comandos ls e cd.

A aula também te ensinou a interpretar o prompt do terminal, que mostra informações importantes como o nome do usuário, o nome do dispositivo e a localização atual.

No próximo passo, vamos explorar mais a fundo a navegação no sistema Linux, para que você se sinta mais confortável e confiante nesse ambiente.

### Aula 01 - Navegando entre diretórios - Vídeo 3

Essa aula foi incrível, né? Nela, aprendemos como navegar no terminal Linux, um ambiente essencial para quem trabalha com essa plataforma.

Começamos aprendendo a usar o comando pwd para verificar em qual diretório estamos. Depois, exploramos como acessar a raiz do sistema usando cd /, que nos leva para o ponto de partida do sistema operacional.

Vimos também que o Linux é um kernel, ou seja, o núcleo do sistema e que Ubuntu, Debian, Red Hat Enterprise Linux (RHEL), Kali Linux, Fedora, Arch Linux, Linux Mint, openSUSE, Gentoo, BRLix, CCux Linux, Chrome OS... São distribuições linux que usam este kernel.

### Aula 01 - Para saber mais: compreendendo o sistema de arquivos do Linux (FHS)

A estrutura de diretórios da raiz (/) no Ubuntu, distribuição que adotamos em nossa jornada de aprendizado, observa o padrão conhecido como Filesystem Hierarchy Standard (Padrão para Sistema de Arquivos Hierárquico - FHS), que define os principais diretórios e o seu conteúdo em sistemas baseados no Linux.

Vamos descrever sinteticamente os principais diretórios que comumente encontramos na raiz do Ubuntu com suas respectivas finalidades de uso. A raiz do sistema de arquivos é o “ponto de partida”, assim todos os demais diretórios e arquivos ficam localizados abaixo desse diretório.

/bin → armazenamento de arquivos binários essenciais do sistema.

/boot → armazenamento de arquivos necessários para a inicialização do sistema, incluindo o carregador de inicialização (boot loader) e o kernel do Linux.

/dev → armazenamento de arquivos de dispositivo (device files) que representam dispositivos de hardware, como discos rígidos, terminais e outros periféricos.

/etc → armazenamento de arquivos de configuração do sistema.

/home → armazenamento de diretórios pessoais dos usuários.

/lib → armazenamento de bibliotecas compartilhadas essenciais para binários localizados nos diretórios /bin e \sbin.

/media → ponto de montagem para mídias removíveis (drivers USB, por exemplo).

/mnt → ponto de montagem temporária para sistemas de arquivos. Usado para montar temporariamente outros sistemas de arquivos durante a administração do sistema.

/opt → armazenamento de aplicativos opcionais e pacotes de software adicionais que não fazem parte da instalação padrão do sistema.

/proc → sistema de arquivos virtual que armazena informações sobre os processos em execução e o estado do kernel.

/root → diretório pessoal do usuário root (superusuário).

/run → armazenamento de informações voláteis sobre o sistema desde a última inicialização, como PID files e sockets.

/sbin → armazenamento de binários essenciais para a administração do sistema, necessários para o boot e recuperação do sistema.

/srv → armazenamento de dados específicos de serviços fornecidos pelo sistema.

/sys → sistema de arquivos virtual que fornece informações e interfaces para o kernel do Linux.

/tmp → armazenamento de arquivos temporários criados por aplicativos e pelo sistema. Esses arquivos geralmente são excluídos ao reiniciar o sistema.

/usr → armazenamento de dados de usuário mais instalados pelo sistema, incluindo binários adicionais, bibliotecas e arquivos de cabeçalho.

/var → armazenamento de arquivos variáveis, como logs, filas de email e arquivos de spool.

Cada diretório presente na raiz do sistema possui um propósito bem definido, atuando para manter o sistema operacional de modo eficiente. A compreensão detalhada de seu funcionamento e configuração está fora do escopo do nosso curso, mas, caso queira entender mais sobre o tema, recomendamos algumas referências ao final do curso.

### Aula 01 - Usando o Sudo - Vídeo 4

Nessa aula, aprendemos como usar o comando sudo para escalar privilégios e acessar áreas restritas do sistema, como o diretório /root.

Vimos que o sudo é essencial para executar comandos que exigem permissões administrativas, mas devemos ter cuidado ao usá-lo, pois podemos causar danos ao sistema se não soubermos o que estamos fazendo. ️

Também aprendemos como iniciar uma sessão como superusuário com o comando sudo -i, o que nos permite acessar e modificar qualquer parte do sistema.

Por fim, exploramos o arquivo sudoers, que define as permissões para que os usuários possam executar ações administrativas.

Lembre-se de sempre sair do modo superusuário após concluir suas tarefas usando o comando exit para evitar problemas no sistema.

Agora, vamos continuar explorando o Linux e aprender como criar diretórios e arquivos!

### Aula 01 - Para saber mais: dicionário de comandos

Ao longo desta aula, tivemos a oportunidade de explorar vários comandos e dar os primeiros passos no mundo do terminal Linux. Para relembrar tudo o que vimos de forma sintética, preparamos um pequeno “dicionário de comandos” com a descrição de todos os comandos que aprendemos aqui.

- pwd (Print Working Directory): Exibe o caminho completo do diretório atual em que você está no terminal.

- ls (List): Lista os arquivos e diretórios no diretório atual. Com a opção -a, exibe também arquivos ocultos.

- cd (Change Directory): Altera o diretório atual para o especificado. Exemplo cd /projeto muda para o diretório projeto.

- sudo (SuperUser Do): Permite a execução de comandos com privilégios de superusuário (root). Exemplo sudo ls /root exibe o conteúdo do diretório root com permissões elevadas.

- sudo -i: Inicia uma sessão de shell interativa como usuário root, permitindo executar comandos com permissões administrativas sem precisar prefixar cada comando com sudo.

- sudo su: Abre uma sessão de shell como usuário root, mantendo o ambiente do usuário atual. Semelhante a sudo -i, mas mantém o ambiente do usuário.

- cat (Concatenate): Exibe o conteúdo de arquivos. Pode também ser usado para concatenar e criar arquivos.

- exit: Encerra a sessão atual de shell, seja ela como usuário normal ou root.

- git clone: Cria uma cópia local de um repositório Git remoto. Exemplo: git clone https://github.com/alura-cursos/adopet-frontend-cypress clona o repositório especificado.

- clear - Limpa o terminal

- man - Exibe o manual do comando digitado a seguir (ex: man sudo)

### Aula 01 - Faça como eu fiz: acessando o Shell como Root

No Linux, o usuário root é o superusuário com permissões administrativas completas em todo o sistema. Dessa forma, o root possui a capacidade de alterar qualquer configuração do sistema, instalar ou remover aplicações e acessar todos os arquivos e diretórios.

Por outro lado, um termo bastante comum nesse mundo Linux é shell. Utilizamos comumente o termo shell para designar a interface de linha de comando que permite nossa interação direta com o sistema operacional. O shell é quem interpreta os comandos que digitamos, executando as operações correspondentes.

Chegou a sua vez de replicar o que fizemos durante a aula, acessando o shell do seu ambiente Linux como superusuário (root). Para saber mais detalhes sobre como fazer isso, clique na opinião da pessoa instrutora!

Opinião do instrutor

Basta digitar em seu terminal sudo -i para iniciar uma sessão interativa como root. Assim, você terá um ambiente de shell completo com as permissões e variáveis de ambiente do root.

Após o comando sudoé comum a solicitação de senha, lembre-se de inserir a senha do usuário atual.

>$ sudo -i  
[sudo] senha para seu usuario:  
root@hostname:~#

Alternativamente, o comando sudo su também inicia um novo shell como root, mas não altera o ambiente do shell para o padrão do root. É uma opção de se tornar root a partir do seu usuário atual.

>$ sudo su  
[sudo] senha para seu usuario:  
root@hostname:~#

Sempre tenha bastante cautela ao operar como root, pois você pode alterar arquivos e configurações críticas do sistema.

Para sair do ambiente de root e retornar ao seu usuário normal, basta utilizar o comando exit.

### Aula 01 - Nesta aula, você aprendeu como:

- Utilizar diretórios em ambientes Linux para organizar arquivos e programas do sistema e de seus usuários;
- Navegar no terminal do Linux utilizando os comandos pwd para identificar o diretório atual, ls para listar arquivos e diretórios de um diretório e cd para mudar de diretório;
- Modificar a execução de comandos com a utilização de opções para realização de tarefas de modo eficiente;
- Utilizar o comando sudo para executar comandos com privilégios de superusuário, obtendo permissões administrativas e acessando diretórios protegidos do sistema.

## Aula 02 - Criando novas estruturas de dados

### Aula 02 - Criando diretórios e arquivos - Vídeo 1

Nessa aula, aprendemos como criar uma estrutura de diretórios para um projeto no Linux.

Primeiro, vimos como criar um novo diretório usando o comando mkdir e como navegar entre os diretórios com o comando cd.

Depois, aprendemos a criar um arquivo vazio com o comando touch e a editar esse arquivo usando o editor de texto nano.

Por fim, vimos como visualizar o conteúdo de um arquivo com o comando cat e como usar as setas do teclado para navegar entre os comandos que já digitamos.

Legal, né?

Agora, você pode praticar criando seus próprios diretórios e arquivos!

Lembre-se de usar os comandos que aprendemos e de explorar as funcionalidades do nano.

### Aula 02 - Para saber mais: compreendendo o mundo Linux

É muito comum nos depararmos com menções ao Linux como um sistema operacional de código aberto. Em certa medida, essa afirmação é verídica, pois o Linux é o núcleo (kernel) de diversas distribuições de sistemas operacionais de código aberto e uso livre.

Um sistema operacional é formado por um conjunto de diferentes programas estruturados em blocos funcionais (utilitários, drivers, ferramentas de gerenciamento, aplicativos, bibliotecas etc.) que operam de modo integrado no gerenciamento de um hardware. Dessa forma, apesar de baseadas no mesmo kernel, podemos ter distribuições bem distintas, concebidas para melhor atendimento de demandas em contextos de uso específicos.

O Android, bem comum em smartphones, é um exemplo de sistema operacional baseado no kernel do Linux, assim como o Ubuntu Server, adotado em servidores, e o Tizen, utilizado em Smart TVs. Se fôssemos listar aqui a grande variedade de distribuições disponíveis, destacando suas principais características e aplicações, precisaríamos de muitas páginas. Daria para construir um livro digital só sobre isso!

Vamos listar algumas distros de uso mais comum:

- Debian: é conhecida como uma distribuição estável e com alta confiabilidade por ser uma das mais antigas. Destaca-se por apresentar suporte a múltiplas arquiteturas de hardware, sendo adotada em uma grande variedade de dispositivos como servidores, desktops e sistemas embarcados.
- Ubuntu: distribuição bastante popular, concebida a partir do Debian, sendo conhecida por oferecer grande facilidade de uso para usuários pouco familiarizados com distribuições do Linux. Possui versões para desktops, servidores e dispositivos IoT.
- Linux Mint: desenvolvida para uso em notebooks e desktops, é reconhecida por sua usabilidade.
- CentOS: amplamente utilizada em servidores e ambientes corporativos devido à sua confiabilidade e compatibilidade com aplicações empresariais.

Quer conhecer mais distribuições do Linux e acompanhar o lançamento de suas novas versões? Visite o site [DistroWatch](https://distrowatch.com/), onde você também encontrará informações sobre o FreeBSD, outro sistema operacional de código aberto que pode ser adotado em diferentes plataformas de hardware.

### Aula 02 - Movendo Arquivos e diretórios - Vídeo 2

Nesta aula, aprendemos como organizar melhor nossos projetos criando subdiretórios e movendo arquivos entre eles.

Primeiro, criamos um subdiretório chamado "ideias" dentro do diretório "projeto_python" usando o comando mkdir ideias.

Depois, aprendemos a mover o arquivo "projeto_ideias.txt" para dentro do subdiretório "ideias" usando o comando mv.

Também exploramos o comando ls -al para visualizar as permissões de arquivos e diretórios, que nos ajudam a entender quem tem acesso a cada item e quais ações podem ser realizadas.

Por fim, aprendemos a mover um diretório chamado "rascunho" para dentro do subdiretório "ideias" usando o comando mv rascunho /home/lcs/projeto_python/ideias.

### Aula 02 - Copiando e renomeando arquivos e diretórios - Vídeo 3

Vimos que para copiar um arquivo, usamos o comando cp seguido do nome do arquivo original e do nome do novo arquivo. Para renomear um arquivo ou diretório, usamos o comando mv seguido do nome original e do novo nome. E para mover um arquivo para outro diretório, usamos o comando mv seguido do nome do arquivo e do caminho completo do novo diretório.

Legal, né?

Agora, vamos praticar um pouco!

Você se lembra de como copiar um arquivo? Tente fazer um exercício: copie o arquivo projeto_ideias.txt para um novo arquivo chamado projeto_ideias_v2.txt.

### Aula 02 - Faça como eu fiz: copiando e movendo arquivos

No gerenciamento de arquivos e diretórios em nossos computadores pessoais e de trabalho, frequentemente precisamos organizar, copiar e renomear arquivos e diretórios. Tratam-se de operações essenciais para manter nossos projetos bem organizados e acessíveis.

Agora é a sua vez de praticar como criar cópias e mover arquivos usando comandos do terminal Linux.

Imagine que você esteja trabalhando em um diretório chamado ideias. Você precisa criar uma cópia de um arquivo chamado projeto_ideias.txt e, em seguida, mover esse arquivo para um novo diretório. Como você realizaria essas ações?

Para obter mais informações sobre como realizar essa tarefa em seu terminal, clique na opinião da pessoa instrutora!

Opinião do instrutor

Se você ainda não estiver no diretório ideias, use o comando cd para mudar para o diretório correto.

Para criar uma cópia do arquivo projeto_ideias.txt, use o comando cp da seguinte forma:

> cp projeto_ideias.txt projeto_ideias_copia.txt

Assim será criada uma cópia do arquivo original chamada projeto_ideias_copia.txt no mesmo diretório ideias.

Para garantir que a cópia foi criada com êxito, basta listar os arquivos presentes no diretório usando o comando ls.

Suponha que você deseja mover o arquivo copiado para um diretório chamado backup, que pode estar no mesmo nível que o diretório ideias. Navegando para o diretório em que backup está localizado com o comando cd, mova o arquivo com o comando mv.

> mv /caminho/para/ideias/projeto_ideias_copia.txt .

Observando estes passos, você conseguirá copiar e mover arquivos no terminal Linux de forma ágil e precisa.

### Aula 02 - Para saber mais: dicionário de comandos

Exploramos novos comandos e funcionalidades do nosso ambiente Linux ao longo dessa aula. Para relembrar o que vimos de forma resumida, listamos aqui os comandos investigados neste passo.

- mkdir (Make Directory): Cria novos diretórios.

- touch: Cria um arquivo vazio ou atualiza a data de modificação de um arquivo existente.

- nano: Editor de texto no terminal, usado para criar e editar arquivos.

- mv (Move): Move ou renomeia arquivos e diretórios.

- cp (Copy): Copia arquivos e diretórios.

- clear: Limpa a tela do terminal, removendo o histórico visível.

- ls -l (List Long): Lista arquivos e diretórios com detalhes, incluindo permissões e proprietários.

- ls -al (List All Long): Combina as opções -a e -l, listando todos os arquivos com detalhes.

### Aula 02 - Nesta aula, você aprendeu como:

- Utilizar o comando mkdir para criar e organizar projetos e diretórios de maneira hierárquica;
- Criar e editar arquivos de texto com touche nano para inserir e modificar conteúdos diretamente no terminal de um computador;
- Usar o comando ls com a opção -l para verificar permissões e obter mais detalhes de itens contidos em um diretório;
- Aplicar o comando mvpara movimentar e renomear arquivos e diretórios;
- Utilizar o comando cp para criar cópias de arquivos.

## Aula 03 - Gerenciando estruturas e o sistema

### Aula 03 - Removendo arquivos e obtendo informações - Vídeo 1

Lista completa de comandos e informações (lista pessoal não inclusa no curso):

- man -k .
- man man
- help
- info

Formas de obter informações do um determinado comando (lista pessoal não inclusa no curso):

- man COMANDO
- info COMANDO
- COMANDO --help

Nessa aula, aprendemos como obter informações sobre comandos do Linux, usando --help e man -k. Também exploramos como remover arquivos e diretórios vazios com os comandos rm e rmdir, respectivamente.

Vimos que o rm remove arquivos de forma permanente, sem uma "lixeira" como no Windows. Já o rmdir só funciona em diretórios vazios.

No próximo vídeo, vamos aprender como remover diretórios que não estão vazios!

### Aula 03 - Excluindo arquivos

Na Clínica Médica Voll, você foi designado para organizar os arquivos e diretórios do sistema. Durante essa tarefa, você encontrou um diretório chamado pacientes_antigos dentro do diretório arquivos_clinica. Ao tentar removê-lo usando o comando rmdir, você recebeu uma mensagem de erro indicando que o diretório não está vazio.

Sabendo que será necessário verificar os itens armazenados no diretório antes de realizar sua exclusão para evitar perdas de informações, qual a melhor abordagem para resolver essa situação?

Resposta:  
> rm -i arquivos_clinica/pacientes_antigos/*  
rmdir arquivos_clinica/pacientes_antigos

Esta abordagem garante que você confirme a exclusão de cada arquivo, evitando a remoção acidental de arquivos importantes. No entanto, em alguns casos, pode se tornar um processo repetitivo e demorado se houver muitos arquivos.

### Aula 03 - Para saber mais: entendo as permissões no Linux

No Linux, quando um arquivo ou diretório é criado, algumas permissões são atribuídas a ele de forma automática.

As permissões são divididas em três grupos: o usuário que criou o arquivo (usuário proprietário), o grupo dono do arquivo (que pode conter vários usuários) e os demais usuários que não pertencem ao grupo dono.

Essas permissões podem ser de três tipos:

- Leitura (r, read): para visualizar o conteúdo de um arquivo ou diretório.
- Escrita (w, write): para alterar o conteúdo de um arquivo e diretório.
- Execução (x, execute): para tornar o arquivo um executável, ou, no caso de um diretório, ser possível acessá-lo e usar comandos, como o cd.

Para visualizar a permissão de um diretório, podemos ir até o local onde esse diretório se encontra utilizando o comando cd. Então, podemos listar os arquivos e suas permissões utilizando ls com a opção -l. Assim, obteremos informações detalhadas sobre cada item armazenado no diretório. As permissões estão listadas na primeira coluna do resultado exibido.

Essa primeira coluna é dividida em dez caracteres. O primeiro nos mostra se o objeto listado é um arquivo (-) ou um diretório (d). Os nove caracteres restantes indicam as permissões do usuário dono, grupo dono e outros usuários respectivamente. Caso tenha alguma permissão, é mostrado a letra correspondente a permissão, senão um traço (-) é apresentado.

### Aula 03 - Removendo diretórioas e redirecionando saídas para arquivos - Vídeo 2

Para remoção de diretórios e seus conteúdo, usamos:

> rm -ri nomediretorio

Nessa aula, aprendemos como remover diretórios que não estão vazios usando o comando rm com as opções -r (remoção recursiva) e -i (remoção iterativa). Também exploramos o redirecionamento de saídas de comandos para arquivos, usando os símbolos **> (sobrescrever)** e **>> (adicionar)**. Vimos como usar o comando ls para listar arquivos e diretórios, cat para visualizar o conteúdo de um arquivo e **echo** para exibir informações no terminal.

Legal, né? Agora você já sabe como gerenciar arquivos e diretórios no Linux de forma mais eficiente!

### Aula 03 - Gerenciando atualizações - Vídeo 3

> sudo apt update  
sudo apt upgrade  
sudo apt install nomepacote

Nessa aula, aprendemos como gerenciar atualizações e instalar pacotes no Linux usando o gerenciador de pacotes apt.

Vimos que o apt nos ajuda a identificar quais pacotes estão desatualizados e precisam de um upgrade, além de instalar novos pacotes.

Para atualizar os pacotes, usamos o comando **sudo apt update** para verificar se há atualizações disponíveis e **sudo apt upgrade** para instalar as atualizações.

Para instalar um pacote específico, usamos o comando **sudo apt install nomepacote**.

### Aula 03 - Faça como eu fiz: utilizando o gerenciador de pacotes

Desempenho e segurança são requisitos fundamentais em sistemas operacionais. Em ambientes Linux, esses aspectos são mantidos e aprimorados por meio do uso eficiente de gerenciadores de pacotes. Um gerenciador é uma ferramenta de software que facilita a instalação, atualização, remoção e gerenciamento de pacotes de software de um sistema operacional. Ao longo desta aula, vimos que no Ubuntu o apt é o gerenciador padrão.

Agora é a sua vez de atualizar e realizar instalações de pacotes em ambientes Linux utilizando gerenciadores de pacotes. Verifique se há atualizações disponíveis para o seu ambiente, realize a instalação das atualizações e explore a instalação e remoção de pacotes. Para obter mais informações sobre como realizar essa atividade no seu terminal, clique na opinião da pessoa instrutora!

Opinião do instrutor

> sudo apt update  
sudo apt upgrade  
sudo apt install python3  
sudo apt remove python3  
sudo apt purge python3

Para verificar se há atualizações disponíveis em seu ambiente Linux, abra o terminal e use o comando sudo apt update. Este comando atualizará a lista de pacotes disponíveis e suas versões, sem realizar instalações ainda.

Após verificar as atualizações disponíveis, você pode realizar as instalações executando o comando sudo apt upgrade. Desse modo, será realizado o download e a instalação das atualizações necessárias para os pacotes existentes em seu sistema. Esse processo pode levar algum tempo, dependendo do número de pacotes que precisam ser atualizados e da velocidade da sua conexão com a internet.

Se você deseja instalar um novo pacote de software, como o Python, use o comando sudo apt install python3. Assim, será feito o download e a instalação da versão mais recente do Python3 disponível nos repositórios do Ubuntu.

Caso você deseje remover um pacote instalado, como o Python3, utilize o comando sudo apt remove python3. Dessa forma, o pacote será removido do seu sistema, mas serão mantidos os arquivos de configuração. Alternativamente, caso queira remover também esses arquivos, basta usar o comando sudo apt purge python3.

### Aula 03 - Para saber mais: dicionário de comandos

Exploramos novos comandos e funcionalidades do nosso ambiente Linux ao longo dessa aula. Para relembrar o que vimos de forma resumida, listamos aqui os comandos investigados neste passo.

- rm (Remove): Remove arquivos especificados.

- rm -i (Remove Interactive): Remove arquivos especificados solicitando confirmação antes da exclusão.

- rmdir (Remove Directory): Remove somente diretórios vazios.

- rm -r (Remove Recursive): Remove diretórios e seus conteúdos de forma recursiva.

- ls > arquivo.txt (List to File): Redireciona a saída do comando ls para o arquivo especificado, criando o arquivo se ele não existir.

- ls >> arquivo.txt (Append List to File): Adiciona a saída do comando ls ao final do arquivo especificado sem sobrescrevê-lo.

- echo "mensagem" >> arquivo.txt (Echo Append): Adiciona a mensagem especificada ao final do arquivo, sem sobrescrever seu conteúdo.

- echo "mensagem" (Echo): Exibe a mensagem especificada no terminal.

- sudo apt update (Update Packages): Atualiza a lista de pacotes disponíveis e suas versões no sistema. O comando requer privilégios de superusuário.

- sudo apt upgrade (Upgrade Packages): Instala as atualizações de pacotes disponíveis no sistema. O comando requer privilégios de superusuário.

- sudo apt install pacote (Install Package): Instala o pacote especificado. O comando requer privilégios de superusuário.

- sudo apt remove pacote (Remove Package): Remove o pacote especificado do sistema.

- sudo apt purge pacote: Remove o pacote e seus arquivos de configuração.

### Aula 03 - NesTa aula, você aprendeu como:

- Utilizar os comandos rm e rmdir para remover arquivos e diretórios de ambientes Linux;
- Redirecionar a saída de comandos para arquivos utilizando o símbolo >, e adicionar o conteúdo a arquivos existentes usando >>;
- Aplicar o comando echo para adicionar mensagens ou informações em arquivos ou exibi-las no terminal;
- Usar o gerenciador de pacotes apt para atualizar e gerenciar pacotes no sistema Linux;
- Aplicar os comandos sudo apt update, sudo apt upgrade e sudo apt install package_name para realizar atualizações e instalações.

## Aula 04 - Realizando tarefas administrativas

### Aula 04 - Analizando processos em execução - Vídeo 1

Nessa aula, aprendemos como analisar os processos em execução no Linux usando o comando **top**. O top exibe uma tabela dinâmica com informações sobre cada processo, como PID (identificação), USER (usuário), PR (prioridade), NI (nice value), VIRT (memória virtual), RES (memória residente), SHR (memória compartilhada), S (estado) e o percentual de uso de CPU e memória.

Podemos ordenar essa tabela por diferentes critérios, como o percentual de uso de CPU (pressionando "P") ou de memória (pressionando "M"). Para sair do top, basta pressionar "Q".

Legal, né? Essa aula nos deu uma visão geral do gerenciamento de processos no Linux, e agora podemos usar o top para monitorar o desempenho do nosso sistema e identificar processos que estão consumindo muitos recursos.

### Aula 04 - Gerenciando processos

Você está gerindo um servidor Linux que está apresentando lentidão. Para identificar a causa, você decide usar o comando top para analisar os processos em execução. Após abrir a tabela dinâmica gerada pelo top, você observa que há muitos processos em estado "sleeping" e alguns consumindo uma quantidade significativa de CPU e memória.

Qual processo é mais crítico para o desempenho do servidor?

Resposta: Processo com maior valor de %CPU.

O processo que consome mais CPU é o que mais impacta o desempenho do servidor. Interrompê-lo pode liberar recursos significativos.

### Aula 04 - Filtrando Processos - Vídeo 2

> ps  
ps -u nomeusuario  
ps -C nomeprocesso  
pstree  
ps aux  
ps aux --sort=-%mem  
ps aux --sort=-%mem | head -n 11

Nessa aula, aprendemos sobre o comando ps, que nos permite analisar os processos em execução no sistema. Vimos como usar o ps com diferentes opções para obter informações detalhadas sobre os processos, como o usuário, PID, uso de CPU e memória, status e tempo de execução.

Também aprendemos a filtrar processos por usuário, visualizar a relação de subordinação entre eles em formato de árvore com o pstree e a classificar os processos por uso de memória com o ps aux --sort=-%mem.

Por fim, vimos como usar o head para filtrar a saída do ps aux e exibir apenas os 10 processos que mais consomem memória.

### Aula 04 - Filtrando a saída de comandos

Você está administrando um servidor Linux e precisa identificar rapidamente os processos que estão consumindo mais memória.

Qual combinação de comandos você deve usar para listar os 15 processos que mais consomem memória em seu servidor?

Resposta: ps aux --sort=-%mem | head -n 16

Esta combinação lista os 16 processos que mais consomem memória, incluindo o cabeçalho.

### Aula 04 - Para saber mais: encadeamento de comandos

Quando executamos um comando no terminal, muitas vezes a saída pode ser extensa e conter um grande volume de informações. Para facilitar nosso processo de análise, podemos usar comandos para filtrar, ordenar e exibir apenas as informações relevantes de acordo com nossa demanda.

Por exemplo, se estamos buscando apenas processos relacionados ao navegador Firefox em um ambiente Linux, podemos usar o seguinte encadeamento de comandos em nosso terminal:

> ps aux | grep firefox

O pipe (|) é utilizado para redirecionar a saída de um comando para a entrada de outro comando. Dessa forma, conseguimos encadear vários comandos e processar sua saída de forma sequencial.

O comando grep, que ainda não exploramos por aqui, realiza uma busca por padrões especificados pelo usuário. Neste caso, estamos filtrando a lista de processos para exibir apenas aqueles que contêm o padrão firefox.

Para exibir linhas específicas da saída de um comando, podemos utilizar os comandos head e tail.

O comando head exibe as primeiras linhas da saída. Por padrão, ele exibe as primeiras 10 linhas, mas podemos ajustar o número de linhas exibidas usando a opção -n.

O comando tail exibe as últimas linhas da saída. Por padrão, ele mostra as últimas 10 linhas, mas podemos ajustar o número de linhas exibidas utilizando também a opção -n.

Por outro lado, podemos aplicar o comando sort para ordenar a saída de um comando ou o conteúdo de um arquivo. Por exemplo, para ordenar a lista de processos pela coluna de uso de CPU, podemos combinar os comandos ps aux com sort da seguinte forma:

> ps aux --sort=-%CPU

Utilizando --sort=-%CPU estamos ordenando a saída de modo decrescente com base no uso de CPU.

### Aula 04 - Controlando processos - Vídeo 3

> kill  
kill numeroPID  
kill -9  numeroPID
kill -STOP numeroPID  
pkill nomeprocesso
killall nomeprocesso (que vai enviar um sinal para todos os processos com nomes similares)

Nessa aula, exploramos como controlar processos no Linux, uma habilidade essencial para qualquer administrador de sistemas.

Começamos relembrando o conceito de PID (Process ID), que é o número de identificação único de cada processo em execução. Com o PID, podemos enviar sinais para os processos, controlando seu comportamento.

Vimos como usar o comando kill para interromper processos. Aprendemos a interromper um processo de forma suave usando o sinal SIGTERM, que permite que o processo finalize suas operações antes de ser encerrado. Também exploramos como interromper um processo abruptamente usando o sinal -9, que força o processo a parar imediatamente.

Além de interromper, também aprendemos como pausar um processo usando o sinal -STOP. Essa técnica permite que você suspenda um processo temporariamente e o retome posteriormente, se necessário.

Por fim, exploramos como usar o comando kill com o nome do processo, usando pkill e killall. É importante lembrar que esses comandos devem ser usados com cuidado, pois podem afetar vários processos com nomes semelhantes.

### Aula 04 - Faça como eu fiz: usando kill no controle de processos

No gerenciamento de sistemas Linux, não basta monitorar os processos em execução, é necessário entender como interagir diretamente com esses processos quando necessário. Nesta aula, exploramos como usar o comando kill para enviar sinais a processos, permitindo que você possa interromper, pausar ou encerrar processos de maneira eficiente.

Chegou a sua vez de praticar o controle de processos. Utilize o comando kill para enviar sinais de interrupção, pausa e retomada a processos em execução. Para realizar esses testes, crie processos com o editor de texto nano em seu terminal. Caso queira obter mais informações sobre o passo a passo dessa atividade.

Opinião do instrutor

Para começar a atividade, abra um terminal e inicie um processo que você possa controlar, como o editor de texto nano. Basta usar o comando nano no terminal para abrir o editor de texto e deixar um processo em execução.

Na sequência, abra um novo terminal para monitorar e controlar o processo que você iniciou. Utilize o comando ps aux para listar todos os processos em execução e localizar o PID (Process ID) do processo nano.

Com o PID, você pode testar o comando kill para testar a interrupção de um processo no ambiente Linux. Para isso, digite kill PID, substituindo PID pelo número do processo indicado pelo ps aux. Ao executar este comando será enviado um sinal de interrupção (SIGTERM), encerrando o processo de modo ordenado.

Para forçar a interrupção abrupta do processo, você pode utilizar o comando kill -9 PID. O sinal -9 envia uma interrupção abrupta que encerra o processo de maneira imediata.

Para pausar e retomar um processo, você pode testar outras opções do comando kill com o nano em execução. Primeiro, utilize o comando kill -STOP PID para pausar o processo. O nano será suspenso e não consumirá recursos, mas ainda estará presente na lista de processos. Na retomada da execução do processo, utilize o comando kill -CONT PID.

### Aula 04 - Para saber mais: dicionário de comandos

Exploramos novos comandos e funcionalidades do nosso ambiente Linux ao longo dessa aula. Para relembrar o que vimos de forma resumida, listamos aqui os comandos investigados neste passo.

- top (Table of Processes): Exibe uma visão em tempo real dos processos em execução, mostrando detalhes como PID, uso de CPU e memória e o estado dos processos.

- ps (Process Status): Fornece uma "fotografia" dos processos em execução no momento. Algumas opções que utilizamos foram:

- ps aux (All Users Extended): lista todos os processos em execução com detalhes extensivos, como %CPU, %MEM, PID etc.
- ps -u [usuario] (User): filtra e exibe apenas os processos pertencentes ao usuário especificado.
- ps -p [PID] (Process ID): exibe informações detalhadas sobre um processo específico com o PID fornecido.
- ps -C [comando] (Command): filtra e exibe os processos associados ao comando especificado.
- pstree (Process Tree): Mostra a árvore de processos, ilustrando a relação hierárquica entre processos pai e filho.

- head (Head of File): Exibe as primeiras linhas de um arquivo ou saída de um comando. Por padrão, exibe as primeiras 10 linhas.

- pipe (|) (Pipe): Redireciona a saída de um comando como entrada para outro comando. É utilizado no encadeamento de comandos e processamento de dados de modo sequencial.

- sort (Sort): Ordena a saída de um comando ou o conteúdo de um arquivo. Pode ser combinado com outros comandos usando pipe para organizar dados.

- kill (Kill Process): Envia sinais para processos em execução, sendo comumente utilizado com os seguintes sinais:

- kill [PID] (Process ID): envia o sinal padrão de interrupção (SIGTERM) para o processo com o PID especificado, permitindo uma interrupção suave.
- kill -9 [PID] (Signal 9): envia um sinal de interrupção abrupta (SIGKILL) para o processo com o PID especificado, forçando o encerramento imediato.
- kill -STOP [PID] (Stop Signal): envia um sinal de pausa (SIGSTOP) para o processo, interrompendo sua execução sem encerrar o processo.
- kill -CONT [PID] (Continue Signal): envia um sinal de continuação (SIGCONT) para retomar a execução de um processo que foi pausado.
- pkill (Process Kill): Envia sinais para processos com base no nome do processo. Ao utilizar esse comando, lembre-se que todos os processos que correspondem ao nome fornecido poderão ser afetados.

- killall (Kill All): Envia sinais para todos os processos com um nome específico. É um comando bastante prático para enviar sinais a múltiplos processos de forma simultânea.

### Aula 04 - Nesta aula, você aprendeu como:

- Utilizar o comando top para monitorar processos em tempo real, observando informações como PID, uso de CPU e memória e o estado dos processos;
- Usar o comando ps para obter uma visão estática dos processos em execução, aplicando opções como ps aux para obtenção de uma lista detalhada;
- Filtrar e ordenar a saída do ps para análise de processos específicos, adotando opções como ps -u para filtrar por usuário e ps -p PID para informações sobre processos específicos;
- Utilizar o comando pstree para visualizar a hierarquia de processos e entender a relação entre diferentes processos;
- Controlar processos com o comando kill, enviando sinais como SIGTERM (15) para uma interrupção suave e SIGKILL (9) para uma interrupção abrupta.

## Aula 05 - Acesso remoto no Linux

### Aula 05 - Criando instâncias com Linux na nuvem - Vídeo 1

Nesta aula, aprendemos como acessar um ambiente Linux na nuvem, utilizando a AWS (Amazon Web Services). Vimos como criar uma instância EC2, que é como um servidor virtual, e configuramos tudo para podermos acessá-la remotamente.

Começamos explorando o console da AWS, onde criamos uma conta e configuramos uma instância EC2 com o Amazon Linux. Depois, criamos um par de chaves para garantir uma conexão segura com a instância.

Em seguida, testamos alguns comandos básicos no terminal da instância, como pwd, ls, cd e yum update, confirmando que o que aprendemos no Ubuntu também se aplica ao Amazon Linux.

No final, vimos como podemos acessar a instância remotamente usando SSH e a chave que baixamos.

### Aula 05 - Para saber mais: entendendo a computação em nuvem

A computação em nuvem (cloud computing) é um conceito que se refere ao uso de um conjunto de serviços, incluindo aplicações, armazenamento e processamento, todos baseados na internet.

Segundo uma definição do NIST (National Institute of Standards and Technology), referenciada no livro Web Services RESTful (2015) de Ricardo Lecheta:

> “Computação em nuvem é um modelo de computação que provê um conjunto compartilhado de recursos de computação customizáveis como redes, servidores, armazenamento, aplicações e serviços.”

Com a utilização de uma solução em nuvem, as empresas têm a possibilidade de utilizar recursos de armazenamento e computação de forma compartilhada e delegar a manutenção de sua infraestrutura, reduzindo drasticamente os seus custos.

O modelo da computação em nuvem é baseado na demanda, permitindo que você acesse os recursos de acordo com suas necessidades específicas. Dessa forma, é possível aumentar os recursos (como armazenamento e capacidade de processamento) durante períodos de alta demanda e reduzi-los em momentos de menor demanda, reduzindo custos.

Imagine uma empresa de e-commerce que enfrenta um aumento significativo no tráfego durante o período de Black Friday. Utilizando a computação em nuvem, essa empresa pode aumentar temporariamente a capacidade de seus servidores para lidar com o aumento de visitas e transações. Após o término do período de alta demanda, a capacidade pode ser reduzida novamente, evitando custos desnecessários.

Caso queira entender mais sobre a computação em nuvem e suas aplicações práticas no dia a dia, recomendamos a leitura do artigo [Cloud: o que é, História e Guia da computação em nuvem](https://www.alura.com.br/artigos/cloud) disponível aqui na plataforma.

### Aula 05 - Faça como eu fiz: virtualizando recursos na nuvem

Virtualização é a tecnologia que permite criar múltiplos ambientes de computação isolados em um único hardware físico.

Imagine um computador físico (máquina host) rodando várias máquinas virtuais (VMs). Cada VM funciona como um computador completo, com seu próprio sistema operacional e recursos de hardware, como memória, processamento e armazenamento. Esse processo é gerenciado por um software chamado hipervisor.

A computação em nuvem consiste na entrega de recursos de computação (como servidores, armazenamento e bancos de dados) pela internet, com pagamento sob demanda. Provedores de nuvem utilizam a virtualização para criar e gerenciar seus data centers de modo eficiente. Portanto, a virtualização é a base da computação em nuvem.

Agora é a sua vez de explorar a virtualização de recursos na nuvem criando uma instância t2.micro na AWS com o sistema operacional Amazon Linux 2023. Caso queira obter mais informações sobre o passo a passo dessa tarefa, clique na opinião da pessoa instrutora!

Opinião do instrutor

Primeiro, acesse o console da AWS e faça login com sua conta. Se ainda não tiver uma conta, você pode criar uma e usar os créditos gratuitos da AWS.

No console da AWS, digite "EC2" na barra de busca e selecione "Instâncias EC2". Clique em "Launch Instance" para iniciar a criação de uma nova instância.

Dê um nome à sua instância, por exemplo, projeto_linux. Selecione a imagem do sistema operacional Amazon Linux 2023. A instância t2.micro está incluída no nível gratuito, então escolha essa opção.

Nas configurações de rede, assegure que o tráfego SSH esteja habilitado. Dessa forma, a instância poderá ser acessada de modo remoto. Gere um novo par de chaves para a conexão SSH. Esse par de chaves consiste em uma chave pública, que será armazenada na instância, e uma chave privada, que será baixada para o seu computador.

Lembre-se de armazenar a chave privada em um local seguro, pois vamos utilizá-la no próximo passo da nossa jornada de aprendizado aqui no curso.

Finalize a criação da instância clicando em "Launch Instance". Aguarde até que a instância esteja em execução.

### Aula 05 - Acesso remoto com SSH - Vídeo 2

Nessa aula, aprendemos como usar o SSH para acessar remotamente uma instância do Linux na AWS. O SSH é um protocolo seguro que permite conectar-se a outros computadores e realizar tarefas como gerenciar arquivos e executar comandos.

Primeiro, precisamos mover a chave SSH para o diretório home do nosso usuário. Depois, podemos usar o comando ssh para conectar-se à instância, especificando a chave, o usuário e o endereço IP.

> ssh -i /home/lcs/conexao_instancia.pem ec2-user@18.191.223.220

Se a conexão falhar, podemos ajustar as permissões da chave usando o comando chmod.

Para isso, utilizamos o comando chmod, que significa change mode (mudar o modo). Esse comando permite alterar as permissões de um arquivo específico. Então, vamos digitar chmod no terminal e indicar o novo código de permissão. Observe que o código atual é 644, que é bastante aberto. Vamos alterar para 400, um modo mais restrito, permitindo apenas o acesso do usuário LCS.

Além disso, precisamos especificar o caminho completo do arquivo para que a mudança seja efetiva: /home/lcs/conexao_instancia.pem.

> chmod 400 /home/lcs/conexao_instancia.pem

Após a conexão, podemos usar comandos como ls e mkdir para gerenciar arquivos e diretórios na instância.

### Aula 05 - Estabelecendo conexão SSH na AWS

Você está trabalhando em um projeto na Clínica Médica Voll e precisa configurar um servidor na nuvem para hospedar uma aplicação web. Após criar uma instância EC2 na AWS com uma distribuição Linux, você precisa se conectar remotamente a essa instância usando SSH.

Considere que a chave privada está localizada em /home/usuario/voll.pem, o usuário da instância é ec2-user e o endereço IP da instância é 192.168.0.1.

Qual comando você deve usar para se conectar à instância?

Alternativa correta
ssh -i /home/usuario/voll.pem ec2-user@192.168.0.1

Este comando especifica o caminho da chave privada com a opção -i, o usuário ec2-user e o endereço IP 192.168.0.1.

### Aula 05 - Para saber mais: alterando permissões no Linux

No Linux, o comando chmod (change mode) é usado para alterar as permissões de arquivos e diretórios. As permissões controlam quem pode ler, escrever ou executar um arquivo.

Vimos que as permissões são representadas por três grupos de três caracteres, cada um representando as permissões do proprietário, do grupo e de outros usuários. Cada conjunto de três caracteres pode incluir:

- r (read) - Permissão de leitura
- w (write) - Permissão de escrita
- x (execute) - Permissão de execução

Para definir essas permissões, usamos valores numéricos:

- 4 para leitura (r)
- 2 para escrita (w)
- 1 para execução (x)

Esses valores podem ser combinados. Por exemplo, uma permissão de leitura e escrita seria 4 + 2 = 6.

No caso da chave privada que utilizamos para a conexão SSH, usamos o comando:

> chmod 400 caminho/para/sua-chave.pem

O valor 400 define as permissões como:

4 (leitura) para o proprietário do arquivo
0 (sem permissões) para o grupo
0 (sem permissões) para outros usuários

Desse modo, somente o proprietário pode ler a chave privada e ninguém mais conseguirá acessá-la garantindo sua segurança. Essa configuração é essencial para a segurança das conexões SSH, pois impede que outros usuários leiam ou modifiquem a chave privada, protegendo o acesso ao servidor remoto.

### Aula 05 - Faça como eu fiz: acesso remoto às instâncias EC2

As aplicações do acesso remoto às instâncias EC2 utilizando o protocolo SSH (Secure Shell) são diversas. Trata-se de uma prática fundamental para a manutenção e administração de servidores, permitindo a realização de atualizações e configurações sem necessidade de acesso físico ao hardware.

Além disso, o protocolo SSH é utilizado para transferência de arquivos de forma segura entre o computador local e a instância EC2, facilitando a implementação de aplicações.

Entender como utilizar o protocolo SSH é um passo importante para garantir o gerenciamento eficiente e seguro de instâncias na AWS. Que tal explorar como acessar a instância EC2 criada usando SSH?

Para conferir detalhes sobre esse processo, conforme realizado durante a aula, clique na Opinião da pessoa instrutora.

Opinião do instrutor

No sistema de arquivos do seu computador, crie uma cópia da chave privada gerada no processo de criação da instância t2.micro no EC2 da AWS e realize sua transferência para o diretório /home de seu usuário no ambiente Linux.

Em seguida, acesse o terminal Linux e navegue até o diretório /home com o comando cd. Altere as permissões da chave para garantir que ela esteja segura e acessível apenas pelo proprietário. Use o comando:

> chmod 400 caminho/para/sua-chave.pem

Esse comando define as permissões da chave privada, garantindo que ela não esteja aberta para leitura e escrita por outros usuários.

Então, conecte-se à instância EC2 utilizando o comando ssh. Substitua caminho/para/sua-chave.pem pelo caminho completo para o arquivo da chave privada e ec2-user@ip-da-instância pelo usuário e endereço IP da sua instância.

> ssh -i caminho/para/sua-chave.pem ec2-user@ip-da-instância

Após executar esse comando, você estará conectado à sua instância EC2 e poderá executar comandos diretamente no servidor remoto.

Com isso, você pode realizar manutenções, atualizações e transferências de arquivos de forma segura e eficiente entre seu computador local e a instância EC2, aproveitando todas as vantagens da computação na nuvem.

### Aula 05 - Para saber mais: dicionário de comandos

Exploramos novos comandos e funcionalidades do nosso ambiente Linux ao longo dessa aula. Para relembrar o que vimos de forma resumida, listamos aqui os comandos investigados neste passo.

- ssh (Secure Shell): Estabelece uma conexão segura entre um cliente e um servidor remoto.

- ssh -i caminho/para/sua-chave.pem usuário@ip_da_instância (SSH with Identity File): Conecta-se a uma instância EC2 usando um arquivo de chave privada para autenticação.

- chmod (Change Mode): Modifica as permissões de arquivos ou diretórios.

- chmod 400 caminho/para/sua-chave.pem (Change Mode 400): Define permissões de leitura para o proprietário e remove todas as permissões para grupo e outros usuários.

- sudo yum update (YUM Update): Atualiza todos os pacotes instalados na distribuição Linux baseada em CentOS, como o Amazon Linux.

- history (History): Exibe o histórico de comandos executados no terminal.

### Aula 05 - Referências

Vamos deixar aqui algumas referências para que você possa investigar e explorar mais detalhes sobre Linux.

1 - [GuiaFoca](https://www.guiafoca.org/) (gratuito, português, guia)

> Guia abrangente em português que contempla diversos tópicos relacionados ao uso e à aplicação de sistemas Linux, partindo de conceitos básicos e explorando até aspectos de administração mais avançada do sistema. Os tópicos são apresentados de forma contextualizada e prática, facilitando sua compreensão.

2 - Monitoramento de distribuições Linux - [DistroWatch.com](https://distrowatch.com/) (gratuito, inglês, texto)

> Site dedicado à divulgação de informações, estatísticas e análises sobre distribuições de sistemas operacionais Linux e outros sistemas Unix-like. É uma ferramenta valiosa para quem deseja explorar as diversas opções disponíveis dentro do ecossistema Linux.

3 - Começando com o [Linux: Comandos, serviços e administração](https://www.casadocodigo.com.br/products/livro-linux) (não gratuito, português, livro)

> Neste livro, Daniel Romero parte desde a instalação do Ubuntu, utilização de comandos básicos, conhecimento do sistema de diretórios para depois atacar processos, configuração de pacotes como Apache, PHP, Java e MySQL, para depois chegar na criação de seus próprios scripts.

4 - [Linux: o que é esse sistema operacional](https://www.alura.com.br/artigos/linux), como instalar e um Guia para iniciar (gratuito, português, texto)

> O artigo discute a história do Linux, seus principais recursos e sua ampla gama de aplicações. É uma boa introdução ao Mundo Linux para pessoas que estão interessadas em aprender mais sobre essa grande família de sistemas operacionais open source.

5 - [The Linux Command Line](https://linuxcommand.org/tlcl.php) (gratuito, inglês, texto)

> O site é focado na oferta de recursos educacionais e materiais de aprendizado para pessoas usuárias do Linux, em especial para aquelas que desejam aprimorar suas habilidades no uso de comandos e ferramentas no terminal do Linux. Este site foi criado por William Shotts, autor do livro The Linux Command Line.

### Aula 05 - Nessa aula, você aprendeu como:

- Provisionar e configurar uma instância na nuvem da AWS com o sistema operacional Linux para uso na hospedagem de aplicações web;
- Configurar uma instância para permitir tráfego SSH, gerando um par de chaves para acesso remoto seguro;
- Conectar-se à instância Linux criada na AWS usando o Amazon EC2 Connect para realizar tarefas administrativas em servidores a partir de navegadores web;
- Utilizar o comando chmod para alterar as permissões de acesso e leitura de um arquivo;
Adotar o protocolo SSH para conexão remota segura com dispositivos computacionais e instâncias.

### Aula 05 - Conclusão - Vídeo 3

Que legal que você chegou até aqui! Essa aula foi um resumo de tudo que aprendemos nesse curso, né? Começamos do zero, aprendendo a navegar no terminal do Linux, a criar e organizar arquivos e pastas, e até a instalar programas! Depois, evoluímos para tarefas mais complexas, como gerenciar atualizações e visualizar processos em execução. No final, aprendemos a conectar remotamente ao Linux usando o SSH, garantindo segurança com uma chave de criptografia.

Você se lembra de como criamos uma instância na AWS e acessamos ela remotamente? Que legal, né?

Agora, continue praticando tudo que aprendeu e não hesite em usar o fórum e o Discord para tirar dúvidas.
