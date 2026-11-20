# MULTIPLAYER_PROBA

Documentacion tecnica de la prueba multijugador realizada en `Assets/MULTIPLAYER_PROBA`.

La practica utiliza Unity Netcode for GameObjects, Unity Multiplayer Services con Relay, Unity Input System, Cinemachine y NavMesh. La escena principal es [MultiplayerProba_Mario.unity](../Scenes/MultiplayerProba_Mario.unity).

## Resumen del sistema

El proyecto implementa una partida con dos tipos de participantes:

- **Anfitrion (Armiarma)**: crea la sesion, controla el inicio de la partida y recibe el modelo rojo.
- **Clientes (Iheslariak)**: se conectan mediante codigo, reciben el modelo azul y pueden ganar puntos por entrar en las zonas activas.

El servidor mantiene el estado importante de la partida: puntuaciones, inicio de partida, congelacion de jugadores y posicion inicial. Los clientes envian acciones mediante RPC y observan los cambios a traves de `NetworkVariable` y `NetworkList`.

## Dependencias principales

Segun `Packages/manifest.json`:

- Netcode for GameObjects `2.13.3`
- Multiplayer Services `2.3.3`
- Multiplayer Tools `2.2.12`
- Input System `1.20.0`
- Cinemachine `3.1.7`
- AI Navigation `2.0.14`
- TextMesh Pro, incluida con el paquete UGUI

La sesion se crea con `SessionOptions.WithRelayNetwork()`, por lo que usa Unity Relay como transporte.

## Escena principal

La escena [MultiplayerProba_Mario.unity](../Scenes/MultiplayerProba_Mario.unity) contiene:

- `NetworkManager` y el jugador configurado como prefab predeterminado.
- La interfaz de creacion y union a partidas.
- Los puntos de salida del host y los clientes.
- Las zonas de puntos `Point_Zone_A` y `Point_Zone_B`.
- Las barreras iniciales que impiden comenzar el recorrido.
- Los muros y zonas de muerte.
- El `ScoreManager`, el `GameManager` y el controlador de escena.
- El sistema de NavMesh y el NPC.

El prefab [Player_Mario_v3.prefab](Player_Mario_v3.prefab) es el objeto que NetworkManager instancia para cada cliente. El prefab [NPC_Mario.prefab](NPC_Mario.prefab) representa al enemigo/NPC de la prueba.

## Creacion y union a una partida

El flujo comienza en `GameKudeatzaile`.

1. El usuario introduce su nombre.
2. Al pulsar **Crear partida** se llama a `SessionManager.StartSessionAsHost()`.
3. Al pulsar **Unirse** se llama a `SessionManager.StartSessionAsClient(joinCode)`.
4. `SessionManager` inicializa Unity Services y autentica anonimamente al jugador.
5. El host crea una sesion con un maximo de diez jugadores.
6. El cliente se une mediante el codigo de la sesion.

La inicializacion se almacena en una tarea reutilizable para evitar inicializar Unity Services varias veces.

### Posicion inicial

`GameKudeatzaile` se ejecuta en el servidor y asigna las posiciones:

- El cliente cuyo `ClientId` coincide con `NetworkManager.ServerClientId` aparece en `hostSpawnPoint`.
- Los demas jugadores aparecen aleatoriamente dentro del rectangulo formado por `clientSpawnPoint_1` y `clientSpawnPoint_2`.

La posicion se aplica mediante `SetInitialSpawnPositionRpc`, enviada al propietario del objeto. Esto evita que el cliente modifique directamente el estado autoritativo del servidor.

## Scripts

### `SessionManager.cs`

Gestiona Unity Authentication y Unity Multiplayer:

- Inicializa los servicios de Unity.
- Inicia sesion anonima.
- Crea sesiones Relay con diez plazas.
- Permite unir a una sesion mediante su codigo.

### `GameKudeatzaile.cs`

Actua como controlador de la interfaz y coordinador de entrada:

- Valida el nombre del jugador.
- Llama a crear o unir una sesion.
- Oculta el panel inicial mientras se conecta.
- Actualiza el nombre local.
- Coloca a los jugadores en sus puntos de salida.
- Gestiona el aviso de nombre obligatorio.

### `PlayerIzena.cs`

Mantiene el nombre sincronizado:

- Usa una `NetworkVariable<FixedString64Bytes>`.
- Solo el servidor puede escribir el nombre.
- El propietario envia su nombre mediante un RPC protegido con permiso `Owner`.
- Todos los clientes reciben la etiqueta visual actualizada.

### `NetworkPlayerMovement.cs`

Contiene el movimiento y la logica multijugador del jugador.

Las entradas se leen con el componente `PlayerInput`:

- `Move`: desplazamiento hacia delante y giro lateral.
- `Sprint`: aumenta la velocidad.
- `HasiJokoa`: permite al host iniciar la partida.
- `Interact`: solicita un punto adicional al servidor.

Solo el propietario ejecuta la lectura de entrada y el movimiento local. Las acciones sensibles se validan o ejecutan en el servidor.

`IsFrozen` es una `NetworkVariable<bool>` con lectura para todos y escritura solo para el servidor. Cuando cambia:

- Se activa o desactiva el componente `PlayerInput`.
- Se reinician las animaciones si el jugador queda congelado.
- El sistema de apariencia cambia el material del personaje.

### `PlayerModelSelector.cs`

Selecciona el modelo segun el rol de red:

