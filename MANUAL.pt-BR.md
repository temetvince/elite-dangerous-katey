# Manual do Katey

Também em [English](MANUAL.md), [Deutsch](MANUAL.de.md), [Español](MANUAL.es.md), [Français](MANUAL.fr.md)
e [Русский](MANUAL.ru.md).

O Katey é um companheiro de desktop para Elite Dangerous. Ele lê os arquivos de diário que o jogo grava na sua
própria máquina e acompanha você enquanto joga. Planeja a engenharia contra os materiais que você realmente
tem. Confere as builds de nave salvas contra as naves e os módulos que você possui. Segue a sua rota e o
sistema em que você está, e lê o seu histórico como um livro-caixa. Nada sai da sua máquina a menos que você
permita em Ajustes, onde toda conexão de saída começa desligada.

## Sumário

- [Primeiros passos](#primeiros-passos)
- [A janela](#a-janela): a [barra superior](#a-barra-superior), a [barra inferior](#a-barra-inferior),
  a [busca](#busca), [a gaveta do plano](#a-gaveta-do-plano) e [o painel ao vivo](#o-painel-ao-vivo)
- [As páginas](#as-páginas): [Início](#início), [Engenheiros](#engenheiros), [um engenheiro](#um-engenheiro),
  [Módulos](#módulos), [um tipo de módulo](#um-tipo-de-módulo), [um projeto](#um-projeto),
  [Materiais](#materiais), [um material](#um-material), [Desbloqueios](#desbloqueios), [Frota](#frota),
  [uma nave](#uma-nave), [uma montagem](#uma-montagem), [Sistema](#sistema), [Rota](#rota),
  [Estatísticas](#estatísticas) e [Ajustes](#ajustes)
- [Trabalhando com o EDSY](#trabalhando-com-o-edsy)
- [Naves pré-montadas](#naves-pré-montadas)
- [O que o diário não pode dizer ao Katey](#o-que-o-diário-não-pode-dizer-ao-katey)
- [Seus dados](#seus-dados)

## Primeiros passos

1. **Inicie o Katey.** A versão lançada é um único executável; nada precisa ser instalado. Só um Katey roda
   por vez: iniciar um segundo mostra um aviso e deixa o primeiro em paz, já que dois sobrescreveriam os
   dados salvos um do outro. No Windows, a pasta de diários do jogo é encontrada sozinha. Se a página Ajustes
   disser que nenhuma pasta foi encontrada, aponte-a para a pasta que contém arquivos chamados
   `Journal.*.log`, normalmente em `Saved Games\Frontier Developments\Elite Dangerous`.
2. **Deixe-o se atualizar.** A primeira execução repassa todo o seu histórico de diário, do arquivo mais
   antigo em diante, e mostra o progresso ao longo do topo da janela e no painel: quantos arquivos foram
   lidos, com uma barra que se preenche arquivo por arquivo. Nada ao vivo aparece até terminar, então você
   nunca vê um número de meio do caminho. Um histórico de vários anos leva alguns segundos.
3. **Marque seus engenheiros.** Abra **Engenheiros** na barra superior e marque os engenheiros que você
   desbloqueou e pretende visitar. Ligue **Mostrar só itens dos engenheiros selecionados** no topo dessa
   página e toda lista e todo seletor no Katey se restringem ao que esses engenheiros podem rolar, com cada
   seletor de grau começando no grau que eles alcançam.
4. **Planeje alguma engenharia.** Abra **Módulos**, escolha um tipo de módulo, escolha um grau e um
   experimental no projeto que você quer e pressione **Adicionar**. Ou abra a gaveta **Plano** na barra
   inferior e cole uma build do EDSY na caixa de importação; cada módulo com engenharia na build vira uma
   entrada.
5. **Leia o plano.** A gaveta lista cada entrada com quem pode rolá-la, cada material de que o plano precisa
   contra o que você tem e, no fim, as trocas a fazer nos comerciantes de materiais para cobrir o que estiver
   faltando. **Copiar lista de compras** coloca a lista de compras na área de transferência.
6. **Voe.** O painel ao vivo à direita segue o jogo: sua posição, a rota, o combustível, os corpos que valem
   um escaneamento neste sistema, com os que têm um material que está faltando marcados. **Destacar** o move
   para uma janela própria que fica sobre o jogo.
7. **Confira uma build contra a sua frota.** Abra **Frota**, cole uma build do EDSY na caixa de montagens
   salvas e abra a montagem. Ela é comparada encaixe por encaixe com a nave que melhor corresponde a ela e
   com tudo o que você tem, e um botão planeja a lacuna de engenharia.

Tudo o mais neste manual diz o que cada parte da janela faz e como aproveitá-la ao máximo.

## A janela

O Katey é uma janela com uma página no meio, um painel ao longo da borda direita e duas barras. Ele desenha a
própria barra de título nas cores do aplicativo e traz o rosto do Katey como ícone. As páginas nunca têm menos
de 960 pixels de largura, então a largura mínima da janela é essa mais o painel enquanto o painel está
acoplado; destaque o painel e a janela cabe em uma tela de 1080 pixels de largura. Ela abre centralizada na
tela principal, qualquer que tenha sido a tela em que estava por último.

### A barra superior

A barra superior é a barra de título da janela: arraste-a para mover a janela, dê dois cliques para maximizar
e use os três botões na ponta direita para minimizar, maximizar e fechar. Da esquerda para a direita, ela
contém:

- **KATEY**, a marca, que abre a [página inicial](#início).
- **Voltar** e **Avançar**, que funcionam como em um navegador e devolvem você ao ponto até onde havia
  rolado.
- Os links das páginas: **Engenheiros**, **Módulos**, **Materiais**, **Desbloqueios**, **Frota**, **Rota**
  e **Sistema**. O da seção em que você está fica aceso, então a página de um engenheiro acende
  Engenheiros e um projeto acende Módulos. Pressione um link de novo enquanto a própria página dele
  está aberta e você volta à página em que estava antes.

### A barra inferior

- **Idioma**, o globo na extremidade esquerda, mostra o idioma em que o Katey está e abre a lista dos idiomas
  em que ele é distribuído: inglês, alemão, espanhol, francês, português (Brasil) e russo, cada um no próprio
  nome, com "Idioma do sistema" primeiro. O Katey segue o idioma do seu Windows até você escolher um; a
  escolha é lembrada e a janela inteira muda de uma vez. Os nomes do próprio jogo mudam junto: materiais,
  projetos, efeitos experimentais, tipos de módulo, itens de corretor de tecnologia, classes de planeta,
  atmosferas e vulcanismo, e os números de toda a carreira. Nomes de espécies, naves e módulos que vêm do seu
  diário seguem o idioma do próprio cliente do jogo. Este manual tem uma cópia em cada idioma, com links no
  topo.
- **Ajustes**, ao lado, abre a página [Ajustes](#ajustes). Pressione de novo enquanto os ajustes estão
  abertos e você volta à página em que estava.
- **Estatísticas**, ao lado, abre a página [Estatísticas](#estatísticas) e, pressionado de novo enquanto
  ela está aberta, devolve você à página em que estava. Fica aqui, e não na barra superior, para que os
  links das páginas caibam em uma linha em todos os idiomas.
- A **caixa de busca** pelo meio; veja [Busca](#busca).
- **Plano**, à direita, abre e fecha a [gaveta do plano](#a-gaveta-do-plano). O número nele é quantas
  entradas o plano contém.

### Busca

Digite na caixa de busca, ou pressione **Ctrl K** para ir até ela, para encontrar um engenheiro, um tipo de
módulo, um projeto, um material, um sistema que você visitou, uma das suas naves ou uma montagem salva. Os
resultados sobem acima da caixa conforme você digita. **Enter** abre o primeiro resultado, as teclas de seta
percorrem a lista e **Escape** limpa a caixa. Cada resultado abre a própria página.

### Páginas, links e escolhas lembradas

Cada página descreve uma coisa ou lista coisas de um só tipo. Cada menção a outra coisa em uma página é um
link que abre a página dela: um nome de sistema abre o sistema, o nome de um engenheiro abre o engenheiro, um
chip de material abre o material. Os pequenos rótulos acima do título de uma página são links de volta ao
índice a que ela pertence.

Três mecanismos se comportam do mesmo jeito em todas as páginas, e cada um lembra seu estado entre execuções:

- **Ordenação.** Clique no cabeçalho de uma coluna para ordenar a tabela por ela; clique de novo para
  inverter a direção.
- **Seções.** Uma página longa mostra uma seção por vez, escolhida pelas abas sob o título.
- **Grupos.** Uma página que agrupa coisas abre e fecha cada grupo com um clique, com **Expandir tudo** e
  **Recolher tudo** ao lado.

Filtros, chaves e o estado aberto de cada dobra também são lembrados, então uma página volta como você a
deixou.

### A gaveta do plano

O plano é a lista da engenharia que você pretende fazer. A gaveta abre sobre qualquer página pelo botão
**Plano** e contém:

- **As entradas.** Cada uma tem:
  - uma chave para deixá-la fora dos totais sem apagá-la;
  - um seletor de grau e, para um tipo que aceita efeitos experimentais, um seletor de experimental e a chave
    **Só o experimental**; um tipo sem nenhum não mostra nenhum dos dois;
  - as rolagens esperadas por grau;
  - os engenheiros que podem rolá-la, cada um um link colorido pelas suas marcações como em todo o Katey. Uma
    entrada que ninguém que você marcou pode rolar traz um aviso.

  A chave **Só o experimental** transforma uma entrada em uma que custa só os materiais do experimental, para
  um módulo que já tem o projeto. Um desbloqueio de corretor de tecnologia que você marcou na página
  [Desbloqueios](#desbloqueios) também é uma entrada, com seus materiais fixos e as mercadorias de que ele
  também precisa de um mercado.

  O plano segue o que você faz no engenheiro:

  - cada rolagem que o jogo informa em um módulo do projeto e do tipo de uma entrada tira uma rolagem desse
    grau, ou o zera quando o grau se completa;
  - o efeito experimental sai da entrada assim que está no módulo, em qualquer grau que ele esteja;
  - quando o módulo está no grau alvo sem nada mais a querer, a entrada sai do plano.

  Só as rolagens feitas depois de a entrada ser adicionada contam, então engenharia feita antes nunca remove
  uma entrada.
- **Os totais.** Cada material de que o plano precisa, com quantos você tem e quantos faltam, agrupados em
  brutos, manufaturados e codificados. **Copiar lista de compras** coloca a lista na área de transferência
  como texto simples.
- **As trocas.** O que entregar nos comerciantes de materiais para cobrir tudo o que falta, troca por troca,
  agrupadas por comerciante. Veja [Materiais](#materiais) para saber como as trocas são escolhidas.
- **A importação do EDSY.** Cole uma build exportada do EDSY, pressione **Ler exportação**, desmarque os
  módulos que não quiser e pressione **Adicionar ao plano**. Veja
  [Trabalhando com o EDSY](#trabalhando-com-o-edsy).

**Limpar plano** esvazia o plano depois de um segundo toque, para que um deslize não o perca.

### O painel ao vivo

O painel segue o jogo ao longo da borda direita da janela. Ele mostra:

- **Onde você está** e o que a nave está fazendo: atracada, em supercruzeiro, pousada e assim por diante.
- **A rota**: a parada em que você está e as próximas, com o resto resumido. A próxima estrela coletável e
  qualquer parada de engenheiro são destacadas. A distância percorrida aparece contra o total e como
  porcentagem, com a barra do tanque como a [página de rota](#rota) a desenha. O quadro do combustível diz
  quantos saltos você pode dar antes de precisar coletar, e até qual estrela coletar. Quando uma rota do
  Spansh está guardada, o próximo ponto de rota aparece com um botão **Copiar** para o mapa da galáxia.
- **Este sistema**: o progresso do escaneamento e as estimativas de pagamento, com cada corpo escaneado que
  tem materiais na superfície ou sinais biológicos. **Dados de exploração não vendidos** é o que os dados
  deste sistema ainda a bordo pagariam. São os escaneamentos que você não vendeu, com o bônus de primeira
  descoberta onde um corpo era seu para descobrir, e as espécies que você amostrou por completo e não vendeu,
  com o bônus de primeiro registro onde o jogo disse que ninguém as havia registrado. A página do sistema
  acrescenta o que você já recebeu. O título e o anel ficam verdes quando todos os corpos estão escaneados.
  - Antes de um corpo ser mapeado, a vida dele é uma linha: quantos gêneros poderiam viver ali e quanto
    provavelmente valem, com e sem o bônus de primeiro registro. Depois de sondado, o corpo lista seus
    gêneros do mais rico ao menos. Cada linha é o gênero, sua espécie mais provável com as chances ou sua
    etapa de amostragem, e o que ele paga com o bônus onde isso se aplica. A linha fica violeta depois da
    primeira amostra, azul depois da segunda e verde quando as três foram colhidas.
  - A gravidade aparece como uma etiqueta a partir de 1,0 g, vermelha a partir de 1,5 g.
  - Uma escolha entre três diz para que serve a lista, cada uma do mais próximo ao mais distante por distância
    da chegada. **Pousáveis** lista todo corpo pousável com sua vida e seus materiais. **Biologia** lista os
    corpos com vida e mostra só a vida deles, então um corpo pousável só com materiais não aparece.
    **Minerais** lista os corpos com materiais na superfície e mostra só os materiais deles; um corpo com
    vida e materiais é listado, com a contagem de biologia ainda no título, mas sem os gêneros. Pousáveis é o
    padrão. A escolha é lembrada.
  - Na vista Biologia, uma segunda escolha entre duas diz quais corpos com vida deixar de fora. A escolha é
    lembrada. Condições de que o Katey não tem registro nunca são ocultadas.
  - **Toda a vida** lista todos.
  - **Pular só bactérias** deixa de fora os corpos cuja vida é só de bactérias: pelos gêneros do scanner
    depois de mapeados, ou, antes disso, um único sinal com pelo menos quatro em cinco chances de ser
    bactéria.

O painel escolhe sozinho entre a rota e os corpos. Ele mostra os corpos desde o momento em que você chega, com
uma linha acima deles sobre a rota: o próximo salto, quantos saltos restam e onde a rota termina. Ele muda
para a rota quando o drive carrega para saltar e a mantém até o momento em que você chega à próxima estrela.
Em supercruzeiro, uma carga só pode ser um salto, então o painel muda na hora; no espaço normal, a mesma carga
pode ser para o supercruzeiro, então o painel espera a contagem regressiva do salto. O jogo apaga uma rota no
meio do último salto, e o painel continua mostrando esse salto até ele ser feito. O que você mirou não move o
painel: o jogo mira o próximo salto sozinho a cada chegada ao longo de uma rota, então um sistema mirado não
diz nada sobre se você está explorando aqui. A seção útil está à vista sem um clique.

Mire em um sistema que não está na sua rota traçada e o jogo refaz a rota por ele, mas não reescreve o arquivo
da rota na hora. O painel e a página Rota dizem "Rota refeita por" esse sistema, com os saltos restantes
tirados da contagem do próprio jogo em vez do arquivo desatualizado, até o arquivo se atualizar.

**Destacar** move o painel para uma janela própria que fica por cima das outras janelas, para um segundo
monitor ou uma captura de transmissão. Uma faixa de uma linha fica na janela principal com o progresso do
escaneamento e estes controles:

- **Trazer o painel de volta** o acopla de novo. Fechar a janela do painel faz o mesmo.
- **Redefinir posição** coloca a janela do painel de volta na tela principal, no tamanho padrão. Fora isso,
  ela lembra onde você a deixou e o tamanho que você deu a ela.
- **Sobreposição** torna a janela quase transparente, com texto claro, para que ela possa ficar sobre um jogo
  rodando em janela ou sem bordas. Um jogo em tela cheia exclusiva cobre todas as outras janelas, inclusive
  esta.
- **Cliques atravessam** deixa os cliques passarem pelo painel até o jogo. Enquanto está ligado, os botões,
  links e chaves do painel se escondem, já que nada nele pode ser pressionado, e a borda da janela também
  some, já que nada nela pode ser agarrado, então só o painel em si aparece sobre o jogo. Desligue na faixa
  para tê-los de volta. Funciona só no Windows.

## As páginas

Uma seção por página.

### Início

A página inicial: um bloco para cada seção com uma linha sobre o que ela contém, e um quadro dizendo o que o
plano contém e se o diário está sendo acompanhado. Cinco números correm pelo topo:

- **Materiais em falta**: quantos materiais ainda faltam ao plano.
- **Ganhos, 7 dias**: créditos ganhos na última semana de jogo.
- **Saltos restantes**: saltos que restam na rota, e se o combustível os cobre.
- **Corpos escaneados**: o progresso do escaneamento deste sistema.
- **Dados a bordo**: quanto valem os escaneamentos e as amostras que você ainda não vendeu, contados desde a
  sua última venda e zerados por uma morte. Corpos que um farol de navegação entregou a você não são
  contados: o jogo não paga nada por dados que ele mesmo deu, e a página do sistema também os avalia em nada.
  - Quando você completa uma amostra, o diário diz se alguém já havia registrado aquela espécie naquele
    corpo. Quando ninguém havia, o Katey conta o bônus de primeiro registro, cinco vezes o valor base, como
    provável. Outro comandante ainda pode vendê-la primeiro, então o número é uma probabilidade, não uma
    promessa.
  - Antes de qualquer amostra, o sinalizador de pegada do escaneamento serve no lugar. A vida é registrada a
    pé, então um corpo em que ninguém pisou não pode ter sido registrado por ninguém. O Katey conta o bônus
    como provável ali, e não onde outra pessoa pousou.
  - O seu próprio pouso não o tira. Em um corpo em que a sua pegada foi a primeira, uma espécie que você não
    amostrou continua contada em cinco vezes, até você vender os dados de exobiologia desse corpo. Depois
    dessa venda, o Katey não assume nada para o que você deixou sem amostrar ali, já que outro comandante
    pode ter pousado desde então. Uma espécie que você amostrou tem a palavra do próprio jogo, dada com a
    amostra, e mantém ou perde o bônus por ela.

A cor tem significado em todo lugar: graus de material, os três inventários, os quatro locais em que um módulo
se encaixa, as classes de estrela como o mapa da galáxia as pinta, e os vereditos pronto, parcial e faltando.
Barras de tem-contra-precisa ficam sob cada material que o plano pede, e a barra de combustível na página de
rota e no painel mostra o tanque com o que o próximo salto vai queimar em azul sobre o laranja do que fica,
como o medidor do próprio jogo desenha.

### Engenheiros

Todos os engenheiros, do mais próximo de você ao mais distante, com a distância até eles e se estão na sua
rota. Marque os engenheiros que você desbloqueou e pretende usar; as marcações guiam os avisos do plano e tudo
o que a chave no topo da página restringe.

**Mostrar só itens dos engenheiros selecionados** é a chave mais importante do Katey. Ligada, o índice de
módulos e cada seletor de projeto, grau e experimental oferecem só o que os engenheiros que você marcou podem
rolar, e cada seletor de grau começa no grau que eles alcançam. Desligada, tudo é oferecido e os seletores de
grau começam no grau máximo. De um jeito ou de outro, o plano ainda avisa sobre uma entrada que ninguém que
você marcou pode rolar. A chave é lembrada.

Onde quer que o Katey nomeie um engenheiro, o nome é um link para a página dele. Com a chave desligada, todo
nome fica em texto simples. Com ela ligada, um engenheiro que você marcou fica no azul do HUD e um que você
não marcou fica esmaecido, então qualquer lista de engenheiros se lê de uma olhada.

Uma caixa de busca filtra a lista por nome, e as colunas ordenam.

### Um engenheiro

Um engenheiro: onde está e a que distância de você, e se é uma parada na sua rota. Cada projeto que ele
oferece é listado com o grau em que ele para e um botão **Adicionar** que o planeja. A página também diz quais
das suas entradas do plano ele pode rolar e quantos dos seus módulos estão armazenados no sistema dele.

### Módulos

Cada tipo de módulo que pode receber engenharia, em um grupo por local na nave: internos essenciais, internos
opcionais, pontos de fixação e suportes utilitários. Clique em um grupo para abri-lo, ou use **Expandir tudo**
e **Recolher tudo**. Com **Mostrar só itens dos engenheiros selecionados** ligado, um grupo lista só os tipos
em que os engenheiros que você marcou podem rolar um projeto. Os grupos abertos são lembrados.

### Um tipo de módulo

Um tipo de módulo, como o Frame Shift Drive ou um laser de feixe. Suas seções:

- **Projetos.** Cada projeto com o grau que seus engenheiros selecionados alcançam, todos que o oferecem e a
  que distância estão, um seletor de grau e um botão **Adicionar**. Os seletores seguem a chave dos
  engenheiros.
- **Experimentais.** Os efeitos experimentais que o tipo aceita e o que cada um custa. O grau 0 na linha de um
  projeto planeja só o experimental, para um módulo que já tem o projeto.
- **Variantes.** Cada tamanho e classe que o jogo vende, com os que você tem marcados.
- **Você tem.** Os módulos deste tipo que você tem, instalados ou armazenados, onde estão e como têm
  engenharia.
- **Nas suas montagens.** O que as suas montagens salvas pedem deste tipo.

### Um projeto

Um projeto em todos os tipos de módulo a que se aplica: o que uma rolagem custa em cada grau, quem o oferece
em quais tipos, e uma barra que o adiciona ao plano. Escolha o tipo de módulo, o grau e um experimental e
pressione **Adicionar**, ou pressione **Adicionar** na linha de um grau. Se os seus engenheiros selecionados
não alcançam o projeto enquanto a chave dos engenheiros está ligada, a barra diz isso em vez de oferecer um
grau que você não pode rolar.

### Materiais

Cada material com quantos você tem, quantos o plano precisa e quantos faltam. Uma chave por inventário mostra
ou esconde os materiais brutos, manufaturados e codificados, um filtro por nome restringe a lista e **Só em
falta** mantém apenas os que faltam. Tudo isso é lembrado.

Cada linha também aceita uma **reserva**, na coluna **Manter**: a quantidade mínima que você quer manter, e
uma chave **Trocar** que diz se o material pode ou não ser entregue em um comerciante. As reservas são salvas.

Sob a tabela, **Nos comerciantes de materiais** diz o que entregar para cobrir tudo o que falta e repor cada
reserva, troca por troca com a taxa, agrupadas por comerciante. O planejador gasta só o que você tem acima da
reserva e nunca um material cuja chave Trocar está desligada. Ele prefere as trocas que perdem menos valor e
saem das suas pilhas mais cheias, e diz por quê quando algo não pode ser coberto.

As taxas são as do jogo: seis por um subindo um grau, um por três descendo, e seis vezes mais ainda entre
famílias. Sob cada troca, uma segunda linha nomeia as famílias dos dois materiais como os comerciantes as
agrupam, como "família Ferro → família Carbono" ou "ambos na família Wake Scans", seguida de quanto os lotes
inteiros deixam sobrando. Cada material nomeado em uma troca é um link que leva à linha dele aqui. A mesma
lista fica no fim da gaveta do plano.

### Um material

Um material: quantos você tem contra o que o plano precisa, quais entradas do plano o gastam, o que mais no
jogo o usa, graus de projeto e experimentais igualmente, e quais corpos escaneados no seu sistema atual o têm.

### Desbloqueios

Tudo o que os corretores de tecnologia Guardiã e humana vendem, um corretor por vez, em grupos recolhíveis:
os módulos, armas, armas modificadas e caças Guardiões, e os módulos, armas e módulos pré-modificados do
corretor humano. Clique em um grupo para abri-lo ou fechá-lo, ou use **Expandir tudo** e **Recolher tudo**;
os grupos abertos são lembrados. Cada linha mostra o que o desbloqueio custa em materiais, por grau e
inventário, e em mercadorias que você compra em um mercado.

Marque um desbloqueio e ele entra no plano. Seus materiais contam nos totais do plano e nas trocas nos
comerciantes de materiais, e suas mercadorias aparecem sob "Comprar em um mercado" na gaveta. O selo de cada
linha diz em que pé ela está:

- **Pagável**, em laranja: o que o plano deixa no seu inventário o cobre.
- **Em falta**, em vermelho: não cobre, e a linha diz quanto falta. A linha fica esmaecida, mas ainda pode
  ser marcada; ela então vira uma entrada em falta como qualquer outra, e a gaveta diz o que coletar.
- **Planejado**: está no plano. Desmarque para tirá-lo.
- **Desbloqueado**, em verde: o diário registra que você o comprou, ou você comprou ou tem um módulo que ele
  desbloqueia, o que prova a compra mesmo quando ela aconteceu antes dos diários que o jogo guardou. Não pode
  ser marcado.

O plano vem primeiro. Marcar um desbloqueio reivindica seus materiais, então outro que precisava dos mesmos
pode ficar em falta; desmarcá-lo traz o outro de volta. Abaixo da tabela, os materiais e as mercadorias de
cada desbloqueio planejado são postos contra o que você tem.

Alguns desbloqueios pré-modificados, como o FSD V1 modificado, não são nomeados no diário, então o Katey não
sabe dizer se você os comprou; uma linha assim diz isso.

### Frota

Suas naves, suas montagens salvas e os módulos que você tem e que nenhuma montagem usa. Suas seções:

- **Naves.** Cada nave que você tem, onde está e se foi resgatada de um pacote; veja
  [Naves pré-montadas](#naves-pré-montadas). Itens cosméticos, como pinturas e kits de nave, ficam de fora em
  todo o Katey.
- **Montagens salvas.** Suas builds salvas, cada uma com um veredito contra tudo o que você tem sob a
  política de compartilhamento. Isso é posse, não a montagem de uma nave; abra a montagem para compará-la com
  uma nave encaixe por encaixe.
  - **Tem todos**: cada módulo que a montagem pede está em algum lugar da sua frota ou do armazenamento.
  - **Parcial**: alguns você tem, mas sem a engenharia completa.
  - **Faltando**, com quantos.

  Quando mais de um módulo que você tem serviria, o que já está instalado em uma nave do casco da montagem é
  nomeado primeiro, depois um no armazenamento, depois um em outra nave. Entre os que estão no mesmo lugar, o
  mais próximo de você vem primeiro, pelas coordenadas que o diário tem para o sistema dele, com o seu
  próprio sistema primeiro e um lugar que você nunca visitou por último.

  Uma caixa de colar recebe uma nova montagem do EDSY, e uma montagem pode ser removida aqui. Uma build
  colada com o mesmo nome de uma montagem salva para o mesmo casco substitui essa montagem em vez de ficar ao
  lado dela: o botão pergunta primeiro, e um segundo toque a substitui no lugar. O mesmo nome em outro casco
  é uma montagem nova.
- **Módulos próprios que nenhuma montagem usa.** Os módulos que você tem e que nenhuma montagem reivindica,
  sob uma **política de compartilhamento** que diz se os módulos podem passar entre naves, só entre naves do
  mesmo casco, ou nunca, e filtros por localização, local na nave e estado de engenharia.

A lista da frota e a lista de módulos armazenados são reescritas pelo jogo quando você atraca em algum lugar
com estaleiro ou Equipamentos, então elas são tão recentes quanto a sua última visita a um.

### Uma nave

Uma nave que você tem: seus módulos instalados, cada um com um seletor que planeja engenharia para aquele
exato módulo, e **Copiar para o EDSY**, que coloca o registro do próprio jogo sobre a nave na área de
transferência para colar na importação do EDSY.

O botão do seletor diz em que pé está o trabalho:

- **Adicionar ao plano** quando a escolha é um trabalho.
- **No plano** enquanto o plano já contém o projeto, o grau e o experimental escolhidos para aquele encaixe.
- **Já tem essa engenharia** enquanto o módulo já os tem. Escolha um grau maior, outro projeto ou outro
  experimental e volta a ser um trabalho.

Um módulo que veio com uma nave de pacote é marcado **Módulo pré-montado**, porque a engenharia feita nele
fica com aquela nave. Para uma nave pré-montada, a página diz se ela ainda está como veio ou quantos encaixes
mudaram; essa linha abre em uma tabela do que cada encaixe alterado trazia e contém agora. Veja
[Naves pré-montadas](#naves-pré-montadas).

### Uma montagem

Uma build salva comparada com o que você tem. Suas seções:

- **Comparar com uma nave.** A montagem encaixe por encaixe contra uma das suas naves, começando pela que já
  corresponde melhor a ela; escolha outra na lista. Cada encaixe diz se precisa de compra, se precisa de
  engenharia e se os seus engenheiros selecionados podem fazê-la, e se um módulo que você já tem em outro
  lugar preencheria o encaixe no lugar, dentro de um local e de uma localização à sua escolha.

  **Planejar as mudanças de engenharia desta nave** acrescenta ao plano cada diferença de engenharia; um
  encaixe ao qual falta só o experimental vira uma entrada só de experimental. O botão conta só o que o plano
  ainda não contém, e se recolhe, dizendo isso, quando todas as mudanças estão planejadas.

  Cada entrada é carimbada com aquela nave e aquele encaixe, como a página da nave carimba as suas, para que
  as duas páginas concordem sobre o que já está planejado para aquela nave. A mesma mudança planejada para
  outra nave ainda conta como não planejada aqui. Os dois botões desta página reconhecem as entradas um do
  outro: a engenharia de um encaixe planejada como uma lacuna no que você tem é o mesmo trabalho que a
  mudança para esta nave, e a página da nave conta a entrada de uma montagem para o casco dela do mesmo
  jeito.
- **Contra tudo o que você tem.** Cada módulo que a montagem pede contra cada módulo que você tem, sob a
  política de compartilhamento da frota. Um módulo com o projeto certo em um grau menor, ou sem o
  experimental, é **parcial** em vez de faltando. O mesmo vale para um módulo no grau certo que ainda não foi
  rolado até o efeito completo; o texto de engenharia dele diz até onde foi, como "Reforçado G3, 25%", e
  planejar a lacuna pede só as rolagens ainda devidas nesse grau. Um módulo com engenharia feita antes de o
  jogo guardar esse número é lido como completo.

  O status é colorido pelo lugar em que o módulo está:

  - verde: instalado em uma nave deste casco;
  - azul: no armazenamento;
  - amarelo: instalado em uma nave de outro casco;
  - laranja: você o tem, mas ele precisa de uma mudança, ou está em dúvida;
  - vermelho: faltando.

  Um módulo que veio com uma nave de pacote não pode sair dela, então conta só para montagens do casco
  daquela nave e é marcado **pré-montado** quando conta. **Planejar as lacunas no que você tem** faz por
  esta lista o que o botão acima faz por uma nave, e se recolhe do mesmo jeito. As duas contagens diferem de
  propósito: um encaixe pode ser uma mudança para esta nave enquanto um módulo que você tem em outro lugar já
  o cobre.

Um módulo é correspondido onde quer que esteja em um encaixe do mesmo tipo, então um módulo movido para outro
compartimento opcional ou ponto de fixação ainda conta. **Copiar para o EDSY** coloca a montagem na área de
transferência na forma que o EDSY importa: a exportação que você colou, exatamente como colou, para que nada
que o EDSY escreveu se perca no caminho de volta.

### Sistema

Para o sistema em que você está: cada corpo que você escaneou, nesta visita ou em uma anterior que o seu
histórico de diário contém, com um selo **escaneado em** dando o dia quando foi em uma visita anterior.
Quando a consulta de sistemas é permitida em Ajustes, os corpos que o Spansh conhece e você não escaneou
entram na lista com um selo **do Spansh**. Eles vêm com suas classes, condições, sinais e gêneros conhecidos.
A vida deles é avaliada como a sua; a cartografia deles não vale nada para você até você mesmo os escanear.
Eles não dizem nada sobre descoberta, mapeamento ou pouso, então nenhum bônus de primeiro de qualquer tipo é
assumido para eles. Para um sistema que você visitou antes, a página diz onde ele fica e quando você esteve
lá.

Acima dos corpos, três linhas resumem o sistema.

- **Dados de exploração não vendidos** é o que os dados deste sistema ainda a bordo pagariam, com os bônus
  contados onde se aplicam, e depois quanto disso é por ser o primeiro. Depois que algum dado deste sistema
  foi vendido, a linha termina com o que você recebeu por ele. Uma venda de um só sistema é tomada como o
  jogo a informou; uma venda de vários é dividida entre eles pela estimativa do Katey para cada um.
- A segunda linha detalha o número. A cartografia aparece como escaneada sem bônus, depois com o bônus de
  primeira descoberta onde um corpo era seu para descobrir. A exobiologia aparece pelos gêneros do scanner,
  "cerca de" até cada gênero ter uma espécie amostrada, depois com o bônus de primeiro registro onde ninguém
  havia pisado.
- A terceira diz quantos corpos ninguém havia descoberto antes de você, em quantos você pousou e quantos
  corpos pousáveis ainda não receberam a pegada de ninguém, você incluído.

Os corpos são listados estrela por estrela: cada estrela, nomeada pela classe e por ser ou não coletável, é
seguida pelos corpos que a orbitam, do mais próximo ao mais distante. Ordenar por uma coluna põe a lista
inteira nessa ordem em vez disso. Cada coluna lembra a própria direção, então voltar a uma a encontra como
você a deixou; isso vale para toda tabela do Katey.

Cada corpo mostra seus sinais e materiais de superfície, depois seus gêneros do mais valioso ao menos, com as
previsões de espécie e as estimativas de pagamento. Onde ninguém pisou no corpo, cada estimativa traz entre
parênteses o número com o bônus de primeiro registro, como "2,1M (10,5M) Cr". Um gênero fica violeta depois
da primeira amostra, azul depois da segunda e verde quando as três foram colhidas. A etapa é lembrada quando
você volta ao sistema e depois de vender, como o jogo a guarda. Começar outra espécie antes da terceira
amostra perde a parcial, como faz o amostrador. Sob os gêneros está o número de exobiologia do próprio corpo,
"cerca de" até cada gênero nele ter uma espécie amostrada. Quando uma espécie ali tem as três amostras, uma
linha em verde sob esse número diz o que você coletou no corpo, e no que isso se transforma com o bônus de
primeiro registro.

O cartão e os selos de cada corpo destacam o que importa antes de você se comprometer com ele.

- A moldura do cartão diz o que o corpo tem: verde para vida, azul para materiais de superfície; um corpo com
  os dois fica verde. O cartão de uma estrela tem moldura laranja e traz um selo **estrela**, com
  **coletável** em verde ou **não coletável** em amarelo ao lado.
- Três vagas de status, sempre na mesma ordem, cada uma vinda dos sinalizadores que o jogo grava com cada
  escaneamento: violeta para descoberta, azul-petróleo para mapeamento, verde para pegada. Uma pílula tem três
  aparências, então um corpo fresco são pílulas sólidas, um corpo que você trabalhou são palavras coloridas e
  um corpo que outros trabalharam é cinza, e você lê qual é qual sem ler as palavras.
  - Sólida com uma linha embaixo, o bônus ainda está aberto: **não mapeado**, que é onde está o bônus de
    mapeamento, ou **sem pegada**.
  - Contornada com as palavras na cor da vaga, o bônus é seu: **descoberto por você**, **mapeado por você** ou
    **pegada sua**.
  - Contornada com palavras em cinza, outra pessoa o levou: **descoberto**, **mapeado**, **com pegada**.
- A descoberta nunca está aberta, já que você escaneou o corpo. O jogo credita uma primeira descoberta quando
  os dados são vendidos, então **descoberto por você** permanece até lá e depois.
- Um primeiro continua seu quando você volta. Depois que você vendeu os dados de um sistema, os sinalizadores
  do jogo informam o seu próprio registro, então o Katey mantém como seu um corpo que você descobriu ou mapeou
  primeiro, seja como for que um escaneamento posterior o leia. Um corpo em que você pisou primeiro é seu a
  partir do pouso. Um corpo que você escaneou mas nunca vendeu toma os sinalizadores posteriores como vêm, já
  que outra pessoa vender primeiro é o que eles então significam.
- Uma estrela não tem vaga de mapeamento nem de pegada, e um corpo em que ninguém pode pousar não tem vaga de
  pegada. Onde outra pessoa foi a primeira mas você também mapeou ou pousou, uma linha discreta sob o cartão
  diz **Mapeado por você.** ou **Você pousou aqui.**
- Em um sistema povoado a pegada não pode ser reivindicada e o sinalizador do jogo não diz nada, então o Katey
  trata todo corpo ali como pisado. Um corpo que veio do Spansh diz **descoberta desconhecida**, **mapeamento
  desconhecido** e **pegada desconhecida**.
- Selo verde **pousável**: uma nave pode pousar ali.
- Gravidade, temperatura, atmosfera e as notas de superfície aparecem na linha do título ao lado da
  distância, que quebra conforme precisa, para que um cartão fique curto. A gravidade fica amarela a partir
  de 1,0 g com um selo **gravidade alta**, e vermelha a partir de 1,5 g com **gravidade extrema**: com essa
  atração, uma nave pesada pode não conseguir frear a descida, então aproxime-se devagar e nivele cedo.

**Copiar nome** coloca o nome do sistema na área de transferência para o mapa da galáxia. Todo nome de sistema
em qualquer lugar do Katey é um link para esta página.

As previsões de espécie vêm de onde cada espécie já foi encontrada antes. Antes de um corpo ser mapeado, um
sinal de gênero é comparado com a atmosfera, o tipo, o vulcanismo, a gravidade, a temperatura e a pressão
dele; depois de mapeado, a lista de gêneros do próprio scanner é usada.

Até então, o número de exobiologia do corpo é uma previsão, marcada "provavelmente":

- cada gênero que pode viver nas condições do corpo é ponderado pela frequência com que foi encontrado em tais
  condições;
- esse pagamento ponderado é multiplicado pelo número de sinais biológicos;
- uma linha diz quantos gêneros poderiam viver ali e nomeia os três mais prováveis, cada um com suas chances,
  sua espécie mais provável e seu pagamento esperado antes de qualquer bônus. Os sinais podem vir de qualquer
  gênero que as condições permitam, não só desses três.

A contagem de sinais aparece como um selo. Uma espécie que ninguém registrou em um corpo paga cinco vezes o
valor base. O diário não sabe dizer se alguém registrou, então, quando ninguém havia mapeado o corpo, o Katey
toma o bônus como provável e mostra o número vezes cinco, com o base ao lado. O painel classifica os corpos
por esse número, conhecido ou previsto, bônus incluído, depois pela contagem de sinais. A coluna **Pousável**
da página do sistema ordena do mesmo jeito, com os corpos pousáveis primeiro.

### Rota

Duas seções:

- **Rota do jogo**, a rota que o jogo traçou, estrela por estrela, com onde você está primeiro em cada linha.
  Cada parada diz se é **novo para você**, **visitado** ou **pesquisado** por um honk ou um farol de
  navegação, a distância desde o início e o combustível esperado na chegada.
  - O sistema que você mirou é sempre o próximo salto. Mirado sem uma rota, é uma rota de um salto. Mirado
    mais adiante na rota, as paradas entre eles caem fora. Mirado fora da rota, a página mostra o salto único
    até ele e uma linha dizendo onde a rota traçada retoma e quantos saltos restam. A distância do alvo vem do
    seu histórico ou da rota do Spansh, e aparece como 0 quando nenhum dos dois conhece o sistema.
  - A projeção de combustível leva a leitura do tanque do jogo pelas pernas adiante com o consumo da própria
    nave, pela equação do próprio jogo a partir das massas, da carga e dos números do drive que o diário
    informa. Ela está lá desde o primeiro momento e diz quantos saltos restam antes de você precisar coletar,
    até qual estrela coletável coletar, e quanto cada parada custa. Cada salto é custeado pelo combustível
    que resta para ele, então a nave que fica mais leve conforme queima é acompanhada.
  - A carga são as toneladas a bordo agora, e a projeção é refeita a cada atualização do diário, então uma
    venda ou uma coleta no caminho a muda no momento em que o jogo a informa. Uma nave para a qual o diário
    não deu as massas do loadout recorre ao consumo aprendido com seus saltos recentes. Uma partida de uma
    estrela de nêutrons ou de uma anã branca conta como um salto impulsionado, e um impulsionador Guardião
    como alcance extra.
  - A barra do tanque acima das paradas é desenhada como o medidor do próprio jogo: laranja é o que fica no
    tanque depois do próximo salto, azul é o que esse salto queima, e o número azul ao lado da leitura é o
    mesmo consumo em toneladas.
  - Depois que você saltou, o jogo tira o combustível na hora e o tanque o mostra, então, durante o salto, a
    parada a que você está chegando mostra a leitura do tanque e a barra azul passa para o salto seguinte.
- **Rota do Spansh.** Com o traçador ligado em [Ajustes](#ajustes), esta seção pede ao traçador de rotas do
  Spansh uma rota de onde você está até qualquer sistema, para uma nave que você tem ou uma montagem salva. O
  traçado usa as massas, os tanques e os números do drive da própria nave: os do diário para uma nave, os da
  exportação do EDSY para uma montagem. Impulsos de nêutrons, injeções de FSD, coletas em estrelas secundárias
  e reabastecimento em toda estrela coletável são opções.
  - O seletor começa na nave que você está pilotando. Clicar nele abre um quadro com uma caixa de busca vazia
    e a lista, nesta ordem: a nave ou montagem que você havia escolhido primeiro, a nave que você está
    pilotando em segundo, depois as montagens salvas para o casco escolhido, as suas naves e as outras
    montagens. Cada uma mostra seu alcance com tanque cheio, com a nave que você está pilotando na cor de
    destaque. Digite para restringir a lista por nome ou casco. Clique em uma entrada, ou pressione Enter para
    a primeira, para escolhê-la. Nada mais muda a escolha.
  - O traçador é informado de que o drive está supercarregado quando você tomou um impulso de cone de jato
    desde o último salto, ou quando está em uma estrela de nêutrons com os impulsos de nêutrons ligados, para
    que o primeiro salto seja planejado como impulsionado.
  - Os números de drive do Katey são as mesmas tabelas com que o próprio site do Spansh trabalha, drives SCO e
    Mk II incluídos, então uma rota pedida pelo Katey corresponde a uma feita colando a build no site.
  - O destino, o roteamento e as opções são salvos conforme você os muda. A nave ou montagem e a carga ficam
    como você as definiu enquanto a rota está guardada, entre páginas e reinícios, até você pressionar
    **Esquecer a rota**. Então a página abre de novo na nave que você está pilotando, com a carga que ela leva
    preenchida, que você pode sobrescrever. Trocar de nave ou montagem deixa a carga em paz.
  - A rota lista cada ponto de rota com seu salto, a distância restante, o combustível que o traçador espera
    na chegada e se é uma estrela de nêutrons ou uma parada de reabastecimento. Ela acompanha os seus saltos
    para dizer qual é o próximo ponto de rota, copia esse nome para o mapa da galáxia e é guardada entre
    execuções.
  - Saia da rota para um desvio, para coletar digamos, e o último ponto de rota que você alcançou desde que
    a rota foi feita é marcado **último conhecido**, enquanto o seguinte continua sendo o próximo ponto de
    rota, então **Copiar** ainda dá a você a estrela de nêutrons a mirar. Uma rota feita de um lugar onde você
    não está não tem ponto de rota último conhecido até você alcançar uma das paradas dela, mesmo que tenha
    passado por algumas delas antes.

### Estatísticas

Seu histórico de diário lido como um livro-caixa. Suas seções:

- **Receita.** Um gráfico de anel do que você ganhou por atividade, de recompensas a exobiologia, com uma
  chave por atividade que redesenha o gráfico sem ela, e um seletor de período de todo o período até a última
  semana.
- **Exobiologia.** O que está a bordo e não vendido: amostras, quantas provavelmente são primeiro registro, e
  seu valor com e sem o bônus. Depois o que os dados orgânicos renderam com e sem o bônus de primeiro
  registro, e cada espécie vendida.
- **Exploração.** O que está a bordo e não vendido: sistemas, corpos escaneados e mapeados, e seu valor de
  cartografia estimado. Depois as vendas do período ao lado dos números de toda a carreira do jogo.
- **Combate, comércio e mineração, missões e outros, Powerplay.** Os números próprios de cada atividade no
  período, ao lado dos números de toda a carreira do jogo para ela.
- **Alas e tripulações.** Tempo voado com outros e o que o jogo marcou como compartilhado, por tipo. Cada
  comandante com quem você voou é listado com o tempo juntos e o que foi compartilhado enquanto ele estava
  presente. Cada missão de ala aceita é listada com quem estava na ala na hora e como ela está. Voar em ala
  enquanto você explora sozinho não compartilha nada, então não conta nada aqui. O diário não diz se uma
  missão de ala veio do quadro ou de um companheiro de ala, então as duas são listadas.
- **Números de toda a carreira.** Cada total acumulado que o jogo informa, tempo jogado incluído. Uma chave
  lembrada também lista os números zerados; ela vem desligada. O seletor de período se esconde aqui, já que
  estes são os totais do próprio jogo.

### Ajustes

- **Atualizações.** A versão que você está usando e, com a chave do GitHub ligada, se existe uma versão mais
  nova. O Katey pergunta uma vez ao iniciar e sempre que você pressiona **Verificar agora**, em segundo plano,
  então iniciar não demora mais. Quando há uma versão mais nova, **Abrir a página da versão** a abre no seu
  navegador; nada é baixado nem instalado por você.
- **Tamanho do texto.** Quão grande as janelas desenham tudo, como porcentagem do tamanho padrão, para quem
  precisa de letras maiores. Texto e espaçamento crescem juntos, então as páginas mantêm a forma; destaque o
  painel ou alargue a janela se uma página não couber mais. A escolha é lembrada. Se a tela for pequena demais
  para as páginas ampliadas, a janela para na borda da tela e as páginas rolam para os lados.
- **Pasta de diários.** A pasta que o Katey acompanha. **Detectar** procura nos lugares habituais,
  **Procurar** escolhe uma, ou digite um caminho; o acompanhamento passa para a pasta na hora e reproduz seu
  histórico. A linha de status diz o que o acompanhamento está fazendo.
- **Backup do diário.** Uma pasta para a qual toda a pasta de diários é copiada, só o que é novo ou mudou,
  quando o Katey fecha e sempre que você pressiona **Fazer backup agora**. Aponte para uma pasta que o
  OneDrive ou outra nuvem sincroniza e os seus diários saem da máquina com ela.
- **Dados salvos.** A pasta em que o Katey guarda os próprios dados; veja [Seus dados](#seus-dados). Escolha
  outra e os dados se movem para lá na hora. Se a pasta já contém um arquivo de dados do Katey, ele é
  carregado; senão, os dados atuais são gravados lá, e os dados do lugar antigo ficam como estavam. Duas
  máquinas apontadas para uma mesma pasta sincronizada compartilham um só plano, um só conjunto de
  engenheiros e um só conjunto de montagens.
- **Conexões de saída.** O único lugar que decide o que o Katey pode enviar pela rede, uma chave por tipo de
  pedido, todas desligadas até você marcá-las. Há três tipos. Dois vão para o [Spansh](https://spansh.co.uk/):
  pedir uma rota quando você pressiona **Traçar**, e perguntar sobre os corpos do sistema em que você acabou
  de chegar. O terceiro vai para o [GitHub](https://github.com/temetvince/elite-dangerous-katey/releases) e
  pede a última versão publicada do Katey. As chaves são lembradas.

## Trabalhando com o EDSY

O [EDSY](https://edsy.org/) é o construtor de naves que a maior parte da comunidade usa, e o Katey fala as
formas de importação e exportação dele nas duas direções.

- **Uma build para o plano.** No EDSY, abra a build e escolha **Export**, depois o texto Loadout no estilo do
  diário. Cole-o na caixa de importação da gaveta do plano e pressione **Ler exportação**. Cada módulo com
  engenharia vira uma entrada no seu projeto, grau e experimental; desmarque os que não quiser e pressione
  **Adicionar ao plano**.
- **Uma build como montagem salva.** Cole o mesmo texto na caixa de montagens salvas da página
  [Frota](#frota). A montagem é então comparada com as suas naves e os módulos que você tem. Colar uma
  exportação mais nova de uma build que você já salvou, mesmo nome e mesmo casco, substitui a montagem salva
  depois que você confirma.
- **Uma nave ou uma montagem de volta ao EDSY.** **Copiar para o EDSY** na página de uma [nave](#uma-nave) ou
  de uma [montagem](#uma-montagem) coloca um Loadout na área de transferência. No EDSY, escolha **Import** e
  cole.

## Naves pré-montadas

Uma nave resgatada de um pacote na loja do jogo vem com módulos que o Katey marca como pré-montados enquanto
eles ficam nos encaixes em que vieram. Comprar, retirar, trocar, armazenar ou vender para um encaixe encerra a
marca, seja qual for o valor que o jogo mostre para o recém-chegado, assim como um módulo comprado do mesmo
tipo tomar o encaixe. Os módulos de pacote do jogo não podem ser armazenados, movidos para outra nave nem
vendidos. Fazer engenharia em um módulo assim é engenharia que fica com aquela nave. O Katey marca isto:

- A página [Frota](#frota) marca uma nave resgatada como **Pré-montada**.
- A página de uma [nave](#uma-nave) marca cada módulo assim como **Módulo pré-montado** ao lado do seletor de
  engenharia, e a comparação de uma [montagem](#uma-montagem) avisa onde o encaixe a modificar contém um.
- A página de uma nave também diz se uma nave pré-montada está **Ainda como resgatada**, de modo que vendê-la
  e resgatá-la de novo não perde nada, ou quantos encaixes mudaram desde então. A linha abre em uma tabela do
  que cada encaixe alterado trazia e contém agora. Um módulo pré-montado trocado por um comprado do mesmo tipo
  conta como mudança, já que o comprado pode sair da nave.

Uma nave pré-montada que você nunca implantou não existe para o jogo, então ela não pode aparecer na frota.

## O que o diário não pode dizer ao Katey

O Katey sabe só o que o jogo grava. Alguns limites decorrem disso:

- Se outra pessoa descobriu um sistema só se sabe depois que você escaneia um corpo nele. Antes da chegada, o
  Katey só pode dizer se *você* esteve lá.
- O diário não registra quem descobriu ou mapeou um corpo primeiro, nem quem pousou nele primeiro, só se você
  foi o primeiro.
- A lista da frota e a lista de módulos armazenados são reescritas quando você atraca em algum lugar com
  estaleiro ou Equipamentos, então elas são tão recentes quanto a sua última visita a um. O projeto e o grau
  de um módulo armazenado são registrados, mas não o efeito experimental dele, que o verificador de montagens
  informa como incerto.
- O diário não diz se uma missão de ala veio do quadro de missões ou de um companheiro de ala.
- Os corpos são mostrados para o sistema em que você está; um sistema anterior mostra onde fica e quando você
  esteve lá.
- Um corpo que você escaneou em uma visita anterior já está nos seus dados, então o jogo não grava nada quando
  o honk o encontra de novo. O Katey lembra cada corpo que você escaneou em qualquer visita dentro do seu
  histórico de diário e o lista de novo quando você volta, marcado com o dia em que foi escaneado. Um corpo
  escaneado antes do seu arquivo de diário mais antigo não pode ser recuperado assim. Quando o honk informa
  que o sistema inteiro já é conhecido, o Katey conta todos os corpos como escaneados, mas só pode listar os
  que tem, ou os que o Spansh conhece quando essa chave está ligada.
- Os corpos marcados **do Spansh** são o que o Spansh conhece, o que pode ser mais antigo do que o sistema é:
  um levantamento de anos atrás, ou só parte de um sistema. O Katey pergunta sobre um sistema uma vez, quando
  você chega nele, e guarda a resposta pela execução; se o Spansh não puder ser alcançado, ele espera cinco
  minutos antes de perguntar sobre esse sistema de novo.

## Seus dados

O Katey guarda um arquivo: o plano, a seleção de engenheiros, as montagens salvas, as reservas de materiais, a
pasta de diários, a pasta e a chave de backup, as chaves de conexões de saída e a última rota traçada, a
posição e os ajustes do painel, e cada escolha de vista lembrada. Por padrão, ele fica na sua pasta de dados
locais de aplicativos, em `Katey\state.json`. A caixa **Dados salvos** em [Ajustes](#ajustes) o move para
qualquer lugar, uma pasta sincronizada inclusive. Quando uma atualização aposenta ou renomeia um ajuste, o
Katey descarta o valor antigo e parte do padrão, para que o arquivo nunca carregue ajustes que nada lê.

O Katey lê os arquivos de diário localmente e não envia nada a lugar nenhum a menos que você permita em
**Conexões de saída** em [Ajustes](#ajustes), onde tudo começa desligado. Existem três tipos de pedido. Dois
vão para o [Spansh](https://spansh.co.uk/): quando você pressiona **Traçar**, dois nomes de sistema e os
números de salto da nave; e, ao chegar a um sistema, o id desse sistema, que traz de volta os corpos que o
Spansh conhece lá. O terceiro vai para o GitHub quando o Katey inicia e quando você pressiona **Verificar
agora**: um pedido da última versão publicada do projeto, que não leva nada seu. Cada pedido informa o Katey,
sua versão e o endereço do projeto, como o Spansh pede às ferramentas que usam o serviço dele. O backup do
diário copia arquivos só para a pasta que você escolher.

O Katey também guarda um log, `katey.log`, ao lado do `state.json` padrão na sua pasta de dados locais de
aplicativos. Ele registra o que o Katey fez e o que deu errado: início e parada, a pasta de diários que
acompanhou, os pedidos que fez e as falhas que encontrou, e o motivo se ele algum dia fechar sozinho. Se o
Katey fechar inesperadamente, esse arquivo diz por quê, e é o que se deve enviar com um relato de bug. Ele é
rotacionado ao passar de um megabyte, então fica pequeno.

O log contém nomes de sistemas estelares e de naves do seu diário e nada mais sobre você: nenhum nome de
comandante, nenhum crédito, nenhuma posição e nenhum nome de pasta. As mensagens do próprio Katey nunca trazem
um caminho. Qualquer outra coisa que chegue ao log, um erro do Windows digamos, fica só com um nome de
arquivo, com os nomes da sua conta e da sua máquina substituídos, para que o arquivo possa ser compartilhado
como está.

Para um bug que precise de mais detalhe, inicie o Katey como `katey.exe --verbose` ou como
`katey.exe --log-level debug`. O log então também registra as páginas abertas, as decisões do painel e as
consultas feitas; `--log-level warning` guarda só os problemas. Tudo volta ao normal na próxima vez que você
iniciar sem a opção. `katey.exe --pseudo-loc` mostra cada palavra mais longa, acentuada e entre colchetes, que
é como um tradutor ou um relato de bug encontra texto cortado ou que não passou pelas tabelas.

Se o arquivo de diário do dia sumir enquanto o jogo roda, o jogo o grava de novo do começo, e tudo o que ele
continha se perde para o jogo e para o Katey. O backup guarda sua cópia anterior de um arquivo assim ao lado
do novo, nomeada com um sufixo `rewritten`. Até o seu próximo salto, o Katey toma a sua posição do arquivo de
rota do jogo, cuja primeira parada é o sistema em que você está, então o painel e a página de rota continuam
certos; os corpos do sistema ficam vazios até você escanear de novo.
