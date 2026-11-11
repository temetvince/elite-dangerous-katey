# Manuel de Katey

Aussi disponible en [English](MANUAL.md), [Deutsch](MANUAL.de.md), [Español](MANUAL.es.md),
[Português (Brasil)](MANUAL.pt-BR.md) et [Русский](MANUAL.ru.md).

Katey est un compagnon de bureau pour Elite Dangerous. Il lit les fichiers journaux que le jeu écrit sur votre
propre machine et vous suit pendant que vous jouez. Il planifie l'ingénierie d'après les matériaux que vous
détenez réellement. Il compare vos builds de vaisseau enregistrés aux vaisseaux et aux modules que vous
possédez. Il suit votre itinéraire et le système où vous êtes, et lit votre historique comme un registre. Rien
ne quitte votre machine tant que vous ne l'autorisez pas dans les Paramètres, où chaque connexion sortante
est désactivée au départ.

## Sommaire

- [Démarrage rapide](#démarrage-rapide)
- [La fenêtre](#la-fenêtre) : la [barre du haut](#la-barre-du-haut), la [barre du bas](#la-barre-du-bas),
  la [recherche](#recherche), [le tiroir du plan](#le-tiroir-du-plan) et
  [le panneau en direct](#le-panneau-en-direct)
- [Les pages](#les-pages) : [Accueil](#accueil), [Ingénieurs](#ingénieurs), [un ingénieur](#un-ingénieur),
  [Modules](#modules), [un type de module](#un-type-de-module), [une modification](#une-modification),
  [Matériaux](#matériaux), [un matériau](#un-matériau), [Déblocages](#déblocages), [Flotte](#flotte),
  [un vaisseau](#un-vaisseau), [une configuration](#une-configuration), [Système](#système),
  [Itinéraire](#itinéraire), [Stats](#stats) et [Paramètres](#paramètres)
- [Travailler avec EDSY](#travailler-avec-edsy)
- [Vaisseaux pré-équipés](#vaisseaux-pré-équipés)
- [Ce que le journal ne peut pas dire à Katey](#ce-que-le-journal-ne-peut-pas-dire-à-katey)
- [Vos données](#vos-données)

## Démarrage rapide

1. **Lancez Katey.** La version publiée est un exécutable unique ; rien n'est à installer. Un seul Katey
   tourne à la fois : en lancer un second affiche un avis et laisse le premier tranquille, car deux copies
   écraseraient mutuellement leurs données enregistrées. Sous Windows, le dossier des journaux du jeu est
   trouvé tout seul. Si la page Paramètres dit qu'aucun dossier n'a été trouvé, indiquez-lui le dossier qui
   contient les fichiers nommés `Journal.*.log`, en général sous
   `Saved Games\Frontier Developments\Elite Dangerous`.
2. **Laissez-le rattraper.** Le premier lancement rejoue tout votre historique de journal, le fichier le
   plus ancien d'abord, et montre sa progression en haut de la fenêtre et dans le panneau : combien de
   fichiers sont lus, avec une barre qui se remplit fichier par fichier. Rien de direct n'apparaît avant la
   fin, vous ne voyez donc jamais un chiffre pris à mi-chemin. Un historique de plusieurs années prend
   quelques secondes.
3. **Cochez vos ingénieurs.** Ouvrez **Ingénieurs** dans la barre du haut et cochez les ingénieurs que vous
   avez débloqués et comptez visiter. Activez **N'afficher que ce que proposent les ingénieurs
   sélectionnés** en haut de cette page et chaque liste et chaque sélecteur de Katey se restreint à ce que
   ces ingénieurs peuvent appliquer, chaque sélecteur de grade démarrant au grade qu'ils atteignent.
4. **Planifiez de l'ingénierie.** Ouvrez **Modules**, choisissez un type de module, un grade et un effet
   expérimental sur la modification voulue, puis appuyez sur **Ajouter**. Ou ouvrez le tiroir **Plan**
   depuis la barre du bas et collez un build EDSY dans sa case d'import ; chaque module modifié du build
   devient une entrée.
5. **Lisez le plan.** Le tiroir liste chaque entrée avec qui peut l'appliquer, chaque matériau dont le plan a
   besoin face à ce que vous détenez et, en bas, les échanges à faire chez les négociants en matériaux pour
   couvrir ce qui vous manque. **Copier la liste de courses** met la liste de courses dans le presse-papiers.
6. **Volez.** Le panneau en direct à droite suit le jeu : votre position, l'itinéraire, le carburant, les
   corps qui valent un scan dans ce système, ceux qui portent un matériau qui vous manque étant marqués.
   **Détacher** le déplace dans une fenêtre à part qui reste au-dessus du jeu.
7. **Comparez un build à votre flotte.** Ouvrez **Flotte**, collez un build EDSY dans la case des
   configurations enregistrées et ouvrez la configuration. Elle est comparée emplacement par emplacement au
   vaisseau qui lui correspond le mieux et à tout ce que vous possédez, et un seul bouton planifie l'écart
   d'ingénierie.

Le reste de ce manuel dit ce que fait chaque partie de la fenêtre et comment en tirer le meilleur.

## La fenêtre

Katey est une seule fenêtre avec une page au milieu, un panneau le long du bord droit et deux barres. Elle
dessine sa propre barre de titre aux couleurs de l'application et porte le visage de Katey comme icône. Les
pages ne font jamais moins de 960 pixels de large, la largeur minimale de la fenêtre est donc celle-là plus
le panneau tant qu'il est ancré ; détachez le panneau et la fenêtre tient sur un écran de 1080 pixels de
large. Elle s'ouvre centrée sur l'écran principal, quel que soit l'écran où elle était la dernière fois.

### La barre du haut

La barre du haut est la barre de titre de la fenêtre : faites-la glisser pour déplacer la fenêtre,
double-cliquez dessus pour agrandir, et utilisez les trois boutons à son extrémité droite pour réduire,
agrandir et fermer. De gauche à droite, elle contient :

- **KATEY**, le logotype, qui ouvre la [page d'accueil](#accueil).
- **Retour** et **Suivant**, qui fonctionnent comme dans un navigateur et vous ramènent où vous aviez fait
  défiler.
- Les liens des pages : **Ingénieurs**, **Modules**, **Matériaux**, **Déblocages**, **Flotte**,
  **Itinéraire** et **Système**. Celui de la section où vous êtes est allumé : la page d'un ingénieur
  allume Ingénieurs et une modification allume Modules. Appuyez de nouveau sur un lien pendant que sa
  propre page est affichée et vous revenez à la page où vous étiez.

### La barre du bas

- **Langue**, le globe tout à gauche, montre la langue dans laquelle Katey est et ouvre la liste des langues
  livrées : anglais, allemand, espagnol, français, portugais (Brésil) et russe, chacune dans son propre nom,
  avec « Langue du système » en premier. Katey suit la langue de votre Windows jusqu'à ce que vous en
  choisissiez une ; le choix est mémorisé et toute la fenêtre change d'un coup. Les noms propres au jeu
  changent avec elle : matériaux, modifications, effets expérimentaux, types de module, objets des courtiers
  technologiques, classes de planète, atmosphères et volcanisme, et les chiffres à vie. Les noms d'espèces,
  de vaisseaux et de modules qui viennent de votre journal suivent la langue du client du jeu. Ce manuel
  existe dans chaque langue, avec un lien en haut.
- **Paramètres**, juste à côté, ouvre la page [Paramètres](#paramètres). Appuyez de nouveau pendant que les
  paramètres sont affichés et vous revenez à la page où vous étiez.
- **Stats**, juste à côté, ouvre la page [Stats](#stats) et, pressé de nouveau pendant qu'elle est
  affichée, vous ramène à la page où vous étiez. Il est ici et non dans la barre du haut pour que les
  liens des pages tiennent sur une ligne dans chaque langue.
- La **case de recherche** au milieu ; voir [Recherche](#recherche).
- **Plan** à droite ouvre et ferme le [tiroir du plan](#le-tiroir-du-plan). Le nombre qu'il porte est le
  nombre d'entrées du plan.

### Recherche

Tapez dans la case de recherche, ou appuyez sur **Ctrl K** pour y sauter, pour trouver un ingénieur, un type
de module, une modification, un matériau, un système que vous avez visité, l'un de vos vaisseaux ou une
configuration enregistrée. Les résultats montent au-dessus de la case à mesure que vous tapez. **Enter**
ouvre le premier résultat, les flèches passent de l'un à l'autre et **Escape** vide la case. Chaque résultat
ouvre sa propre page.

### Pages, liens et choix mémorisés

Chaque page décrit une chose ou liste des choses d'une même sorte. Chaque mention d'autre chose sur une page
est un lien qui ouvre sa page : un nom de système ouvre le système, le nom d'un ingénieur ouvre l'ingénieur,
une puce de matériau ouvre le matériau. Les petites étiquettes au-dessus du titre d'une page sont des liens
qui remontent vers l'index auquel elle appartient.

Trois mécanismes se comportent de la même façon sur chaque page, et chacun mémorise son état d'une session
à l'autre :

- **Le tri.** Cliquez sur l'en-tête d'une colonne pour trier le tableau selon elle ; cliquez de nouveau pour
  inverser le sens.
- **Les sections.** Une page longue montre une section à la fois, choisie par les onglets sous son titre.
- **Les groupes.** Une page qui regroupe les choses ouvre et ferme chaque groupe d'un clic, avec
  **Tout déplier** et **Tout replier** à côté.

Les filtres, les interrupteurs et l'état ouvert ou fermé de chaque pli sont mémorisés aussi, une page revient
donc comme vous l'avez laissée.

### Le tiroir du plan

Le plan est la liste de l'ingénierie que vous comptez faire. Le tiroir s'ouvre par-dessus n'importe quelle
page depuis le bouton **Plan** et contient :

- **Les entrées.** Chacune a :
  - un interrupteur pour la laisser hors des totaux sans la supprimer ;
  - un sélecteur de grade et, pour un type qui prend des effets expérimentaux, un sélecteur d'effet
    expérimental et l'interrupteur **Expérimental seul** ; un type qui n'en a aucun ne montre ni l'un ni
    l'autre ;
  - les jets attendus par grade ;
  - les ingénieurs qui peuvent l'appliquer, chacun un lien coloré selon vos coches comme partout dans Katey.
    Une entrée que personne parmi vos cochés ne peut appliquer porte un avertissement.

  L'interrupteur **Expérimental seul** transforme une entrée en une entrée qui ne coûte que les matériaux
  de l'effet expérimental, pour un module qui a déjà sa modification. Un déblocage de courtier technologique
  que vous avez coché sur la page [Déblocages](#déblocages) est une entrée aussi, avec ses matériaux fixes
  et les marchandises qu'il demande en plus sur un marché.

  Le plan suit ce que vous faites chez l'ingénieur :

  - chaque jet que le jeu rapporte sur un module de la modification et du type d'une entrée retire un jet de
    ce grade, ou l'efface quand le grade est achevé ;
  - l'effet expérimental quitte l'entrée dès qu'il est sur le module, quel que soit le grade atteint ;
  - une fois que le module est au grade cible sans plus rien à vouloir, l'entrée quitte le plan.

  Seuls les jets faits après l'ajout de l'entrée comptent, une ingénierie faite plus tôt ne retire donc
  jamais une entrée.
- **Les totaux.** Chaque matériau dont le plan a besoin, avec combien vous en détenez et combien il vous en
  manque, groupés en bruts, manufacturés et encodés. **Copier la liste de courses** met la liste dans le
  presse-papiers en texte brut.
- **Les échanges.** Ce qu'il faut céder chez les négociants en matériaux pour couvrir tout ce qui vous
  manque, échange par échange, groupés par négociant. Voir [Matériaux](#matériaux) pour la façon dont les
  échanges sont choisis.
- **L'import EDSY.** Collez un build exporté depuis EDSY, appuyez sur **Lire l'export**, décochez les
  modules que vous ne voulez pas et appuyez sur **Ajouter**. Voir
  [Travailler avec EDSY](#travailler-avec-edsy).

**Vider le plan** vide le plan après une seconde pression, pour qu'un faux geste ne puisse pas le perdre.

### Le panneau en direct

Le panneau suit le jeu le long du bord droit de la fenêtre. Il montre :

- **Où vous êtes** et ce que fait le vaisseau : amarré, en supercroisière, posé, et ainsi de suite.
- **L'itinéraire** : l'étape où vous êtes et les quelques suivantes, le reste étant résumé. La prochaine
  étoile ravitaillable et toute étape chez un ingénieur sont signalées. La distance parcourue s'affiche face
  au total et en pourcentage, avec la barre du réservoir telle que la dessine la
  [page Itinéraire](#itinéraire). Le tableau du carburant dit combien de sauts vous pouvez faire avant de
  devoir ravitailler, et avant quelle étoile ravitailler. Quand un tracé Spansh est conservé, le prochain
  point de passage s'affiche avec un bouton **Copier** pour la carte de la galaxie.
- **Ce système** : la progression du scan et les estimations de gains, avec chaque corps scanné qui porte
  des matériaux de surface ou des signaux biologiques. **Données d'exploration non vendues** est ce que les
  données de ce système encore à bord rapporteraient. Ce sont les scans que vous n'avez pas vendus, avec le
  bonus de première découverte là où un corps était à vous à découvrir, et les espèces que vous avez
  échantillonnées en entier et pas vendues, avec le bonus de premier enregistrement là où le jeu a dit que
  personne ne les avait enregistrées. La page du système ajoute ce qui vous a déjà été payé. Le titre et
  l'anneau passent au vert une fois chaque corps scanné.
  - Avant qu'un corps soit cartographié, sa vie tient en une ligne : combien de genres pourraient y vivre et
    ce qu'ils valent probablement, avec et sans le bonus de premier enregistrement. Une fois sondé, le corps
    liste ses genres, le plus riche d'abord. Chaque ligne est le genre, son espèce la plus probable avec ses
    chances ou son étape d'échantillonnage, et ce qu'il paie avec le bonus là où il s'applique. La ligne est
    violette après le premier échantillon, bleue après le deuxième et verte une fois les trois pris.
  - La gravité s'affiche en étiquette à partir de 1,0 g, en rouge à partir de 1,5 g.
  - Un choix à trois dit à quoi sert la liste, chacune les plus proches d'abord par distance depuis
    l'arrivée. **Atterrissable** liste chaque corps atterrissable avec sa vie et ses matériaux. **Biologie**
    liste les corps abritant de la vie et ne montre que leur vie, un corps atterrissable avec seulement des
    matériaux n'apparaît donc pas. **Minéraux** liste les corps avec des matériaux de surface et ne montre
    que leurs matériaux ; un corps avec de la vie et des matériaux est listé, son compte de biologie
    toujours dans le titre, ses genres non. Atterrissable est le choix par défaut. Le choix est mémorisé.
  - Dans la vue Biologie, un second choix à deux dit quels corps abritant de la vie laisser de côté. Le
    choix est mémorisé. Les conditions dont Katey n'a aucune trace ne sont jamais masquées.
  - **Toute vie** les liste tous.
  - **Ignorer les bactéries** laisse de côté les corps dont la seule vie est bactérienne : d'après les genres
    du scanner une fois cartographiés, ou avant cela un signal unique qui a au moins quatre chances sur cinq
    d'être une bactérie.

Le panneau choisit tout seul entre l'itinéraire et les corps. Il montre les corps dès votre arrivée, avec une
ligne au-dessus sur l'itinéraire : le prochain saut, combien de sauts restent et où l'itinéraire finit. Il
passe à l'itinéraire quand le moteur charge pour sauter et le garde jusqu'au moment où vous arrivez à l'étoile
suivante. En supercroisière une charge ne peut être qu'un saut, le panneau bascule donc aussitôt ; en espace
normal la même charge peut être pour la supercroisière, le panneau attend donc le décompte du saut. Le jeu
efface un itinéraire au milieu de son dernier saut, et le panneau continue de montrer ce saut jusqu'à ce qu'il
soit fait. Ce que vous avez ciblé ne déplace pas le panneau : le jeu cible tout seul le prochain saut à chaque
arrivée le long d'un itinéraire, un système ciblé ne dit donc rien sur le fait que vous exploriez ici ou non.
La section utile est affichée sans un clic.

Ciblez un système qui n'est pas sur votre itinéraire tracé et le jeu retrace son itinéraire en passant par
lui, mais il ne réécrit pas aussitôt son fichier d'itinéraire. Le panneau et la page Itinéraire disent
« Redirigé par » ce système, avec les sauts restants pris du compte du jeu lui-même plutôt que du fichier
périmé, jusqu'à ce que le fichier rattrape.

**Détacher** déplace le panneau dans une fenêtre à part qui reste au-dessus des autres fenêtres, pour un
second écran ou une capture de stream. Une bande d'une ligne reste dans la fenêtre principale avec la
progression du scan et ces commandes :

- **Ramener le panneau** l'ancre de nouveau. Fermer la fenêtre du panneau fait de même.
- **Réinitialiser la position** remet la fenêtre du panneau sur l'écran principal à sa taille par défaut.
  Sinon elle se souvient d'où vous l'avez laissée et de la taille que vous lui avez donnée.
- **Superposition** rend la fenêtre presque transparente avec un texte lumineux, pour qu'elle puisse se poser
  sur un jeu qui tourne en fenêtre ou en fenêtre sans bordure. Un jeu en plein écran exclusif couvre toute
  autre fenêtre, celle-ci comprise.
- **Clics traversants** laisse les clics traverser le panneau jusqu'au jeu. Tant que c'est activé, les
  boutons, liens et interrupteurs du panneau se cachent, puisque rien dessus ne peut être pressé, et la
  bordure de la fenêtre disparaît aussi, puisque rien dessus ne peut être saisi, si bien que seul le propre
  cadre du panneau se montre sur le jeu. Désactivez les clics traversants depuis la bande pour les retrouver.
  Cela ne fonctionne que sous Windows.

## Les pages

Une section par page.

### Accueil

La page d'accueil : une tuile par section avec une ligne sur ce qu'elle contient, et un encadré disant ce que
le plan contient et si le journal est suivi. Cinq chiffres courent en haut :

- **Matériaux en manque** : combien de matériaux manquent encore au plan.
- **Gagné, 7 jours** : les crédits gagnés sur la dernière semaine de jeu.
- **Sauts restants** : les sauts restants sur l'itinéraire, et si le carburant les couvre.
- **Corps scannés** : la progression du scan de ce système.
- **Données à bord** : ce que valent les scans et les échantillons que vous n'avez pas encore vendus, comptés
  depuis votre dernière vente et remis à zéro par une mort. Les corps qu'une balise de navigation vous a
  donnés ne sont pas comptés : le jeu ne paie rien pour des données qu'il a offertes, et la page du système
  les évalue aussi à rien.
  - Quand vous terminez un échantillon, le journal dit si quelqu'un avait enregistré cette espèce sur ce
    corps. Quand personne ne l'avait fait, Katey compte le bonus de premier enregistrement, cinq fois la
    base, comme probable. Un autre commandant peut encore la vendre avant vous, le chiffre est donc une
    probabilité, pas une promesse.
  - Avant tout échantillon, le drapeau de premier pas du scan tient lieu. La vie s'enregistre à pied, un
    corps où personne n'a posé le pied ne peut donc avoir été enregistré par quiconque. Katey compte le
    bonus comme probable là, et pas là où quelqu'un d'autre s'est posé.
  - Votre propre atterrissage ne l'enlève pas. Sur un corps où le vôtre a été le premier pas, une espèce que
    vous n'avez pas échantillonnée est toujours comptée à cinq fois, jusqu'à ce que vous vendiez les données
    d'exobiologie de ce corps. Après cette vente, Katey ne suppose rien pour ce que vous y avez laissé non
    échantillonné, puisqu'un autre commandant a pu s'y poser depuis. Une espèce que vous avez échantillonnée
    a la parole du jeu lui-même, donnée avec l'échantillon, et garde ou perd le bonus selon elle.

La couleur a un sens partout : les grades de matériaux, les trois inventaires, les quatre emplacements où un
module se monte, les classes d'étoile telles que la carte de la galaxie les peint, et les verdicts prêt,
partiel et manquant. Des barres de possédé face à requis se trouvent sous chaque matériau que le plan veut, et
la barre de carburant de la page Itinéraire et du panneau montre le réservoir avec ce que le prochain saut
brûlera en bleu par-dessus l'orange de ce qui reste, comme la jauge du jeu lui-même le dessine.

### Ingénieurs

Chaque ingénieur, le plus proche de vous d'abord, avec sa distance et s'il est sur votre itinéraire. Cochez
les ingénieurs que vous avez débloqués et comptez utiliser ; les coches pilotent les avertissements du plan
et tout ce que l'interrupteur en haut de la page restreint.

**N'afficher que ce que proposent les ingénieurs sélectionnés** est l'interrupteur le plus important de
Katey. Activé, l'index des modules et chaque sélecteur de modification, de grade et d'effet expérimental ne
proposent que ce que les ingénieurs cochés peuvent appliquer, et chaque sélecteur de grade démarre au grade
qu'ils atteignent. Désactivé, tout est proposé et les sélecteurs de grade démarrent au grade maximal. Dans
les deux cas, le plan avertit toujours d'une entrée que personne parmi vos cochés ne peut appliquer.
L'interrupteur est mémorisé.

Partout où Katey nomme un ingénieur, le nom est un lien vers sa page. L'interrupteur désactivé, chaque nom est
en texte ordinaire. Activé, un ingénieur que vous avez coché est dans le bleu du HUD et un que vous n'avez pas
coché est grisé, si bien que toute liste d'ingénieurs se lit d'un coup d'œil.

Une case de recherche filtre la liste par nom, et les colonnes se trient.

### Un ingénieur

Un seul ingénieur : où il est et à quelle distance de vous, et s'il est une étape de votre itinéraire. Chaque
modification qu'il propose est listée avec le grade où il s'arrête et un bouton **Ajouter** qui la planifie.
La page dit aussi lesquelles de vos entrées du plan il peut appliquer et combien de vos modules sont stockés
dans son système.

### Modules

Chaque type de module qui peut être modifié, dans un groupe par emplacement sur le vaisseau : modules internes
essentiels, modules internes optionnels, points d'emport et supports utilitaires. Cliquez sur un groupe pour
l'ouvrir, ou utilisez **Tout déplier** et **Tout replier**. Avec **N'afficher que ce que proposent les
ingénieurs sélectionnés** activé, un groupe ne liste que les types sur lesquels vos ingénieurs cochés peuvent
appliquer une modification. Les groupes ouverts sont mémorisés.

### Un type de module

Une sorte de module, comme le réacteur FSD ou un laser à rayon. Ses sections :

- **Modifications.** Chaque modification avec le grade que vos ingénieurs sélectionnés atteignent, tous ceux
  qui la proposent et leur distance, un sélecteur de grade et un bouton **Ajouter**. Les sélecteurs suivent
  l'interrupteur des ingénieurs.
- **Effets expérimentaux.** Les effets expérimentaux que le type accepte et ce que chacun coûte. Le grade 0
  sur la ligne d'une modification planifie l'effet expérimental seul, pour un module qui a déjà sa
  modification.
- **Variantes.** Chaque taille et classe que le jeu vend, avec celles que vous possédez marquées.
- **Vous possédez.** Les modules de ce type que vous possédez, installés ou stockés, où ils sont et comment
  ils sont modifiés.
- **Dans vos configurations.** Ce que vos configurations enregistrées demandent de ce type.

### Une modification

Une modification à travers chaque type de module auquel elle s'applique : ce que coûte un jet à chaque grade,
qui la propose sur quels types, et une barre qui l'ajoute au plan. Choisissez le type de module, le grade et
un effet expérimental et appuyez sur **Ajouter**, ou appuyez sur **Ajouter** sur la ligne d'un grade. Si vos
ingénieurs sélectionnés ne peuvent pas atteindre la modification pendant que l'interrupteur des ingénieurs
est activé, la barre le dit au lieu de proposer un grade que vous ne pouvez pas appliquer.

### Matériaux

Chaque matériau avec combien vous en détenez, combien le plan en demande et combien il vous en manque. Un
interrupteur par inventaire montre ou cache les matériaux bruts, manufacturés et encodés, un filtre par nom
restreint la liste, et **Manques seulement** ne garde que ceux qui vous manquent. Tout cela est mémorisé.

Chaque ligne prend aussi une **réserve** : le nombre que vous voulez garder au minimum, et un interrupteur
**Échange** qui dit si le matériau peut être cédé chez un négociant tout court. Les réserves sont
enregistrées.

Sous le tableau, **Chez les négociants en matériaux** dit ce qu'il faut céder pour couvrir tout ce qui vous
manque et reconstituer chaque réserve, échange par échange avec le taux, groupés par négociant. Le
planificateur ne dépense que ce que vous détenez au-delà de la réserve et jamais un matériau dont
l'interrupteur Échange est désactivé. Il préfère les échanges qui perdent le moins de valeur et viennent de
vos piles les plus pleines, et dit pourquoi quand quelque chose ne peut pas être couvert.

Les taux sont ceux du jeu : six pour un en montant d'un grade, un pour trois en descendant, et encore six
fois plus d'une famille à l'autre. Sous chaque échange, une seconde ligne nomme les familles des deux
matériaux telles que les négociants les groupent, comme « famille Fer → famille Carbone » ou « tous deux de
la famille Scans de sillage », suivie de ce que les lots entiers laissent en surplus. Chaque matériau nommé
dans un échange est un lien qui mène à sa ligne ici. La même liste se trouve en bas du tiroir du plan.

### Un matériau

Un seul matériau : combien vous en détenez face à ce que le plan demande, quelles entrées du plan le
dépensent, quoi d'autre dans le jeu l'utilise, grades de modification et effets expérimentaux confondus, et
quels corps scannés de votre système actuel en portent.

### Déblocages

Tout ce que vendent le courtier technologique des Gardiens et le courtier humain, un courtier à la fois, en
groupes repliables : les modules, armes, armes modifiées et chasseurs des Gardiens, et les modules, armes et
modules pré-modifiés du courtier humain. Cliquez sur un groupe pour l'ouvrir ou le fermer, ou utilisez
**Tout déplier** et **Tout replier** ; les groupes ouverts sont mémorisés. Chaque ligne montre ce que le
déblocage coûte en matériaux, par grade et par inventaire, et en marchandises que vous achetez sur un marché.

Cochez un déblocage et il rejoint le plan. Ses matériaux comptent dans les totaux du plan et dans les
échanges chez les négociants en matériaux, et ses marchandises apparaissent sous « À acheter sur un marché »
dans le tiroir. Le badge de chaque ligne dit où elle en est :

- **Abordable**, en orange : ce que le plan laisse dans votre inventaire le couvre.
- **Manque**, en rouge : ce n'est pas le cas, et la ligne dit de combien. La ligne est grisée mais reste
  cochable ; elle devient alors une entrée en manque comme une autre, et le tiroir dit quoi collecter.
- **Planifié** : il est dans le plan. Décochez-le pour le retirer.
- **Débloqué**, en vert : le journal enregistre que vous l'avez acheté, ou vous avez acheté ou possédez un
  module qu'il débloque, ce qui prouve l'achat même s'il a eu lieu avant les journaux que le jeu a gardés. Il
  ne peut pas être coché.

Le plan passe d'abord. Cocher un déblocage réclame ses matériaux, un autre qui avait besoin des mêmes peut
donc passer en manque ; le décocher ramène l'autre. Sous le tableau, les matériaux et les marchandises de
chaque déblocage planifié sont mis face à ce que vous détenez.

Certains déblocages pré-modifiés, comme le FSD modifié V1, ne sont pas nommés dans le journal, Katey ne peut
donc pas dire si vous les avez achetés ; une telle ligne le dit.

### Flotte

Vos vaisseaux, vos configurations enregistrées et les modules que vous possédez qu'aucune configuration
n'utilise. Ses sections :

- **Vaisseaux.** Chaque vaisseau que vous possédez, où il est, et s'il a été obtenu d'un pack ; voir
  [Vaisseaux pré-équipés](#vaisseaux-pré-équipés). Les objets cosmétiques comme les peintures et les kits de
  vaisseau sont laissés de côté partout dans Katey.
- **Configurations enregistrées.** Vos builds enregistrés, chacun avec un verdict face à tout ce que vous
  possédez selon la règle de partage. C'est de la possession, pas l'équipement d'un vaisseau ; ouvrez la
  configuration pour la comparer à un vaisseau emplacement par emplacement.
  - **Tout possédé** : chaque module que veut la configuration est quelque part dans votre flotte ou votre
    stockage.
  - **Partiel** : certains sont possédés mais pas modifiés jusqu'au bout.
  - **Manquants**, avec leur nombre.

  Quand plus d'un module possédé ferait l'affaire, celui déjà installé sur un vaisseau de la coque de la
  configuration est nommé d'abord, puis un en stockage, puis un sur un autre vaisseau. Parmi ceux au même
  endroit, le plus proche de vous vient en premier, d'après les coordonnées que le journal a pour son
  système, votre propre système d'abord et un lieu où vous n'êtes jamais allé en dernier.

  Une case de collage prend une nouvelle configuration depuis EDSY, et une configuration peut être retirée
  ici. Un build collé portant le même nom qu'une configuration enregistrée pour la même coque remplace cette
  configuration au lieu de se placer à côté : le bouton demande d'abord, et une seconde pression la remplace
  sur place. Le même nom sur une autre coque est une nouvelle configuration.
- **Modules possédés qu'aucune configuration n'utilise.** Les modules possédés qu'aucune configuration ne
  réclame, sous une **règle de partage** qui dit si les modules peuvent passer d'un vaisseau à l'autre,
  seulement entre vaisseaux d'une même coque, ou pas du tout, et des filtres par lieu, emplacement sur le
  vaisseau et état d'ingénierie.

La liste de la flotte et la liste des modules stockés sont réécrites par le jeu quand vous vous amarrez
quelque part avec un chantier naval ou un équipement, elles sont donc aussi fraîches que votre dernière
visite de ce genre.

### Un vaisseau

Un vaisseau que vous possédez : ses modules installés, chacun avec un sélecteur qui planifie l'ingénierie
pour ce module précis, et **Copier pour EDSY**, qui met l'enregistrement du jeu lui-même pour ce vaisseau
dans le presse-papiers, à coller dans l'import d'EDSY.

Le bouton du sélecteur dit où en est le travail :

- **Ajouter au plan** quand le choix est un travail à faire.
- **Dans le plan** tant que le plan contient déjà la modification, le grade et l'effet expérimental choisis
  pour cet emplacement.
- **Déjà modifié** tant que le module les a déjà. Choisissez un grade supérieur, une autre modification ou
  un autre effet expérimental et c'est de nouveau un travail.

Un module venu avec un vaisseau de pack est marqué **Module pré-équipé**, parce que son ingénierie reste sur
ce vaisseau. Pour un vaisseau pré-équipé, la page dit s'il est toujours tel qu'obtenu ou combien
d'emplacements ont changé ; cette ligne s'ouvre sur un tableau de ce que chaque emplacement changé contenait
à l'origine et contient maintenant. Voir [Vaisseaux pré-équipés](#vaisseaux-pré-équipés).

### Une configuration

Un build enregistré comparé à ce que vous avez. Ses sections :

- **Comparer avec un vaisseau.** La configuration emplacement par emplacement face à l'un de vos vaisseaux,
  en commençant par celui qui lui correspond déjà le mieux ; choisissez-en un autre dans la liste. Chaque
  emplacement dit s'il faut acheter, s'il faut de l'ingénierie et si vos ingénieurs sélectionnés peuvent la
  faire, et si un module que vous possédez déjà ailleurs remplirait l'emplacement à la place, dans
  l'emplacement et le lieu de votre choix.

  **Planifier les changements d'ingénierie pour ce vaisseau** ajoute chaque différence d'ingénierie au plan ;
  un emplacement auquel il ne manque que l'effet expérimental devient une entrée expérimental seul. Le bouton
  ne compte que ce que le plan ne contient pas déjà, et se met en retrait, en le disant, une fois chaque
  changement planifié.

  Chaque entrée est marquée de ce vaisseau et de cet emplacement, comme la page du vaisseau marque les
  siennes, les deux pages s'accordent donc sur ce qui est déjà planifié pour ce vaisseau. Le même changement
  planifié pour un autre vaisseau compte toujours comme non planifié ici. Les deux boutons de cette page
  reconnaissent leurs entrées l'un l'autre : l'ingénierie d'un emplacement planifiée comme un manque dans ce
  que vous possédez est le même travail que le changement pour ce vaisseau, et la page du vaisseau compte de
  la même façon l'entrée d'une configuration pour sa coque.
- **Face à tout ce que vous possédez.** Chaque module que veut la configuration face à chaque module que
  vous possédez, selon la règle de partage de la flotte. Un module avec la bonne modification à un grade
  inférieur, ou sans l'effet expérimental, est **partiel** plutôt que manquant. De même un module au bon
  grade qui n'est pas encore poussé jusqu'à son plein effet ; son texte d'ingénierie dit où il en est, comme
  « Renforcé G3, 25 % », et planifier le manque ne demande que les jets encore dus à ce grade. Un module
  modifié avant que le jeu ne garde ce chiffre se lit comme achevé.

  Le statut est coloré selon l'endroit où est le module :

  - vert : installé sur un vaisseau de cette coque ;
  - bleu : en stockage ;
  - jaune : installé sur un vaisseau d'une autre coque ;
  - orange : possédé mais demandant un changement, ou incertain ;
  - rouge : manquant.

  Un module venu avec un vaisseau de pack ne peut pas le quitter, il ne compte donc que pour les
  configurations de la coque de ce vaisseau et est marqué **pré-équipé** quand c'est le cas. **Planifier les
  manques dans ce que vous possédez** fait pour cette liste ce que le bouton du dessus fait pour un vaisseau,
  et se met en retrait de la même façon. Les deux comptes diffèrent à dessein : un emplacement peut être un
  changement pour ce vaisseau alors qu'un module que vous possédez ailleurs le couvre déjà.

Un module est reconnu partout où il se trouve dans un emplacement de la même sorte, un module déplacé vers
une autre baie optionnelle ou un autre point d'emport compte donc toujours. **Copier pour EDSY** met la
configuration dans le presse-papiers sous la forme qu'EDSY importe : l'export que vous avez collé, exactement
tel que vous l'avez collé, pour que rien de ce qu'EDSY a écrit ne se perde au retour.

### Système

Pour le système où vous êtes : chaque corps que vous avez scanné, lors de cette visite ou d'une précédente
que votre historique de journal contient, avec un badge **scanné** donnant le jour quand c'était une visite
précédente. Quand la recherche de système est autorisée dans les Paramètres, les corps que Spansh connaît et
que vous n'avez pas scannés rejoignent la liste avec un badge **d'après Spansh**. Ils viennent avec leurs
classes, leurs conditions, leurs signaux et leurs genres connus. Leur vie est évaluée comme la vôtre ; leur
cartographie ne vous vaut rien tant que vous ne les scannez pas vous-même. Ils ne disent rien de la
découverte, de la cartographie ni de l'atterrissage, aucun bonus de premier d'aucune sorte n'est donc supposé
pour eux. Pour un système que vous avez visité plus tôt, la page dit où il est et quand vous y étiez.

Au-dessus des corps, trois lignes résument le système.

- **Données d'exploration non vendues** est ce que les données de ce système encore à bord rapporteraient,
  bonus comptés là où ils s'appliquent, puis la part qui revient au fait d'être premier. Une fois qu'une
  partie des données de ce système a été vendue, la ligne finit par ce qui vous a été payé. Une vente d'un
  seul système est prise telle que le jeu l'a rapportée ; une vente de plusieurs est répartie entre eux selon
  l'estimation de Katey pour chacun.
- La deuxième ligne détaille le chiffre. La cartographie s'affiche telle que scannée sans bonus, puis avec le
  bonus de première découverte là où un corps était à vous à découvrir. L'exobiologie s'affiche d'après les
  genres du scanner, « environ » jusqu'à ce que chaque genre ait une espèce échantillonnée, puis avec le
  bonus de premier enregistrement là où personne n'avait posé le pied.
- La troisième dit combien de corps personne n'avait découverts avant vous, sur combien vous vous êtes posé,
  et sur combien de corps atterrissables personne n'a encore posé le pied, vous compris.

Les corps sont listés étoile par étoile : chaque étoile, nommée par sa classe et si vous pouvez y ravitailler,
est suivie des corps qui l'orbitent, les plus proches d'abord. Trier par une colonne met toute la liste dans
cet ordre à la place. Chaque colonne mémorise son propre sens, revenir à l'une la retrouve donc comme vous
l'avez laissée ; cela vaut pour chaque tableau de Katey.

Chaque corps montre ses signaux et ses matériaux de surface, puis ses genres, le plus précieux d'abord, avec
leurs prédictions d'espèce et leurs estimations de gain. Là où personne n'a posé le pied sur le corps, chaque
estimation porte le chiffre avec le bonus de premier enregistrement entre parenthèses, comme
« 2,1 M (10,5 M) Cr ». Un genre est violet après son premier échantillon, bleu après le deuxième et vert une
fois les trois pris. L'étape est mémorisée quand vous revenez dans le système et après une vente, comme le jeu
la garde. Commencer une autre espèce avant le troisième échantillon perd l'échantillon partiel, comme le fait
l'échantillonneur. Sous les genres se trouve le propre chiffre d'exobiologie du corps, « environ » jusqu'à ce
que chaque genre dessus ait une espèce échantillonnée. Une fois qu'une espèce y a ses trois échantillons, une
ligne en vert sous ce chiffre dit ce que vous avez récolté sur le corps, et ce que cela devient avec le bonus
de premier enregistrement.

La carte et les badges de chaque corps signalent ce qui compte avant que vous ne vous y engagiez.

- Le cadre de la carte dit ce que le corps porte : vert pour la vie, bleu pour les matériaux de surface ; un
  corps avec les deux prend le vert. La carte d'une étoile est encadrée d'orange et porte un badge
  **étoile**, avec **ravitaillable** en vert ou **non ravitaillable** en jaune à côté.
- Trois cases de statut, toujours dans le même ordre, chacune d'après les drapeaux que le jeu écrit avec
  chaque scan : violet pour la découverte, bleu sarcelle pour la cartographie, vert pour le premier pas. Une
  pastille a trois aspects, un corps vierge est donc des pastilles pleines, un corps que vous avez travaillé
  des mots colorés, et un corps que d'autres ont travaillé du gris, et vous lisez lequel est lequel sans
  lire les mots.
  - Pleine avec un trait dessous, le bonus est encore ouvert : **non cartographié**, là où est le bonus de
    cartographie, ou **jamais foulé**.
  - Détourée avec ses mots dans la couleur de la case, le bonus est à vous : **découvert par vous**,
    **cartographié par vous** ou **foulé par vous**.
  - Détourée avec des mots gris, quelqu'un d'autre l'a pris : **découvert**, **cartographié**, **foulé**.
- La découverte n'est jamais ouverte, puisque vous avez scanné le corps. Le jeu crédite une première
  découverte quand les données sont vendues, **découvert par vous** reste donc jusque-là et après.
- Un premier reste à vous quand vous revenez. Une fois les données d'un système vendues, les drapeaux du jeu
  rapportent votre propre fait, Katey garde donc comme vôtre un corps que vous avez découvert ou cartographié
  en premier, quoi que dise un scan ultérieur. Un corps où vous avez posé le pied en premier est à vous dès
  l'atterrissage. Un corps que vous avez scanné mais jamais vendu prend les drapeaux ultérieurs comme ils
  viennent, puisque quelqu'un d'autre vendant avant vous est alors ce qu'ils veulent dire.
- Une étoile n'a ni case de cartographie ni case de premier pas, et un corps où personne ne peut se poser n'a
  pas de case de premier pas. Là où quelqu'un d'autre a été premier mais que vous avez cartographié ou vous
  êtes posé aussi, une ligne discrète sous la carte dit **Cartographié par vous.** ou **Vous vous y êtes
  posé.**
- Dans un système peuplé, le premier pas ne peut pas être revendiqué et le drapeau du jeu ne dit rien, Katey
  traite donc chaque corps qui s'y trouve comme foulé. Un corps venu de Spansh dit **découverte inconnue**,
  **cartographie inconnue** et **premier pas inconnu**.
- Badge **atterrissable** vert : un vaisseau peut s'y poser.
- La gravité, la température, l'atmosphère et les notes de surface s'affichent sur la ligne de titre à côté
  de la distance, qui passe à la ligne autant qu'il le faut, une carte reste donc courte. La gravité passe au
  jaune à partir de 1,0 g avec un badge **gravité élevée**, et au rouge à partir de 1,5 g avec **gravité
  extrême** : à cette attraction, un vaisseau lourd peut ne pas réussir à arrêter sa descente, approchez donc
  lentement et redressez tôt.

**Copier le nom** met le nom du système dans le presse-papiers pour la carte de la galaxie. Chaque nom de
système, partout dans Katey, est un lien vers cette page.

Les prédictions d'espèce viennent des endroits où chaque espèce a déjà été trouvée. Avant qu'un corps soit
cartographié, un signal de genre est comparé à son atmosphère, son type, son volcanisme, sa gravité, sa
température et sa pression ; une fois cartographié, la liste de genres du scanner lui-même est utilisée.

Jusque-là, le chiffre d'exobiologie du corps est une prédiction, marquée « probablement » :

- chaque genre qui peut vivre dans les conditions du corps est pondéré par la fréquence à laquelle il a été
  trouvé dans de telles conditions ;
- ce gain pondéré est multiplié par le nombre de signaux biologiques ;
- une ligne dit combien de genres pourraient y vivre et nomme les trois plus probables, chacun avec ses
  chances, son espèce la plus probable et son gain attendu avant tout bonus. Les signaux peuvent venir de
  n'importe quel genre que les conditions permettent, pas seulement de ces trois.

Le nombre de signaux s'affiche en badge. Une espèce que personne n'a enregistrée sur un corps paie cinq fois
sa valeur de base. Le journal ne peut pas dire si quelqu'un l'a fait, donc quand personne n'avait cartographié
le corps, Katey prend le bonus comme probable et montre le chiffre fois cinq, avec la base à côté. Le panneau
classe les corps selon ce chiffre, connu ou prédit, bonus compris, puis par nombre de signaux. La colonne
**Atterrissable** de la page du système trie de la même façon, les corps atterrissables d'abord.

### Itinéraire

Deux sections :

- **Itinéraire du jeu**, celui que le jeu a tracé, étoile par étoile, avec où vous en êtes en tête de chaque
  ligne. Chaque étape porte la mention **inconnu de vous**, **visité** ou **relevé**, ce dernier par un scan
  de découverte ou une balise de navigation, puis la distance depuis le départ et le carburant attendu à
  l'arrivée.
  - Le système que vous avez ciblé est toujours le prochain saut. Ciblé sans itinéraire, c'est un itinéraire
    d'un saut. Ciblé plus loin sur l'itinéraire, les étapes intermédiaires disparaissent. Ciblé hors de
    l'itinéraire, la page montre le saut unique vers lui et une ligne disant où l'itinéraire tracé reprend et
    combien de sauts restent. La distance de la cible vient de votre historique ou du tracé Spansh, et se lit
    0 quand ni l'un ni l'autre ne connaît le système.
  - La projection de carburant fait courir la lecture du réservoir du jeu le long des étapes à venir avec la
    consommation propre du vaisseau, selon l'équation du jeu lui-même à partir des masses, de la cargaison
    et des chiffres du moteur que le journal rapporte. Elle est là dès le premier instant et dit combien de
    sauts restent avant de devoir ravitailler, avant quelle étoile ravitaillable le faire, et ce que coûte
    chaque étape. Chaque saut est chiffré au carburant qu'il lui reste, le vaisseau qui s'allège en brûlant
    est donc suivi.
  - La cargaison est le tonnage à bord maintenant, et la projection est refaite à chaque mise à jour du
    journal, une vente ou une collecte en route la change donc dès que le jeu la rapporte. Un vaisseau dont
    le journal n'a pas donné les masses de l'équipement se rabat sur la consommation apprise de ses sauts
    récents. Un départ depuis une étoile à neutrons ou une naine blanche compte comme un saut amplifié, et
    un amplificateur Gardien comme de la portée en plus.
  - La barre du réservoir au-dessus des étapes est dessinée comme la jauge du jeu lui-même : l'orange est ce
    qui reste dans le réservoir après le prochain saut, le bleu est ce que ce saut brûle, et le chiffre bleu
    à côté de la lecture est la même consommation en tonnes.
  - Une fois que vous avez sauté, le jeu prend le carburant aussitôt et le réservoir le montre, pendant le
    saut l'étape où vous arrivez montre donc la lecture du réservoir et la barre bleue passe au saut suivant.
- **Tracé Spansh.** Avec le traceur activé dans les [Paramètres](#paramètres), cette section demande au
  traceur galactique de Spansh un itinéraire d'où vous êtes vers n'importe quel système, pour un vaisseau que
  vous possédez ou une configuration enregistrée. Le tracé utilise les propres masses, réservoirs et chiffres
  de moteur du vaisseau : ceux du journal pour un vaisseau, ceux de l'export EDSY pour une configuration. Les
  amplifications à neutrons, les injections de FSD, le ravitaillement aux étoiles secondaires et le
  ravitaillement à chaque étoile ravitaillable sont des options.
  - Le sélecteur démarre sur le vaisseau que vous pilotez. Cliquer dessus ouvre un panneau avec une case de
    recherche vide et la liste, dans cet ordre : le vaisseau ou la configuration que vous aviez choisi en
    premier, le vaisseau que vous pilotez en second, puis les configurations enregistrées pour la coque
    choisie, vos vaisseaux et les autres configurations. Chacun montre sa portée à plein réservoir, le
    vaisseau que vous pilotez dans la couleur d'accent. Tapez pour restreindre la liste par nom ou par
    coque. Cliquez sur une entrée, ou appuyez sur Enter pour la première, pour la choisir. Rien d'autre ne
    change le choix.
  - Le traceur est informé que le moteur est surchargé quand vous avez pris une amplification de cône de jet
    depuis votre dernier saut, ou quand vous êtes à une étoile à neutrons avec les amplifications à neutrons
    activées, le premier saut est donc planifié comme un saut amplifié.
  - Les chiffres de moteur de Katey sont les mêmes tables que celles dont se sert le site de Spansh, moteurs
    SCO et Mk II compris, un tracé demandé par Katey correspond donc à un tracé fait en collant le build sur
    le site.
  - La destination, l'itinéraire et les options sont enregistrés à mesure que vous les changez. Le vaisseau
    ou la configuration et la cargaison restent tels que vous les avez réglés tant que le tracé est conservé,
    d'une page à l'autre et d'un lancement à l'autre, jusqu'à ce que vous appuyiez sur **Oublier le tracé**.
    La page s'ouvre alors de nouveau sur le vaisseau que vous pilotez, avec la cargaison qu'il porte
    remplie, que vous pouvez remplacer en tapant. Changer de vaisseau ou de configuration laisse la cargaison
    telle quelle.
  - Le tracé liste chaque point de passage avec son saut, la distance restante, le carburant que le traceur
    attend à l'arrivée et s'il s'agit d'une étoile à neutrons ou d'une étape de ravitaillement. Il suit vos
    sauts pour dire quel point de passage est le prochain, copie ce nom pour la carte de la galaxie, et est
    conservé d'un lancement à l'autre.
  - Quittez le tracé pour un détour, pour ravitailler par exemple, et le dernier point de passage que vous
    avez atteint depuis que le tracé a été fait est marqué **dernier connu** tandis que celui d'après reste
    le prochain point de passage, **Copier** vous donne donc toujours l'étoile à neutrons à viser. Un tracé
    fait depuis un endroit où vous n'êtes pas n'a pas de dernier point de passage connu tant que vous
    n'atteignez pas l'une de ses étapes, même si vous êtes passé par certaines d'entre elles avant.

### Stats

Votre historique de journal lu comme un registre. Ses sections :

- **Revenus.** Un graphique en anneau de ce que vous avez gagné par activité, des primes à l'exobiologie,
  avec un interrupteur par activité qui redessine le graphique sans elle, et un sélecteur de période, de tout
  l'historique à la dernière semaine.
- **Exobiologie.** Ce qui est à bord et non vendu : les échantillons, combien sont probablement des premiers
  enregistrements, et leur valeur avec et sans le bonus. Puis ce que les données organiques ont rapporté avec
  et sans le bonus de premier enregistrement, et chaque espèce vendue.
- **Exploration.** Ce qui est à bord et non vendu : les systèmes, les corps scannés et cartographiés, et
  leur valeur cartographique estimée. Puis les ventes de la période à côté des chiffres à vie du jeu.
- **Combat, Commerce et minage, Missions et autres, Powerplay.** Les propres chiffres de chaque activité
  pour la période, à côté des chiffres à vie du jeu pour elle.
- **Ailes et équipages.** Le temps volé avec d'autres et ce que le jeu a marqué comme partagé, par sorte.
  Chaque commandant avec qui vous avez volé est listé avec le temps ensemble et ce qui a été partagé pendant
  sa présence. Chaque mission d'aile acceptée est listée avec qui était dans l'aile à ce moment et où elle en
  est. Voler en aile pendant que vous explorez seul ne partage rien, cela ne compte donc pour rien ici. Le
  journal ne dit pas si une mission d'aile vient du tableau ou d'un coéquipier, les deux sont donc listées.
- **Chiffres à vie.** Chaque total cumulé que le jeu rapporte, temps de jeu compris. Un interrupteur
  mémorisé liste aussi les chiffres à zéro ; il est désactivé par défaut. Le sélecteur de période se cache
  ici, puisque ce sont les propres totaux du jeu.

### Paramètres

- **Dossier des journaux.** Le dossier que Katey suit. **Détecter** cherche aux emplacements habituels,
  **Parcourir** en choisit un, **Suivre** commence à le lire et **Arrêter** arrête. La ligne de statut dit ce
  que fait le suivi.
- **Sauvegarde des journaux.** Un dossier dans lequel tout le dossier des journaux est copié, seulement ce
  qui est nouveau ou modifié, quand Katey se ferme et chaque fois que vous appuyez sur **Sauvegarder
  maintenant**. Pointez-le vers un dossier que OneDrive ou un autre espace de stockage en ligne synchronise
  et vos journaux quittent la machine avec lui.
- **Données enregistrées.** Le dossier où Katey garde ses propres données ; voir [Vos données](#vos-données).
  Choisissez-en un autre et les données y passent aussitôt. Si le dossier contient déjà un fichier de données
  Katey, il est chargé, sinon les données actuelles y sont écrites, et les données à l'ancien endroit sont
  laissées telles quelles. Deux machines pointées vers un même dossier synchronisé partagent un seul plan,
  un seul jeu d'ingénieurs et un seul jeu de configurations.
- **Connexions sortantes.** Le seul endroit qui décide de ce que Katey peut envoyer sur le réseau, un
  interrupteur par sorte de requête, chacun désactivé tant que vous ne le cochez pas. Il y a trois sortes.
  Deux vont vers [Spansh](https://spansh.co.uk/) : demander un itinéraire quand vous appuyez sur
  **Tracer**, et demander les corps du système où vous venez d'arriver. La troisième va vers
  [GitHub](https://github.com/temetvince/elite-dangerous-katey/releases) et demande la dernière version publiée
  de Katey. Les interrupteurs sont mémorisés.
- **Mises à jour.** La version que vous utilisez et, l'interrupteur GitHub coché, s'il existe une version plus
  récente. Katey demande une fois au démarrage et chaque fois que vous appuyez sur **Vérifier maintenant**,
  en arrière-plan, si bien que le démarrage ne prend pas plus de temps. Quand une version plus récente
  existe, **Ouvrir la page de la version** l'ouvre dans votre navigateur ; rien n'est téléchargé ni installé
  pour vous.

## Travailler avec EDSY

[EDSY](https://edsy.org/) est le constructeur de vaisseaux que la plupart de la communauté utilise, et Katey
parle ses formes d'import et d'export dans les deux sens.

- **Un build dans le plan.** Dans EDSY, ouvrez le build et choisissez **Export**, puis le texte Loadout au
  format du journal. Collez-le dans la case d'import du tiroir du plan et appuyez sur **Lire l'export**.
  Chaque module modifié devient une entrée à sa modification, son grade et son effet expérimental ; décochez
  ceux que vous ne voulez pas et appuyez sur **Ajouter**.
- **Un build comme configuration enregistrée.** Collez le même texte dans la case des configurations
  enregistrées de la page [Flotte](#flotte). La configuration est alors comparée à vos vaisseaux et à vos
  modules possédés. Coller un export plus récent d'un build que vous avez déjà enregistré, même nom et même
  coque, remplace la configuration enregistrée après votre confirmation.
- **Un vaisseau ou une configuration vers EDSY.** **Copier pour EDSY** sur la page d'un
  [vaisseau](#un-vaisseau) ou d'une [configuration](#une-configuration) met un Loadout dans le
  presse-papiers. Dans EDSY, choisissez **Import** et collez.

## Vaisseaux pré-équipés

Un vaisseau obtenu d'un pack dans la boutique du jeu vient avec des modules que Katey marque comme
pré-équipés tant qu'ils restent dans les emplacements où ils sont arrivés. Acheter, récupérer, échanger,
stocker ou vendre dans un emplacement met fin à la marque, quoi que le jeu montre pour la valeur du nouveau
venu, de même qu'un module acheté de la même sorte prenant l'emplacement. Les modules de pack du jeu ne
peuvent être ni stockés, ni déplacés vers un autre vaisseau, ni vendus. Modifier un tel module est une
ingénierie qui reste sur ce vaisseau. Katey les marque ainsi :

- La page [Flotte](#flotte) marque un vaisseau obtenu d'un pack **pré-équipé**.
- La page d'un [vaisseau](#un-vaisseau) marque chacun de ces modules **Module pré-équipé** à côté de son
  sélecteur d'ingénierie, et la comparaison d'une [configuration](#une-configuration) avertit là où
  l'emplacement à modifier en contient un.
- La page d'un vaisseau dit aussi si un vaisseau pré-équipé est **toujours tel qu'obtenu**, auquel cas le
  vendre et l'obtenir de nouveau ne perd rien, ou combien d'emplacements ont changé depuis. La ligne s'ouvre
  sur un tableau de ce que chaque emplacement changé contenait à l'origine et contient maintenant. Un module
  pré-équipé échangé contre un module acheté de la même sorte compte comme un changement, puisque le module
  acheté peut quitter le vaisseau.

Un vaisseau pré-équipé que vous n'avez jamais déployé n'existe pas pour le jeu, il ne peut donc pas apparaître
dans la flotte.

## Ce que le journal ne peut pas dire à Katey

Katey ne sait que ce que le jeu écrit. Quelques limites en découlent :

- Savoir si quelqu'un d'autre a découvert un système n'est connu qu'une fois que vous scannez un corps
  dedans. Avant l'arrivée, Katey peut seulement dire si *vous* y êtes allé.
- Le journal n'enregistre pas qui a découvert ou cartographié un corps en premier, ni qui s'y est posé en
  premier, seulement si vous l'avez été.
- La liste de la flotte et la liste des modules stockés sont réécrites quand vous vous amarrez quelque part
  avec un chantier naval ou un équipement, elles sont donc aussi fraîches que votre dernière visite de ce
  genre. La modification et le grade d'un module stocké sont enregistrés mais pas son effet expérimental,
  que le vérificateur de configuration rapporte comme incertain.
- Le journal ne dit pas si une mission d'aile vient du tableau des missions ou d'un coéquipier.
- Les corps sont montrés pour le système où vous êtes ; un système précédent montre où il est et quand vous y
  étiez.
- Un corps que vous avez scanné lors d'une visite précédente est déjà dans vos données, le jeu n'écrit donc
  rien quand le scan de découverte le retrouve. Katey se souvient de chaque corps que vous avez scanné lors
  de n'importe quelle visite dans votre historique de journal et le liste de nouveau quand vous revenez,
  marqué du jour où il a été scanné. Un corps scanné avant votre plus ancien fichier journal ne peut pas être
  retrouvé ainsi. Quand le scan de découverte rapporte tout le système déjà connu, Katey compte chaque corps
  comme scanné mais ne peut lister que ceux qu'il a, ou ceux que Spansh connaît quand cet interrupteur est
  activé.
- Les corps marqués **d'après Spansh** sont ce que Spansh connaît, qui peut être plus ancien que le système
  ne l'est : un relevé d'il y a des années, ou seulement une partie d'un système. Katey interroge sur un
  système une fois, quand vous y arrivez, et garde la réponse pour la session ; si Spansh est injoignable, il
  attend cinq minutes avant de redemander pour ce système.

## Vos données

Katey garde un seul fichier : le plan, la sélection d'ingénieurs, les configurations enregistrées, les
réserves de matériaux, le dossier des journaux, le dossier et l'interrupteur de sauvegarde, les interrupteurs
des connexions sortantes et le dernier tracé, la position et les réglages du panneau, et chaque choix de vue
mémorisé. Par défaut il se trouve sous votre dossier de données d'application local, dans `Katey\state.json`.
La case **Données enregistrées** des [Paramètres](#paramètres) le déplace où vous voulez, dossier synchronisé
compris. Quand une mise à jour retire ou renomme un paramètre, Katey abandonne l'ancienne valeur et repart de
la valeur par défaut, le fichier ne porte donc jamais de paramètre que rien ne lit.

Katey lit les fichiers journaux localement et n'envoie rien nulle part tant que vous ne l'autorisez pas sous
**Connexions sortantes** dans les [Paramètres](#paramètres), où tout est désactivé au départ. Trois sortes de
requête existent. Deux vont vers [Spansh](https://spansh.co.uk/) : quand vous appuyez sur **Tracer**, deux
noms de système et les chiffres de saut du vaisseau ; et, à l'arrivée dans un système, l'identifiant de ce
système, qui rapporte les corps que Spansh y connaît. La troisième va vers GitHub quand Katey démarre et quand
vous appuyez sur **Vérifier maintenant** : une requête pour la dernière version publiée du projet, qui ne
porte rien de vous. Chaque requête indique Katey, sa version et l'adresse du projet, comme Spansh le demande
aux outils qui utilisent son service. La sauvegarde des journaux ne copie les fichiers que vers le dossier que
vous choisissez.

Katey garde aussi un fichier de log, `katey.log`, à côté du `state.json` par défaut sous votre dossier de
données d'application local. Il enregistre ce que Katey a fait et ce qui a mal tourné : les démarrages et les
arrêts, le dossier des journaux suivi, les requêtes faites et les échecs rencontrés, et la raison s'il se
ferme un jour de lui-même. Si Katey se ferme de façon inattendue, ce fichier dit pourquoi, et c'est ce qu'il
faut joindre à un rapport de bug. Il est renouvelé au-delà d'un mégaoctet, il reste donc petit.

Le fichier de log contient des noms de systèmes stellaires et de vaisseaux tirés de votre journal et rien
d'autre sur vous : pas de nom de commandant, pas de crédits, pas de positions et pas de noms de dossier. Les
propres messages de Katey ne portent jamais de chemin. Tout ce qui atteint le log par ailleurs, une erreur de
Windows par exemple, ne garde qu'un nom de fichier, vos noms de compte et de machine remplacés, le fichier
peut donc être partagé tel quel.

Pour un bug qui demande plus de détail, lancez Katey avec `katey.exe --verbose`, ou
`katey.exe --log-level debug`. Le log enregistre alors aussi les pages ouvertes, les décisions du panneau et
les recherches faites ; `--log-level warning` ne garde que les problèmes. Tout redevient normal au prochain
lancement sans l'option. `katey.exe --pseudo-loc` montre chaque mot allongé, accentué et entre crochets, ce
qui est la façon dont un traducteur ou un rapport de bug trouve un texte coupé ou qui n'est pas passé par les
tables.

Si le fichier journal du jour disparaît pendant que le jeu tourne, le jeu le réécrit depuis le début, et tout
ce qu'il contenait est perdu pour le jeu et pour Katey. La sauvegarde garde sa copie antérieure d'un tel
fichier à côté de la nouvelle, nommée avec un suffixe `rewritten`. Jusqu'à votre prochain saut, Katey prend
votre position dans le fichier d'itinéraire du jeu, dont la première étape est le système où vous êtes, le
panneau et la page Itinéraire restent donc justes ; les corps du système sont vides jusqu'à ce que vous
scanniez de nouveau.