- El modelo asignado a `hostModel` corresponde al jugador del servidor.
- El modelo asignado a `clientModel` corresponde al resto de clientes.
- Activa el modelo seleccionado y desactiva el otro.
- Obtiene el `Animator` que utiliza `NetworkPlayerMovement`.
- Notifica a `PlayerAppearance` cuando termina la seleccion.

La seleccion se realiza en `OnNetworkPostSpawn()`.

### `PlayerAppearance.cs`

Sincroniza el aspecto visual:

- El host usa `hostMaterial`.
- Los clientes usan `clientMaterial`.
- Los clientes congelados usan `frozenMaterial`.
- Actualiza todos los `Renderer` del modelo seleccionado.

La logica escucha cambios en `IsFrozen` y en el evento `ModelInitialized`.

### `PlayerCameraOwner.cs`

Activa el rig de Cinemachine solo para el propietario del objeto. Cada jugador recibe una camara propia, mientras que las camaras de los otros clientes permanecen desactivadas.

### `GameManager.cs`

Controla el estado global `GameStarted`.

Al iniciarse la partida:

- La `NetworkVariable<bool> GameStarted` pasa a `true`.
- Las barreras visuales desaparecen.
- Los colliders de las barreras se desactivan.

Solo el servidor puede iniciar la partida y solo se puede hacer una vez por sesion. El host puede iniciarla desde la accion `HasiJokoa`.

### `ScoreManager.cs`

Mantiene dos estructuras sincronizadas:

- `TotalScore`: puntuacion global.
- `NetworkList<PlayerScoreEntry>`: puntuacion por jugador.

`PlayerScoreEntry` incluye `ClientId`, nombre fijo de red y puntuacion. Implementa `INetworkSerializable` y `IEquatable`, por lo que puede serializarse dentro de `NetworkList`.

El servidor incrementa las puntuaciones y actualiza el texto global. `AddPlayerPoint()` combina por `ClientId` y evita crear entradas duplicadas para el mismo jugador.

### `Leaderboard.cs`

Lee `ScoreManager.Instance.PlayerScores`, escucha cambios de `NetworkList` y genera una clasificacion descendente.

Tambien vuelve a enlazar con el `ScoreManager` si la instancia cambia durante el ciclo de vida de red.

## Puntos y colisiones

Cada jugador tiene una capa segun su rol:

- `Armiarma` para el servidor o host.
- `Iheslariak` para los clientes.

Cuando un cliente entra en una zona etiquetada como `Point_Zone`, solicita al servidor incrementar su puntuacion individual. Solo se aplica si el propietario coincide con el remitente del RPC.

La logica de colision congela y libera jugadores:

- Un jugador no congelado que colisiona con `Armiarma` pasa a `IsFrozen = true`.
- Un jugador congelado que colisiona con otro jugador `Iheslariak` pasa a `IsFrozen = false`.

Las colisiones las detecta el propietario, pero las modificaciones reales las decide el servidor para evitar cambios locales inconsistentes.

## NPC y NavMesh

La escena usa una superficie de NavMesh para el NPC.

### `SimplePatrolBehavior.cs`

Componente local sin `NetworkBehaviour` que:

- Recorre una lista de puntos.
- Espera en cada punto.
- Utiliza `NavMeshAgent`.
- Dibuja el recorrido y el estado mediante Gizmos.

### `SimpleChaseBehavior.cs`

Logica de persecucion autoritativa:

- Solo el servidor mueve al agente.
- Busca el jugador `Iheslariak` mas cercano.
- Ignora al jugador de la capa `Armiarma`.
- Actualiza el destino a intervalos configurables.
- Los clientes reciben la posicion mediante sincronizacion de red.

### `SimpleAttackBehavior.cs`

Contiene la busqueda de objetivo, distancia, orientacion y cooldown del ataque. En la implementacion actual, el bloque que ejecuta el ataque esta desactivado, incluido el daño al componente de salud, que no existe en esta prueba. Sirve como base didactica para completar el comportamiento de ataque.

## Convenciones de red

La implementacion sigue estas reglas:

- El servidor escribe variables globales sensibles.
- Los propietarios leen las entradas y envian solicitudes.
- Los RPC al servidor incluyen validacion de remitente cuando es necesaria.
- Las posiciones iniciales se pueden enviar al propietario.
- `OnNetworkSpawn` y `OnNetworkDespawn` se usan para suscripciones y limpieza.
- Las referencias a objetos se envian mediante `NetworkObjectReference`.

## Ejecucion

1. Abrir el proyecto con una version compatible de Unity.
2. Abrir `Assets/Scenes/MultiplayerProba_Mario.unity`.
3. Configurar los servicios de Unity para el proyecto.
4. Ejecutar una instancia como host.
5. Ejecutar otra instancia como cliente.
6. Introducir un nombre y crear la sesion desde el host.
7. Copiar el codigo mostrado y utilizarlo en el cliente.
8. El host inicia la partida con la accion configurada como `HasiJokoa`.

Para pruebas locales es posible utilizar Multiplayer Play Mode, pero las conexiones Relay requieren proyectos y servicios configurados en Unity.

## Estado actual y posibles ampliaciones

- Completar el ataque del NPC con salud y confirmacion de daño.
- Añadir inicio y final de partida con temporizador.
- Reiniciar puntuaciones y jugadores al comenzar una ronda.
- Añadir reconexion y gestion de desconexion.
- Validar y limitar nombres vacios o excesivamente largos.
- Sustituir las comparaciones por nombre de capa por IDs de capa cacheados.
- Evitar que `GameManager` y `ScoreManager` creen instancias duplicadas.
- Añadir pruebas automatizadas de movimientos, puntuaciones y permisos de RPC.
