# Manual de Katey

También en [English](MANUAL.md), [Deutsch](MANUAL.de.md), [Français](MANUAL.fr.md),
[Português (Brasil)](MANUAL.pt-BR.md) y [Русский](MANUAL.ru.md).

Katey es un compañero de escritorio para Elite Dangerous. Lee los archivos de diario que el juego escribe en
tu propia máquina y te sigue mientras juegas. Planifica la ingeniería frente a los materiales que de verdad
tienes. Comprueba las configuraciones de nave que has guardado frente a las naves y los módulos que posees.
Sigue tu ruta y el sistema en el que estás, y lee tu historial como un libro de cuentas. Nada sale de tu
máquina salvo que lo permitas en Ajustes, donde toda conexión saliente empieza desactivada.

## Contenido

- [Inicio rápido](#inicio-rápido)
- [La ventana](#la-ventana): la [barra superior](#la-barra-superior),
  la [barra inferior](#la-barra-inferior), la [búsqueda](#búsqueda),
  [el cajón del plan](#el-cajón-del-plan) y [el panel en vivo](#el-panel-en-vivo)
- [Las páginas](#las-páginas): [Inicio](#inicio), [Ingenieros](#ingenieros), [un ingeniero](#un-ingeniero),
  [Módulos](#módulos), [un tipo de módulo](#un-tipo-de-módulo), [un plano](#un-plano),
  [Materiales](#materiales), [un material](#un-material), [Desbloqueos](#desbloqueos), [Flota](#flota),
  [una nave](#una-nave), [una configuración](#una-configuración),
  [Sistema](#sistema), [Ruta](#ruta), [Datos](#datos) y [Ajustes](#ajustes)
- [Trabajar con EDSY](#trabajar-con-edsy)
- [Naves preconstruidas](#naves-preconstruidas)
- [Lo que el diario no puede decirle a Katey](#lo-que-el-diario-no-puede-decirle-a-katey)
- [Tus datos](#tus-datos)

## Inicio rápido

1. **Inicia Katey.** La versión publicada es un único ejecutable; no hay nada que instalar. Solo se ejecuta
   un Katey a la vez: iniciar un segundo muestra un aviso y deja el primero en paz, ya que dos sobrescribirían
   los datos guardados del otro. En Windows la carpeta de diarios del juego se encuentra sola. Si la página
   Ajustes dice que no se ha encontrado ninguna carpeta, apúntala a la carpeta que contiene los archivos
   llamados `Journal.*.log`, normalmente bajo `Saved Games\Frontier Developments\Elite Dangerous`.
2. **Deja que se ponga al día.** La primera ejecución reproduce todo tu historial de diario, el archivo más
   antiguo primero, y muestra su progreso a lo largo de la parte superior de la ventana y en el panel: cuántos
   archivos se han leído, con una barra que se llena archivo a archivo. No aparece nada en vivo hasta que
   termina, así que nunca ves una cifra tomada a medias. Un historial de varios años tarda unos segundos.
3. **Marca tus ingenieros.** Abre **Ingenieros** en la barra superior y marca los ingenieros que has
   desbloqueado y a los que piensas visitar. Activa **Mostrar solo lo de los ingenieros seleccionados** en la
   parte superior de esa página y todas las listas y selectores de Katey se limitan a lo que esos ingenieros
   pueden aplicar, con cada selector de grado empezando en el grado que alcanzan.
4. **Planifica algo de ingeniería.** Abre **Módulos**, elige un tipo de módulo, escoge un grado y un efecto
   experimental en el plano que quieras y pulsa **Añadir**. O abre el cajón **Plan** desde la barra inferior
   y pega una configuración de EDSY en su caja de importación; cada módulo con ingeniería de la configuración
   se convierte en una entrada.
5. **Lee el plan.** El cajón lista cada entrada con quién puede aplicarla, cada material que necesita el plan
   frente a lo que tienes y, al final, los intercambios que hacer en los comerciantes de materiales para
   cubrir lo que te falte. **Copiar lista de la compra** pone la lista de la compra en el portapapeles.
6. **Vuela.** El panel en vivo de la derecha sigue al juego: tu posición, la ruta, el combustible, los
   cuerpos que merece la pena escanear en este sistema, con los que llevan un material que te falta
   etiquetados. **Separar** lo lleva a una ventana propia que se queda sobre el juego.
7. **Comprueba una configuración frente a tu flota.** Abre **Flota**, pega una configuración de EDSY en la
   caja de configuraciones guardadas y abre la configuración. Se compara ranura por ranura con la nave que
   mejor coincide con ella y con todo lo que tienes, y un botón planifica la ingeniería que falta.

Todo lo demás en este manual dice qué hace cada parte de la ventana y cómo sacarle el máximo partido.

## La ventana

Katey es una sola ventana con una página en el centro, un panel a lo largo del borde derecho y dos barras.
Dibuja su propia barra de título con los colores de la aplicación y lleva la cara de Katey como icono. Las
páginas nunca tienen menos de 960 píxeles de ancho, así que el ancho mínimo de la ventana es ese más el panel
mientras el panel está acoplado; separa el panel y la ventana cabe en una pantalla de 1080 píxeles de ancho.
Se abre centrada en la pantalla principal, sea cual sea la pantalla en la que estuvo por última vez.

### La barra superior

La barra superior es la barra de título de la ventana: arrástrala para mover la ventana, haz doble clic en
ella para maximizar y usa los tres botones de su extremo derecho para minimizar, maximizar y cerrar. De
izquierda a derecha contiene:

- **KATEY**, el logotipo, que abre la [página de inicio](#inicio).
- **Atrás** y **adelante**, que funcionan como en un navegador y te devuelven a donde te habías desplazado.
- Los enlaces de página: **Ingenieros**, **Módulos**, **Materiales**, **Desbloqueos**, **Flota**, **Ruta**
  y **Sistema**. El de la sección en la que estás se ilumina, así que la página de un ingeniero
  ilumina Ingenieros y un plano ilumina Módulos. Pulsa un enlace otra vez mientras su propia página
  está abierta y vuelves a la página en la que estabas antes.

### La barra inferior

- **Idioma**, el globo del extremo izquierdo, muestra el idioma en el que está Katey y abre la lista de
  idiomas en los que se distribuye: inglés, alemán, español, francés, portugués (Brasil) y ruso, cada uno en
  su propio nombre, con "Idioma del sistema" primero. Katey sigue el idioma de tu Windows hasta que eliges
  uno; la elección se recuerda y toda la ventana cambia a la vez. Los nombres propios del juego cambian con
  él: materiales, planos, efectos experimentales, tipos de módulo, artículos de intermediario tecnológico,
  clases de planeta, atmósferas y vulcanismo, y las cifras históricas. Los nombres de especies, naves y
  módulos que vienen de tu diario siguen el idioma del propio cliente del juego. Este manual tiene una copia
  en cada idioma, enlazada en su parte superior.
- **Ajustes**, a su lado, abre la página [Ajustes](#ajustes). Púlsalo otra vez mientras los ajustes están
  abiertos y vuelves a la página en la que estabas.
- **Datos**, a su lado, abre la página [Datos](#datos) y, pulsado otra vez mientras está abierta, te
  devuelve a la página en la que estabas. Está aquí y no en la barra superior para que los enlaces de
  página quepan en una línea en todos los idiomas.
- La **caja de búsqueda** que cruza el centro; consulta [Búsqueda](#búsqueda).
- **Plan**, a la derecha, abre y cierra el [cajón del plan](#el-cajón-del-plan). El número que lleva es
  cuántas entradas tiene el plan.

### Búsqueda

Escribe en la caja de búsqueda, o pulsa **Ctrl K** para saltar a ella, para encontrar un ingeniero, un tipo
de módulo, un plano, un material, un sistema que hayas visitado, una de tus naves o una configuración
guardada. Los resultados suben por encima de la caja a medida que escribes. **Enter** abre el primer
resultado, las teclas de flecha se mueven por ellos y **Escape** vacía la caja. Cada resultado abre su propia
página.

### Páginas, enlaces y opciones recordadas

Cada página describe una cosa o lista cosas de un mismo tipo. Cada mención de otra cosa en una página es un
enlace que abre su página: el nombre de un sistema abre el sistema, el nombre de un ingeniero abre al
ingeniero, una ficha de material abre el material. Las etiquetas pequeñas sobre el título de una página son
enlaces de vuelta al índice al que pertenece.

Tres mecanismos se comportan igual en todas las páginas, y cada uno recuerda su estado entre ejecuciones:

- **Ordenar.** Haz clic en el encabezado de una columna para ordenar la tabla por ella; haz clic otra vez
  para invertir el sentido.
- **Secciones.** Una página larga muestra una sección a la vez, elegida con las pestañas bajo su título.
- **Grupos.** Una página que agrupa cosas abre y cierra cada grupo con un clic, con **Expandir todo** y
  **Contraer todo** a su lado.

Los filtros, los interruptores y el estado abierto de cada pliegue también se recuerdan, así que una página
vuelve tal como la dejaste.

### El cajón del plan

El plan es la lista de ingeniería que piensas hacer. El cajón se abre sobre cualquier página desde el botón
**Plan** y contiene:

- **Las entradas.** Cada una tiene:
  - un interruptor para dejarla fuera de los totales sin borrarla;
  - un selector de grado y, para un tipo que admite efectos experimentales, un selector de experimental y
    el interruptor **Solo experimental**; un tipo sin ninguno no muestra ni uno ni otro;
  - las tiradas previstas por grado;
  - los ingenieros que pueden aplicarla, cada uno un enlace coloreado según tus marcas como en todo Katey.
    Una entrada que nadie de los que has marcado puede aplicar lleva un aviso.

  El interruptor **Solo experimental** convierte una entrada en una que cuesta solo los materiales del efecto
  experimental, para un módulo que ya tiene su plano. Un desbloqueo de intermediario tecnológico que hayas
  marcado en la página [Desbloqueos](#desbloqueos) también es una entrada, con sus materiales fijos y las
  mercancías que además necesita de un mercado.

  El plan sigue lo que haces en el ingeniero:

  - cada tirada que el juego informa sobre un módulo del plano y tipo de una entrada quita una tirada de ese
    grado, o lo despeja cuando el grado se completa;
  - el efecto experimental sale de la entrada en cuanto está en el módulo, sea cual sea el grado en que esté;
  - una vez que el módulo está en el grado objetivo sin nada más pendiente, la entrada sale del plan.

  Solo cuentan las tiradas hechas después de añadir la entrada, así que la ingeniería hecha antes nunca
  quita una entrada.
- **Los totales.** Cada material que necesita el plan, con cuántos tienes y cuántos te faltan, agrupados en
  bruto, manufacturados y codificados. **Copiar lista de la compra** pone la lista en el portapapeles como
  texto sin formato.
- **Los intercambios.** Qué entregar en los comerciantes de materiales para cubrir todo lo que te falta,
  intercambio a intercambio, agrupados por comerciante. Consulta [Materiales](#materiales) para saber cómo se
  eligen los intercambios.
- **La importación de EDSY.** Pega una configuración exportada desde EDSY, pulsa **Leer exportación**,
  desmarca los módulos que no quieras y pulsa **Añadir al plan**. Consulta
  [Trabajar con EDSY](#trabajar-con-edsy).

**Vaciar el plan** vacía el plan tras una segunda pulsación, para que un despiste no pueda perderlo.

### El panel en vivo

El panel sigue al juego a lo largo del borde derecho de la ventana. Muestra:

- **Dónde estás** y qué hace la nave: atracado, en supercrucero, aterrizado, etcétera.
- **La ruta**: la parada en la que estás y las siguientes, con el resto resumido. Se señalan la siguiente
  estrella repostable y cualquier parada con ingeniero. La distancia recorrida se muestra frente al total y
  como porcentaje, con la barra del depósito tal como la dibuja la [página de ruta](#ruta). El cuadro del
  combustible dice cuántos saltos puedes hacer antes de tener que repostar, y antes de qué estrella repostar.
  Cuando hay un trazado de Spansh guardado, el siguiente punto de ruta se muestra con un botón **Copiar**
  para el mapa de la galaxia.
- **Este sistema**: el progreso del escaneo y las estimaciones de pago, con cada cuerpo escaneado que tiene
  materiales en superficie o señales biológicas. **Datos de exploración sin vender** es lo que pagarían los
  datos de este sistema que aún llevas a bordo. Eso son los escaneos que no has vendido, con la bonificación
  por primer descubrimiento donde un cuerpo era tuyo por descubrir, y las especies que has muestreado por
  completo y no has vendido, con la bonificación por primer registro donde el juego dijo que nadie las había
  registrado. La página del sistema añade lo que ya te han pagado. El encabezado y el anillo se vuelven
  verdes cuando todos los cuerpos están escaneados.
  - Antes de cartografiar un cuerpo, su vida es una sola línea: cuántos géneros podrían vivir allí y cuánto
    es probable que valgan, con y sin la bonificación por primer registro. Una vez sondeado, el cuerpo lista
    sus géneros, los más ricos primero. Cada fila es el género, su especie más probable con las
    probabilidades o su etapa de muestreo, y lo que paga con la bonificación donde corresponda. La fila es
    violeta tras la primera muestra, azul tras la segunda y verde una vez tomadas las tres.
  - La gravedad se muestra como etiqueta desde 1,0 g, en rojo desde 1,5 g.
  - Una elección entre tres dice para qué es la lista, cada una con los más cercanos primero por distancia
    desde la llegada. **Aterrizables** lista todos los cuerpos aterrizables con su vida y sus materiales.
    **Biología** lista los cuerpos con vida y muestra solo su vida, así que un cuerpo aterrizable con
    materiales solamente no aparece. **Minerales** lista los cuerpos con materiales en superficie y muestra
    solo sus materiales; un cuerpo con vida y materiales aparece, con su recuento de biología todavía en el
    título, pero sin sus géneros. Aterrizables es la opción predeterminada. La elección se recuerda.
  - En la vista Biología, una segunda elección entre dos dice qué cuerpos con vida dejar fuera. La elección
    se recuerda. Las condiciones de las que Katey no tiene registro nunca se ocultan.
  - **Toda la vida** los lista todos.
  - **Omitir solo bacterias** deja fuera los cuerpos cuya única vida son bacterias: por los géneros del
    escáner una vez cartografiados, o antes de eso por una sola señal con al menos cuatro de cinco
    probabilidades de ser bacterias.

El panel elige por sí mismo la ruta o los cuerpos. Muestra los cuerpos desde el momento en que llegas, con
una línea sobre ellos acerca de la ruta: el siguiente salto, cuántos saltos quedan y dónde termina la ruta.
Cambia a la ruta cuando el motor carga para saltar y la mantiene hasta el momento en que llegas a la
siguiente estrella. En supercrucero una carga solo puede ser un salto, así que el panel cambia al instante;
en espacio normal la misma carga puede ser para supercrucero, así que el panel espera a la cuenta atrás del
salto. El juego borra la ruta a mitad de su último salto, y el panel sigue mostrando ese salto hasta que se
hace. Lo que tengas fijado no mueve el panel: el juego fija el siguiente salto por sí mismo en cada llegada
a lo largo de una ruta, así que un sistema fijado no dice nada sobre si estás explorando aquí. La sección
útil está arriba sin un clic.

Fija un sistema que no está en tu ruta trazada y el juego recalcula su ruta a través de él, pero no
reescribe su archivo de ruta al instante. El panel y la página Ruta dicen "Ruta recalculada por" ese
sistema, con los saltos restantes tomados del propio recuento del juego en vez del archivo desfasado, hasta
que el archivo se pone al día.

**Separar** lleva el panel a una ventana propia que se queda por encima de las demás ventanas, para un
segundo monitor o una captura de retransmisión. Una franja de una línea se queda en la ventana principal con
el progreso del escaneo y estos controles:

- **Devolver el panel** lo acopla de nuevo. Cerrar la ventana del panel hace lo mismo.
- **Restablecer posición** devuelve la ventana del panel a la pantalla principal con su tamaño
  predeterminado. Si no, recuerda dónde la dejaste y el tamaño que le diste.
- **Superposición** hace la ventana casi transparente con texto brillante, para que pueda colocarse sobre un
  juego que se ejecuta en ventana o sin bordes. Un juego a pantalla completa exclusiva cubre todas las demás
  ventanas, incluida esta.
- **Traspasar clics** deja que los clics atraviesen el panel hasta el juego. Mientras está activado, los
  botones, enlaces e interruptores del panel se ocultan, ya que nada en él puede pulsarse, y el borde de la
  ventana también desaparece, ya que nada en él puede agarrarse, así que solo el propio panel se muestra
  sobre el juego. Desactiva Traspasar clics desde la franja para recuperarlos. Solo funciona en Windows.

## Las páginas

Una sección por página.

### Inicio

La página de inicio: una ficha por cada sección con una línea sobre lo que contiene, y un panel que dice qué
tiene el plan y si se está siguiendo el diario. Cinco cifras recorren la parte superior:

- **Materiales que faltan**: cuántos materiales le faltan todavía al plan.
- **Ganado, 7 días**: créditos ganados en la última semana de juego.
- **Saltos restantes**: saltos que quedan en la ruta, y si el combustible los cubre.
- **Cuerpos escaneados**: el progreso del escaneo de este sistema.
- **Datos a bordo**: lo que valen los escaneos y muestras que aún no has vendido, contado desde tu última
  venta y puesto a cero por una muerte. Los cuerpos que te entregó una baliza de navegación no se cuentan: el
  juego no paga nada por datos que regaló, y la página del sistema también los valora en nada.
  - Cuando completas una muestra el diario dice si alguien había registrado esa especie en ese cuerpo. Cuando
    nadie lo había hecho, Katey cuenta como probable la bonificación por primer registro, cinco veces la base.
    Otro comandante aún puede venderla antes, así que la cifra es una probabilidad, no una promesa.
  - Antes de cualquier muestra, lo sustituye el indicador de pisada del escaneo. La vida se registra a pie,
    así que nadie puede haber registrado un cuerpo en el que nadie ha puesto el pie. Katey cuenta allí la
    bonificación como probable, y no donde otro ha aterrizado.
  - Tu propio aterrizaje no la quita. En un cuerpo donde la tuya fue la primera pisada, una especie que no has
    muestreado sigue contándose a cinco veces, hasta que vendas los datos de exobiología de ese cuerpo. Tras
    esa venta Katey no supone nada de lo que dejaste sin muestrear allí, ya que otro comandante puede haber
    aterrizado desde entonces. Una especie que sí muestreaste tiene la palabra del propio juego, dada con la
    muestra, y conserva o pierde la bonificación según ella.

El color significa algo en todas partes: los grados de los materiales, los tres inventarios, los cuatro
lugares en los que va un módulo, las clases de estrella tal como las pinta el mapa de la galaxia, y los
veredictos listo, parcial y falta. Bajo cada material que pide el plan hay barras de lo que tienes frente a lo
que necesitas, y la barra de combustible de la página de ruta y del panel muestra el depósito con lo que
quemará el siguiente salto en azul sobre el naranja de lo que queda, tal como lo dibuja el propio indicador
del juego.

### Ingenieros

Todos los ingenieros, los más cercanos a ti primero, con a qué distancia están y si están en tu ruta. Marca
los ingenieros que has desbloqueado y piensas usar; las marcas gobiernan los avisos del plan y todo lo que
limita el interruptor de la parte superior de la página.

**Mostrar solo lo de los ingenieros seleccionados** es el interruptor más importante de Katey. Activado, el
índice de módulos y cada selector de plano, grado y efecto experimental ofrecen solo lo que pueden aplicar los
ingenieros que has marcado, y cada selector de grado empieza en el grado que alcanzan. Desactivado, se ofrece
todo y los selectores de grado empiezan en el grado máximo. En ambos casos, el plan sigue avisando de una
entrada que nadie de los que has marcado puede aplicar. El interruptor se recuerda.

Dondequiera que Katey nombra a un ingeniero, el nombre es un enlace a su página. Con el interruptor
desactivado, cada nombre va en texto normal. Con él activado, un ingeniero que has marcado va en el azul del
HUD y uno que no, atenuado, así que cualquier lista de ingenieros se lee de un vistazo.

Una caja de búsqueda filtra la lista por nombre, y las columnas se ordenan.

### Un ingeniero

Un ingeniero: dónde está y a qué distancia de ti, y si es una parada de tu ruta. Cada plano que ofrece se
lista con el grado en el que se queda y un botón **Añadir** que lo planifica. La página también dice cuáles
de las entradas de tu plan puede aplicar y cuántos de tus módulos están almacenados en su sistema.

### Módulos

Todos los tipos de módulo que se pueden modificar, en un grupo por cada lugar de la nave: internos
principales, internos opcionales, anclajes y soportes de utilidad. Haz clic en un grupo para abrirlo, o usa
**Expandir todo** y **Contraer todo**. Con **Mostrar solo lo de los ingenieros seleccionados** activado, un
grupo lista solo los tipos en los que tus ingenieros marcados pueden aplicar un plano. Los grupos abiertos se
recuerdan.

### Un tipo de módulo

Un tipo de módulo, como el motor de distorsión o un láser de haz. Sus secciones:

- **Planos.** Cada plano con el grado que alcanzan tus ingenieros seleccionados, todos los que lo ofrecen y a
  qué distancia están, un selector de grado y un botón **Añadir**. Los selectores siguen al interruptor de
  ingenieros.
- **Efectos experimentales.** Los efectos experimentales que admite el tipo y lo que cuesta cada uno. El
  grado 0 en la fila de un plano planifica solo el efecto experimental, para un módulo que ya tiene su plano.
- **Variantes.** Todos los tamaños y clases que vende el juego, con los que tienes marcados.
- **Tienes.** Los módulos de este tipo que posees, instalados o almacenados, dónde están y cómo están
  modificados.
- **En tus configuraciones.** Lo que piden de este tipo tus configuraciones guardadas.

### Un plano

Un plano en todos los tipos de módulo a los que se aplica: lo que cuesta una tirada en cada grado, quién lo
ofrece en qué tipos, y una barra que lo añade al plan. Elige el tipo de módulo, el grado y un efecto
experimental y pulsa **Añadir**, o pulsa **Añadir** en la fila de un grado. Si tus ingenieros seleccionados
no pueden alcanzar el plano mientras el interruptor de ingenieros está activado, la barra lo dice en vez de
ofrecer un grado que no puedes aplicar.

### Materiales

Todos los materiales con cuántos tienes, cuántos necesita el plan y cuántos te faltan. Un interruptor por
inventario muestra u oculta los materiales en bruto, manufacturados y codificados, un filtro por nombre
reduce la lista y **Solo los que faltan** deja solo los que te faltan. Todo ello se recuerda.

Cada fila admite además una **reserva**: la cantidad que quieres conservar como mínimo, y un interruptor
**Intercambiar** que dice si el material puede entregarse en un comerciante en absoluto. Las reservas se
guardan.

Bajo la tabla, **En los comerciantes de materiales** dice qué entregar para cubrir todo lo que te falta y
reponer cada reserva, intercambio a intercambio con la tasa, agrupado por comerciante. El planificador gasta
solo lo que tienes por encima de su reserva y nunca un material cuyo interruptor Intercambiar está
desactivado. Prefiere los intercambios que pierden menos valor y salen de tus pilas más llenas, y dice por qué
cuando algo no puede cubrirse.

Las tasas son las del juego: seis por uno al subir un grado, uno por tres al bajar, y seis veces más otra vez
entre familias. Bajo cada intercambio, una segunda línea nombra las familias de los dos materiales tal como
las agrupan los comerciantes, como "familia Hierro → familia Carbono" o "ambos de la familia Escaneos de
estela", seguida de cuántos sobran de los lotes completos. Cada material nombrado en un intercambio es un
enlace que lleva a su fila aquí. La misma lista está al final del cajón del plan.

### Un material

Un material: cuántos tienes frente a lo que necesita el plan, qué entradas del plan lo gastan, qué más lo usa
en el juego, tanto grados de plano como efectos experimentales, y qué cuerpos escaneados de tu sistema actual
lo tienen.

### Desbloqueos

Todo lo que venden el intermediario tecnológico guardián y el humano, un intermediario a la vez, en grupos
plegables: los módulos, armas, armas modificadas y cazas guardianes, y los módulos, armas y módulos
premodificados del intermediario humano. Haz clic en un grupo para abrirlo o cerrarlo, o usa **Expandir
todo** y **Contraer todo**; los grupos abiertos se recuerdan. Cada fila muestra lo que cuesta el desbloqueo
en materiales, por grado e inventario, y en mercancías que compras en un mercado.

Marca un desbloqueo y entra en el plan. Sus materiales cuentan en los totales del plan y en los intercambios
en los comerciantes de materiales, y sus mercancías aparecen bajo "Comprar en un mercado" en el cajón. La
insignia de cada fila dice en qué estado está:

- **Asequible**, en naranja: lo que el plan deja en tu inventario lo cubre.
- **Falta**, en rojo: no lo cubre, y la fila dice por cuánto. La fila está atenuada pero aún puede marcarse;
  entonces se convierte en una entrada con faltas como cualquier otra, y el cajón dice qué recoger.
- **Planificado**: está en el plan. Desmárcalo para sacarlo.
- **Desbloqueado**, en verde: el diario registra que lo compraste, o has comprado o posees un módulo que
  desbloquea, lo que demuestra la compra incluso cuando ocurrió antes de los diarios que conserva el juego.
  No puede marcarse.

El plan va primero. Marcar un desbloqueo reclama sus materiales, así que otro que necesitaba los mismos puede
pasar a faltar; desmarcarlo recupera el otro. Bajo la tabla, los materiales y mercancías de cada desbloqueo
planificado se disponen frente a lo que tienes.

Algunos desbloqueos premodificados, como el FSD modificado V1, no se nombran en el diario, así que Katey no
puede saber si los has comprado; esa fila lo dice.

### Flota

Tus naves, tus configuraciones guardadas y los módulos que posees y que ninguna configuración usa. Sus
secciones:

- **Naves.** Cada nave que posees, dónde está y si se canjeó de un paquete; consulta
  [Naves preconstruidas](#naves-preconstruidas). Los artículos cosméticos como las pinturas y los kits de
  nave se dejan fuera en todo Katey.
- **Configuraciones guardadas.** Tus configuraciones guardadas, cada una con un veredicto frente a todo lo
  que posees según la política de compartir. Eso es propiedad, no la configuración de una nave; abre la
  configuración para compararla con una nave ranura por ranura.
  - **Todo en propiedad**: cada módulo que pide la configuración está en algún sitio de tu flota o almacén.
  - **Parcial**: algunos están en propiedad pero no modificados hasta el final.
  - **Faltan**, con cuántos.

  Cuando más de un módulo en propiedad serviría, se nombra primero el que ya está instalado en una nave del
  casco de la configuración, luego uno en el almacén, luego uno en otra nave. Entre los que están en el mismo
  lugar, el más cercano a ti va primero, por las coordenadas que el diario tiene de su sistema, con tu propio
  sistema primero y un lugar que nunca has visitado al final.

  Una caja de pegado admite una configuración nueva de EDSY, y aquí puede quitarse una configuración. Una
  configuración pegada con el mismo nombre que una guardada para el mismo casco reemplaza esa configuración
  en vez de ponerse a su lado: el botón pregunta primero, y una segunda pulsación la reemplaza en su sitio.
  El mismo nombre en otro casco es una configuración nueva.
- **Módulos propios que ninguna configuración usa.** Los módulos en propiedad que ninguna configuración
  reclama, bajo una **política de compartir** que dice si los módulos pueden moverse entre naves, solo entre
  naves del mismo casco o en absoluto, y filtros por ubicación, lugar en la nave y estado de ingeniería.

El juego reescribe la lista de la flota y la lista de módulos almacenados cuando atracas en algún sitio con
astillero o equipamiento, así que están tan al día como tu última visita de ese tipo.

### Una nave

Una nave que posees: sus módulos instalados, cada uno con un selector que planifica ingeniería para ese
mismo módulo, y **Copiar para EDSY**, que pone en el portapapeles el propio registro del juego de la nave
para pegarlo en la importación de EDSY.

El botón del selector dice en qué estado está el trabajo:

- **Añadir al plan** cuando la elección es un trabajo.
- **En el plan** mientras el plan ya tiene el plano, el grado y el efecto experimental elegidos para esa
  ranura.
- **Ya tiene esa ingeniería** mientras el módulo ya los tiene. Elige un grado más alto, otro plano u otro
  efecto experimental y vuelve a ser un trabajo.

Un módulo que vino con una nave de paquete se marca como **Módulo preconstruido**, porque su ingeniería se
queda con esa nave. Para una nave preconstruida, la página dice si sigue tal como vino o cuántas ranuras han
cambiado; esa línea se abre en una tabla de lo que traía cada ranura cambiada y lo que tiene ahora. Consulta
[Naves preconstruidas](#naves-preconstruidas).

### Una configuración

Una configuración guardada comparada con lo que tienes. Sus secciones:

- **Comparar con una nave.** La configuración ranura por ranura frente a una de tus naves, empezando por la
  que ya coincide mejor con ella; elige otra de la lista. Cada ranura dice si hay que comprar, si necesita
  ingeniería y si tus ingenieros seleccionados pueden hacerla, y si un módulo que ya posees en otro sitio
  cubriría la ranura en su lugar, dentro del lugar y la ubicación que elijas.

  **Planificar los cambios de ingeniería de esta nave** añade al plan cada diferencia de ingeniería; una
  ranura a la que solo le falta el efecto experimental se convierte en una entrada solo experimental. El
  botón cuenta solo lo que el plan aún no tiene, y se retira, diciéndolo, cuando todos los cambios están
  planificados.

  Cada entrada lleva el sello de esa nave y ranura, igual que la página de la nave sella las suyas, así que
  las dos páginas coinciden en lo que ya está planificado para esa nave. El mismo cambio planificado para
  otra nave sigue contando aquí como no planificado. Los dos botones de esta página reconocen las entradas
  del otro: la ingeniería de una ranura planificada como carencia en lo que tienes es el mismo trabajo que
  el cambio para esta nave, y la página de la nave cuenta igual la entrada de una configuración para su
  casco.
- **Frente a todo lo que tienes.** Cada módulo que pide la configuración frente a cada módulo que posees,
  según la política de compartir de la flota. Un módulo con el plano correcto en un grado inferior, o sin el
  efecto experimental, es **parcial** en vez de faltar. También lo es un módulo en el grado correcto al que
  aún no se le han aplicado todas las tiradas hasta su efecto completo; su texto de ingeniería dice cuánto
  lleva, como "Reforzado G3, 25%", y planificar la carencia pide solo las tiradas que aún se deben en ese
  grado. Un módulo modificado antes de que el juego guardara esa cifra se lee como completo.

  El estado se colorea según dónde está el módulo:

  - verde: instalado en una nave de este casco;
  - azul: en el almacén;
  - amarillo: instalado en una nave de otro casco;
  - naranja: en propiedad pero necesita un cambio, o en duda;
  - rojo: falta.

  Un módulo que vino con una nave de paquete no puede salir de ella, así que solo cuenta para configuraciones
  del casco de esa nave y se marca como **preconstruido** cuando lo hace. **Planificar las carencias en lo
  que tienes** hace por esta lista lo que el botón de arriba hace por una nave, y se retira de la misma
  manera. Los dos recuentos difieren a propósito: una ranura puede ser un cambio para esta nave mientras un
  módulo que posees en otro sitio ya la cubre.

Un módulo coincide dondequiera que esté en una ranura del mismo tipo, así que un módulo movido a otra bahía
opcional u otro anclaje sigue contando. **Copiar para EDSY** pone la configuración en el portapapeles en la
forma que EDSY importa: la exportación que pegaste, exactamente como la pegaste, así que nada de lo que
escribió EDSY se pierde en el camino de vuelta.

### Sistema

Para el sistema en el que estás: cada cuerpo que has escaneado, en esta visita o en una anterior que
conserve tu historial de diario, con una insignia **escaneado** que da el día cuando fue en una visita
anterior. Cuando la consulta de sistemas está permitida en Ajustes, los cuerpos que Spansh conoce y tú no has
escaneado se unen a la lista con una insignia **de Spansh**. Vienen con sus clases, condiciones, señales y
géneros conocidos. Su vida se valora como la tuya; su cartografía no vale nada para ti hasta que los escanees
tú mismo. No dicen nada sobre descubrimiento, cartografía o aterrizaje, así que no se supone para ellos
ninguna bonificación por ser el primero. Para un sistema que visitaste antes, la página dice dónde está y
cuándo estuviste allí.

Sobre los cuerpos, tres líneas resumen el sistema.

- **Datos de exploración sin vender** es lo que pagarían los datos de este sistema que aún llevas a bordo,
  con las bonificaciones contadas donde corresponden, y luego cuánto de ello es por ser el primero. Una vez
  vendido cualquier dato de este sistema, la línea termina con lo que te pagaron por él. Una venta de un solo
  sistema se toma tal como la informó el juego; una venta de varios se reparte entre ellos según la
  estimación de Katey de cada uno.
- La segunda línea desglosa la cifra. La cartografía se muestra como escaneada sin bonificación, y luego con
  la bonificación por primer descubrimiento donde un cuerpo era tuyo por descubrir. La exobiología se muestra
  por los géneros del escáner, "unos" hasta que cada género tiene una especie muestreada, y luego con la
  bonificación por primer registro donde nadie había puesto el pie.
- La tercera dice cuántos cuerpos no había descubierto nadie antes que tú, en cuántos has aterrizado y cuántos
  cuerpos aterrizables no ha pisado nadie todavía, tú incluido.

Los cuerpos se listan estrella por estrella: cada estrella, nombrada por su clase y si puedes repostar en
ella, va seguida de los cuerpos que la orbitan, los más cercanos primero. Ordenar por una columna pone toda
la lista en ese orden en su lugar. Cada columna recuerda su propio sentido, así que al volver a una la
encuentras como la dejaste; eso vale para todas las tablas de Katey.

Cada cuerpo muestra sus señales y materiales en superficie, y luego sus géneros, los más valiosos primero, con
sus predicciones de especie y estimaciones de pago. Donde nadie ha pisado el cuerpo, cada estimación lleva la
cifra con la bonificación por primer registro entre paréntesis, como "2,1M (10,5M) Cr". Un género es violeta
tras su primera muestra, azul tras la segunda y verde una vez tomadas las tres. La etapa se recuerda cuando
vuelves al sistema y después de vender, tal como la conserva el juego. Empezar otra especie antes de la
tercera muestra pierde la parcial, como hace el muestreador. Bajo los géneros está la propia cifra de
exobiología del cuerpo, "unos" hasta que cada género en él tiene una especie muestreada. Una vez que una
especie allí tiene las tres muestras, una línea en verde bajo esa cifra dice lo que has recogido en el cuerpo,
y en qué se convierte con la bonificación por primer registro.

La tarjeta y las insignias de cada cuerpo señalan lo que importa antes de comprometerte con él.

- El marco de la tarjeta dice qué tiene el cuerpo: verde para vida, azul para materiales en superficie; un
  cuerpo con ambos lleva verde. La tarjeta de una estrella va enmarcada en naranja y lleva una insignia
  **estrella**, con **repostable** en verde o **no repostable** en amarillo a su lado.
- Tres huecos de estado, siempre en el mismo orden, cada uno a partir de los indicadores que el juego escribe
  con cada escaneo: violeta para el descubrimiento, verde azulado para la cartografía, verde para la pisada.
  Una píldora tiene tres aspectos, así que un cuerpo intacto son píldoras rellenas, un cuerpo que trabajaste
  son palabras coloreadas y un cuerpo que trabajaron otros es gris, y sabes cuál es cuál sin leer las
  palabras.
  - Rellena con una línea debajo, la bonificación sigue abierta: **sin cartografiar**, que es donde está la
    bonificación por cartografía, o **sin pisadas**.
  - Perfilada con sus palabras en el color del hueco, la bonificación es tuya: **descubierto por ti**,
    **cartografiado por ti** o **pisada tuya**.
  - Perfilada con palabras grises, otro se la llevó: **descubierto**, **cartografiado**, **pisada**.
- El descubrimiento nunca está abierto, ya que has escaneado el cuerpo. El juego acredita un primer
  descubrimiento cuando se venden los datos, así que **descubierto por ti** se queda hasta entonces y después.
- Un primero sigue siendo tuyo cuando vuelves. Una vez que has vendido los datos de un sistema, los
  indicadores del juego informan de tu propio registro, así que Katey conserva como tuyo un cuerpo que
  descubriste o cartografiaste primero, diga lo que diga un escaneo posterior. Un cuerpo que pisaste primero
  es tuyo desde el aterrizaje. Un cuerpo que escaneaste pero nunca vendiste toma los indicadores posteriores
  tal como llegan, ya que lo que entonces significan es que otro vendió primero.
- Una estrella no tiene hueco de cartografía ni de pisada, y un cuerpo en el que nadie puede aterrizar no
  tiene hueco de pisada. Donde otro fue primero pero tú también cartografiaste o aterrizaste, una línea
  discreta bajo la tarjeta dice **Cartografiado por ti.** o **Has aterrizado aquí.**
- En un sistema poblado la pisada no puede reclamarse y el indicador del juego no dice nada, así que Katey
  trata cada cuerpo de allí como pisado. Un cuerpo que vino de Spansh dice **descubrimiento desconocido**,
  **cartografía desconocida** y **pisada desconocida**.
- Insignia **aterrizable** en verde: una nave puede aterrizar allí.
- La gravedad, la temperatura, la atmósfera y las notas de superficie se muestran en la línea del título
  junto a la distancia, que se ajusta según necesita, para que la tarjeta se mantenga corta. La gravedad se
  vuelve amarilla desde 1,0 g con una insignia **gravedad alta**, y roja desde 1,5 g con **gravedad
  extrema**: con esa atracción una nave pesada puede no ser capaz de frenar su descenso, así que acércate
  despacio y nivela pronto.

**Copiar nombre** pone el nombre del sistema en el portapapeles para el mapa de la galaxia. Cada nombre de
sistema en cualquier parte de Katey es un enlace a esta página.

Las predicciones de especie vienen de dónde se ha encontrado antes cada especie. Antes de cartografiar un
cuerpo, una señal de género se compara con su atmósfera, tipo, vulcanismo, gravedad, temperatura y presión;
una vez cartografiado, se usa la propia lista de géneros del escáner.

Hasta entonces la cifra de exobiología del cuerpo es una predicción, marcada "probablemente":

- cada género que puede vivir en las condiciones del cuerpo se pondera por la frecuencia con que se ha
  encontrado en tales condiciones;
- ese pago ponderado se multiplica por el número de señales biológicas;
- una línea dice cuántos géneros podrían vivir allí y nombra los tres más probables, cada uno con sus
  probabilidades, su especie más probable y su pago previsto antes de cualquier bonificación. Las señales
  pueden venir de cualquier género que las condiciones permitan, no solo de esos tres.

El recuento de señales se muestra como insignia. Una especie que nadie ha registrado en un cuerpo paga cinco
veces su valor base. El diario no puede decir si alguien lo ha hecho, así que cuando nadie había cartografiado
el cuerpo Katey toma la bonificación como probable y muestra la cifra por cinco, con la base a su lado. El
panel ordena los cuerpos por esa cifra, conocida o predicha, bonificación incluida, y luego por el recuento de
señales. La columna **Aterrizable** de la página del sistema ordena de la misma manera con los cuerpos
aterrizables primero.

### Ruta

Dos secciones:

- **La ruta del juego**: la que el juego ha trazado, estrella por estrella, con dónde estás primero en cada
  fila. Cada parada dice si es **nuevo para ti**, **visitado** o **explorado** por un escaneo de
  descubrimiento o una baliza de navegación, la distancia desde el inicio y el combustible previsto a la
  llegada.
  - El sistema que has fijado es siempre el siguiente salto. Fijado sin ruta, es una ruta de un salto. Fijado
    más adelante en la ruta, las paradas intermedias desaparecen. Fijado fuera de la ruta, la página muestra
    el salto hasta él y una línea que dice dónde se reanuda la ruta trazada y cuántos saltos quedan. La
    distancia del objetivo viene de tu historial o del trazado de Spansh, y se lee como 0 cuando ninguno
    conoce el sistema.
  - La proyección de combustible recorre la lectura del depósito del juego por los tramos que quedan con el
    consumo propio de la nave, según la propia ecuación del juego a partir de las masas, la carga y las
    cifras del motor que informa el diario. Está ahí desde el primer momento y dice cuántos saltos quedan
    antes de tener que repostar, antes de qué estrella repostable repostar, y lo que cuesta cada parada. Cada
    salto se calcula con el combustible que queda para él, así que se sigue el aligeramiento de la nave a
    medida que quema.
  - La carga son las toneladas a bordo ahora, y la proyección se rehace con cada actualización del diario,
    así que una venta o una recogida por el camino la cambia en el momento en que el juego la informa. Una
    nave cuyo equipamiento el diario no ha dado las masas recurre al consumo aprendido de sus saltos
    recientes. Una salida desde una estrella de neutrones o una enana blanca cuenta como salto con impulso, y
    un impulsor guardián como alcance extra.
  - La barra del depósito sobre las paradas se dibuja como el propio indicador del juego: el naranja es lo
    que queda en el depósito tras el siguiente salto, el azul es lo que quema ese salto, y la cifra azul junto
    a la lectura es el mismo consumo en toneladas.
  - Una vez que has saltado, el juego quita el combustible al instante y el depósito lo muestra, así que
    durante el salto la parada a la que llegas muestra la lectura del depósito y la barra azul pasa al salto
    siguiente.
- **El trazado de Spansh.** Con el trazador activado en [Ajustes](#ajustes), pide al trazador galáctico de
  Spansh una ruta desde donde estás hasta cualquier sistema, para una nave que posees o una configuración
  guardada. El trazado usa las propias masas, depósitos y cifras del motor de la nave: las del diario para
  una nave, las de la exportación de EDSY para una configuración. Los impulsos de neutrones, las inyecciones
  de FSD, los repostajes en estrellas secundarias y repostar en cada estrella repostable son opciones.
  - El selector empieza en la nave que pilotas. Al hacer clic se abre un panel con una caja de búsqueda vacía
    y la lista, en este orden: la nave o configuración que habías elegido primero, la nave que pilotas
    segunda, luego las configuraciones guardadas para el casco elegido, tus naves y las demás
    configuraciones. Cada una muestra su alcance con el depósito lleno, con la nave que pilotas en el color
    de acento. Escribe para reducir la lista por nombre o casco. Haz clic en una entrada, o pulsa Enter para
    la primera, para elegirla. Nada más cambia la elección.
  - Al trazador se le dice que el motor está sobrecargado cuando has tomado un impulso de cono de chorro
    desde tu último salto, o cuando estás en una estrella de neutrones con los impulsos de neutrones
    activados, así que el primer salto se planifica como uno con impulso.
  - Las cifras de motor de Katey son las mismas tablas con las que trabaja el propio sitio de Spansh,
    motores SCO y Mk II incluidos, así que un trazado pedido a través de Katey coincide con uno hecho pegando
    la configuración en el sitio.
  - El destino, el trazado y las opciones se guardan a medida que los cambias. La nave o configuración y la
    carga se quedan como las pusiste mientras se conserva el trazado, entre páginas y reinicios, hasta que
    pulsas **Olvidar el trazado**. Entonces la página se abre de nuevo en la nave que pilotas, con la carga
    que lleva rellenada, que puedes sobrescribir. Cambiar de nave o configuración deja la carga como está.
  - El trazado lista cada punto de ruta con su salto, la distancia restante, el combustible que el trazador
    espera a la llegada y si es una estrella de neutrones o una parada de repostaje. Sigue tus saltos para
    decir qué punto de ruta es el siguiente, copia ese nombre para el mapa de la galaxia y se conserva entre
    ejecuciones.
  - Deja el trazado para un desvío, para repostar por ejemplo, y el último punto de ruta que alcanzaste desde
    que se hizo el trazado se marca **última conocida** mientras el siguiente sigue siendo el siguiente punto
    de ruta, así que **Copiar** sigue dándote la estrella de neutrones a la que apuntar. Un trazado hecho
    desde un sitio en el que no estás no tiene último punto de ruta conocido hasta que alcanzas una de sus
    paradas, aunque hayas pasado por algunas de ellas antes.

### Datos

Tu historial de diario leído como un libro de cuentas. Sus secciones:

- **Ingresos.** Un gráfico de anillo de lo que has ganado por actividad, de las recompensas a la
  exobiología, con un interruptor por actividad que redibuja el gráfico sin ella, y un selector de periodo
  desde todo el tiempo hasta la última semana.
- **Exobiología.** Lo que llevas a bordo sin vender: muestras, cuántas son probablemente primer registro, y
  su valor con y sin la bonificación. Luego lo que ganaron los datos orgánicos con y sin la bonificación por
  primer registro, y cada especie vendida.
- **Exploración.** Lo que llevas a bordo sin vender: sistemas, cuerpos escaneados y cartografiados, y su valor
  cartográfico estimado. Luego las ventas del periodo junto a las cifras históricas del juego.
- **Combate, comercio y minería, misiones y otros, Powerplay.** Las cifras propias de cada actividad en el
  periodo, junto a las cifras históricas del juego para ella.
- **Alas y tripulaciones.** Tiempo volado con otros y lo que el juego marcó como compartido, por tipo. Cada
  comandante con el que has volado se lista con el tiempo juntos y lo que se compartió mientras estaba
  presente. Cada misión de ala aceptada se lista con quién estaba en el ala en ese momento y cómo está.
  Volar en ala mientras exploras por tu cuenta no comparte nada, así que aquí no cuenta nada. El diario no
  dice si una misión de ala vino del tablón o de un compañero de ala, así que se listan ambas.
- **Cifras históricas.** Cada total acumulado que informa el juego, tiempo jugado incluido. Un interruptor
  recordado lista también las cifras a cero; está desactivado de forma predeterminada. El selector de periodo
  se oculta aquí, ya que son los propios totales del juego.

### Ajustes

- **Actualizaciones.** La versión que estás usando y, con el interruptor de GitHub activado, si existe una
  versión más nueva. Katey pregunta una vez al arrancar y cada vez que pulsas **Comprobar ahora**, en segundo
  plano, así que arrancar no tarda más. Cuando hay una versión más nueva, **Abrir la página de la versión** la
  abre en tu navegador; no se descarga ni se instala nada por ti.
- **Tamaño del texto.** Cuán grande dibujan todo las ventanas, como porcentaje del tamaño estándar, para
  quienes necesitan letra más grande. El texto y los espacios crecen juntos, así que las páginas conservan su
  forma; separa el panel o ensancha la ventana si una página ya no cabe. La elección se recuerda. Si la
  pantalla es demasiado pequeña para las páginas ampliadas, la ventana se detiene en el borde de la pantalla y
  las páginas se desplazan de lado.
- **Carpeta de diarios.** La carpeta que Katey sigue. **Detectar** busca en los sitios habituales,
  **Examinar** elige una, o escribe una ruta; el seguimiento pasa a la carpeta al instante y reproduce su
  historial. La línea de estado dice qué está haciendo el seguimiento.
- **Copia de seguridad del diario.** Una carpeta a la que se copia toda la carpeta de diarios, solo lo nuevo o
  modificado, cuando Katey se cierra y cada vez que pulsas **Copiar ahora**. Apúntala a una carpeta que
  sincronice OneDrive u otra nube y tus diarios saldrán de la máquina con ella.
- **Datos guardados.** La carpeta en la que Katey guarda sus propios datos; consulta [Tus datos](#tus-datos).
  Elige otra y los datos se mueven allí al instante. Si la carpeta ya contiene un archivo de datos de Katey,
  se carga; si no, los datos actuales se escriben allí, y los datos del sitio antiguo se dejan como estaban.
  Dos máquinas apuntadas a una misma carpeta sincronizada comparten un plan, un conjunto de ingenieros y un
  conjunto de configuraciones.
- **Conexiones salientes.** El único sitio que decide qué puede enviar Katey por la red, un interruptor por
  cada tipo de petición, todos desactivados hasta que los marcas. Hay tres tipos. Dos van a
  [Spansh](https://spansh.co.uk/): pedir una ruta cuando pulsas **Trazar**, y preguntar por los cuerpos del
  sistema al que acabas de llegar. El tercero va a
  [GitHub](https://github.com/temetvince/elite-dangerous-katey/releases) y pide la última versión publicada
  de Katey. Los interruptores se recuerdan.

## Trabajar con EDSY

[EDSY](https://edsy.org/) es el constructor de naves que usa la mayor parte de la comunidad, y Katey habla
sus formatos de importación y exportación en ambos sentidos.

- **Una configuración al plan.** En EDSY, abre la configuración y elige **Export**, y luego el texto Loadout
  al estilo del diario. Pégalo en la caja de importación del cajón del plan y pulsa **Leer exportación**.
  Cada módulo con ingeniería se convierte en una entrada con su plano, grado y efecto experimental; desmarca
  los que no quieras y pulsa **Añadir al plan**.
- **Una configuración como configuración guardada.** Pega el mismo texto en la caja de configuraciones
  guardadas de la página [Flota](#flota). La configuración se compara entonces con tus naves y tus módulos
  en propiedad. Pegar una exportación más reciente de una configuración que ya guardaste, mismo nombre y
  mismo casco, reemplaza la configuración guardada tras tu confirmación.
- **Una nave o una configuración de vuelta a EDSY.** **Copiar para EDSY** en la página de una
  [nave](#una-nave) o una [configuración](#una-configuración) pone un Loadout en el portapapeles. En EDSY
  elige **Import** y pega.

## Naves preconstruidas

Una nave canjeada de un paquete de la tienda del juego viene con módulos que Katey marca como
preconstruidos mientras sigan en las ranuras en las que vinieron. Comprar, recuperar, intercambiar,
almacenar o vender en una ranura termina la marca, muestre lo que muestre el juego como valor del recién
llegado, igual que un módulo comprado del mismo tipo que ocupe la ranura. Los módulos de paquete del juego
no pueden almacenarse, moverse a otra nave ni venderse. Modificar un módulo así es ingeniería que se queda
con esa nave. Katey marca estos:

- La página [Flota](#flota) marca una nave canjeada como **preconstruida**.
- La página de una [nave](#una-nave) marca cada módulo así como **Módulo preconstruido** junto a su selector
  de ingeniería, y la comparación de una [configuración](#una-configuración) avisa cuando la ranura por
  modificar lleva uno.
- La página de una nave también dice si una nave preconstruida **sigue como se canjeó**, de modo que
  venderla y canjearla de nuevo no pierde nada, o cuántas ranuras han cambiado desde entonces. La línea se
  abre en una tabla de lo que traía cada ranura cambiada y lo que tiene ahora. Un módulo preconstruido
  cambiado por uno comprado del mismo tipo cuenta como cambio, ya que el comprado puede salir de la nave.

Una nave preconstruida que nunca has desplegado no existe para el juego, así que no puede aparecer en la
flota.

## Lo que el diario no puede decirle a Katey

Katey solo sabe lo que el juego escribe. De ahí se siguen algunos límites:

- Si otro ha descubierto un sistema solo se sabe cuando escaneas un cuerpo en él. Antes de llegar, Katey
  solo puede decir si *tú* has estado allí.
- El diario no registra quién descubrió o cartografió primero un cuerpo, ni quién aterrizó primero en él,
  solo si fuiste tú el primero.
- La lista de la flota y la lista de módulos almacenados se reescriben cuando atracas en algún sitio con
  astillero o equipamiento, así que están tan al día como tu última visita de ese tipo. De un módulo
  almacenado se registran el plano y el grado pero no su efecto experimental, que el comprobador de
  configuraciones informa como incierto.
- El diario no dice si una misión de ala vino del tablón de misiones o de un compañero de ala.
- Los cuerpos se muestran para el sistema en el que estás; un sistema anterior muestra dónde está y cuándo
  estuviste allí.
- Un cuerpo que escaneaste en una visita anterior ya está en tus datos, así que el juego no escribe nada
  cuando el escaneo de descubrimiento lo vuelve a encontrar. Katey recuerda cada cuerpo que escaneaste en
  cualquier visita dentro de tu historial de diario y lo lista de nuevo cuando vuelves, marcado con el día en
  que se escaneó. Un cuerpo escaneado antes de tu archivo de diario más antiguo no puede recuperarse así.
  Cuando el escaneo de descubrimiento informa de que todo el sistema ya es conocido, Katey cuenta cada cuerpo
  como escaneado pero solo puede listar los que tiene, o los que Spansh conoce cuando ese interruptor está
  activado.
- Los cuerpos marcados **de Spansh** son lo que Spansh conoce, que puede ser más antiguo que el sistema: una
  exploración de hace años, o solo parte de un sistema. Katey pregunta por un sistema una vez, cuando llegas
  a él, y conserva la respuesta durante la ejecución; si no se puede contactar con Spansh, espera cinco
  minutos antes de volver a preguntar por ese sistema.

## Tus datos

Katey conserva un archivo: el plan, la selección de ingenieros, las configuraciones guardadas, las reservas
de materiales, la carpeta de diarios, la carpeta y el interruptor de la copia de seguridad, los
interruptores de conexiones salientes y el último trazado, la colocación y los ajustes del panel, y cada
elección de vista recordada. De forma predeterminada vive bajo tu carpeta local de datos de aplicación, en
`Katey\state.json`. La caja **Datos guardados** de [Ajustes](#ajustes) lo mueve a cualquier sitio, una
carpeta sincronizada incluida. Cuando una actualización retira o renombra un ajuste, Katey descarta el valor
antiguo y parte del predeterminado, así que el archivo nunca lleva ajustes que nada lee.

Katey lee los archivos de diario en local y no envía nada a ningún sitio salvo que lo permitas bajo
**Conexiones salientes** en [Ajustes](#ajustes), donde todo está desactivado al principio. Existen tres tipos
de petición. Dos van a [Spansh](https://spansh.co.uk/): cuando pulsas **Trazar**, dos nombres de sistema y
las cifras de salto de la nave; y, al llegar a un sistema, el identificador de ese sistema, que trae los
cuerpos que Spansh conoce allí. El tercero va a GitHub cuando Katey arranca y cuando pulsas **Comprobar
ahora**: una petición de la última versión publicada del proyecto, que no lleva nada tuyo. Cada petición
indica Katey, su versión y la dirección del proyecto, como Spansh pide a las herramientas que usan su
servicio. La copia de seguridad del diario copia archivos solo a la carpeta que eliges.

Katey también conserva un registro, `katey.log`, junto al `state.json` predeterminado bajo tu carpeta local
de datos de aplicación. Anota lo que Katey hizo y lo que salió mal: el inicio y la parada, la carpeta de
diarios que siguió, las peticiones que hizo y los fallos que encontró, y el motivo si alguna vez se cierra
por sí solo. Si Katey se cierra de forma inesperada, ese archivo dice por qué, y es lo que hay que enviar con
un informe de error. Se renueva al pasar de un megabyte, así que se mantiene pequeño.

El registro contiene nombres de sistemas estelares y de naves de tu diario y nada más sobre ti: ni nombre de
comandante, ni créditos, ni posiciones, ni nombres de carpeta. Los propios mensajes de Katey nunca llevan
una ruta de archivo. Cualquier otra cosa que llegue al registro, un error de Windows por ejemplo, conserva
solo un nombre de archivo, con los nombres de tu cuenta y de tu máquina reemplazados, así que el archivo
puede compartirse tal cual.

Para un error que necesite más detalle, inicia Katey como `katey.exe --verbose` o
`katey.exe --log-level debug`. El registro anota entonces también las páginas abiertas, las decisiones del
panel y las consultas hechas; `--log-level warning` conserva solo los problemas. Vuelve a la normalidad la
próxima vez que inicies sin la opción. `katey.exe --pseudo-loc` muestra cada palabra más larga, acentuada y
entre corchetes, que es como un traductor o un informe de error encuentra texto que se corta o que no ha
pasado por las tablas.

Si el archivo de diario del día desaparece mientras el juego se ejecuta, el juego lo escribe de nuevo desde
el principio, y todo lo que contenía se pierde para el juego y para Katey. La copia de seguridad conserva su
copia anterior de tal archivo junto a la nueva, nombrada con un sufijo `rewritten`. Hasta tu siguiente salto
Katey toma tu posición del archivo de ruta del juego, cuya primera parada es el sistema en el que estás, así
que el panel y la página de ruta siguen bien; los cuerpos del sistema están vacíos hasta que escanees de
nuevo.
