# CyberDefenseCoop — misión cooperativa SQL Injection

Actualización: 3 de noviembre de 2026.
Estado: implementación conectada y comprobaciones de C# y lógica realizadas. NO equivale a una validación de la partida real. Falta importar/procesar con Fusion, generar el build y comprobar visualmente ambos clientes.

## Antes de probar

1. Detener Play. Revisar/guardar manualmente cualquier escena con cambios propios antes de recargarla. No se descartaron escenas desde esta implementación.
2. Dejar que Unity termine de importar y compilar. Abrir Console y corregir cualquier error real antes de continuar.
3. Ejecutar **CyberDefense → Validar configuración de la misión**. Comprueba los siete pasos, modelo de tablet, temas y audio. Este menú todavía debe ejecutarse en tu Editor.
4. Ejecutar **CyberDefense → Crear build Windows en carpeta nueva**. No sobrescribe builds: usa una carpeta con fecha/hora y se niega a construir si hay escenas sin guardar.
5. Usar dos ejecutables de ese MISMO build (o Editor y build actualizado). No conectar una versión anterior: cambió el esquema de propiedades Networked.

## Controles

- WASD: caminar; ratón: mirar; espacio: saltar; Shift: correr sin tablet.
- TAB: sacar/guardar tablet. Al abrir, entra en consulta: permite caminar despacio y mirar.
- F1: alternar cursor/interacción de tablet. En este modo se detiene el movimiento.
- Mientras un campo de texto tiene foco, las teclas no mueven al personaje ni abren otras herramientas.
- E cerca de una estación: abrir la PC/terminal propia del rol.
- ESC: campo de texto → modo consulta/cerrar PC → pausa. Una pulsación no debe ejecutar dos niveles.
- La pausa es individual; no modifica el tiempo compartido.
- El chat y la voz se conservan. El HUD indica MIC ACTIVO al usar push-to-talk; no es una prueba de que el dispositivo esté transmitiendo correctamente.

## Recorrido configurado

1. Menu → sala → Game + Environment 1 → roles distintos y ambos LISTO.
2. Tutorial/exploración libre por rol. TAB, F1 y pestaña TUTORIAL. Ambos completan las páginas o pulsan explícitamente omitir y preparado. El reloj de misión aún no corre.
3. Alerta e inicio del cronómetro. Pentester localiza **SRV-01**, introduce el ID en su tablet cerca del servidor y conecta. Un ID incorrecto no resta puntos.
4. Iniciar análisis: barra de 5 segundos. Cerrar la pantalla permite continuar mientras permanece cerca. Alejarse cancela sin éxito ni recompensa.
5. E abre la estación del servidor. Seleccionar las tres evidencias observadas: concatenación, alteración de consulta y acceso sin sesión. El correo de phishing es un distractor.
6. Pestaña TICKETS: diagnóstico, gravedad, recomendación, servicio, evidencias y comentario opcional. Los campos comienzan sin elegir. Puede redactar/enviar desde cualquier lugar después de analizar.
7. Administrador recibe resumen/código en su tablet. E cerca de **PC ADMIN**, pestaña TICKETS, introducir código TK-001-001 (o el mostrado). El informe completo se abre al validar el código.
8. Puede devolverlo. Un informe incorrecto se penaliza una sola vez al revisarlo; si además el Administrador intenta aceptarlo, se registra su decisión incorrecta. Nunca se acepta un reporte inválido ni se bloquea una corrección.
9. Con reporte aceptado, OPERACIONES en PC: contener **Portal de matrículas** (3 s).
10. Reparación: seleccionar en orden sustituir concatenación por parámetros → probar entradas y acceso legítimo → desplegar en servicio aislado (5 s). Reiniciar sin corregir no resuelve.
11. Pentester, en servidor: evaluar ambas pruebas simuladas y verificar (4 s). Servicio restaurado y resultado para ambos.
12. HISTORIAL muestra tiempo, tutorial, puntos, contribuciones, versiones de tickets, errores, pistas y cronología. Repetir/intercambiar requiere votos coincidentes de ambos. Volver al menú cierra el Runner por el camino existente.
13. Si alguien sale durante tutorial/misión, se interrumpe. El restante consulta el informe y vuelve al menú; no se sustituye al compañero ni se reinicia automáticamente. Si la conexión local se pierde, el menú muestra la última instantánea recibida, claramente etiquetada.

## Configuración editable y conexión

- **Assets/Scripts/Roles/SQLInjection.asset**: siete pasos explícitos; ID SRV-01; radio 4 m; operaciones 5/3/5/4 s; velocidad con tablet 0.65; penalización 10; pistas 3; error sin descuento de tiempo ni espera añadida (penaltyDelay=0).
- **Assets/Resources/MissionPresentation.asset**: referencia al prefab de tablet existente de iPoly3D, sonidos Click/Notification de Heat, temas y transformaciones de la tablet local. Transiciones 0.25 s.
- **Assets/Resources/PentesterSkin.asset** y **AdminSkin.asset**: verde/negro y azul/cian/negro. Se cargan desde el asset de presentación, no hace falta arrastrarlos a mano a la escena.
- Game conserva su **RoleSystem / CooperativeIncident** registrado en NetworkedBehaviours y su referencia a SQLInjection.asset. No se agregó otro objeto coordinador de red.
- El bool TabletRaised se añadió al PlayerRole ya registrado en los prefabs. GetPlayerCameraAndControls crea la presentación visual en cada instancia; solo el dueño controla su bit.
- Las estaciones existentes ServidorRed/ConsolaControl se reutilizan. Las etiquetas y luces se crean localmente sobre ellas. Environment 2 no tiene estas estaciones: no cambiar de escenario para esta misión.
- Heat sigue gestionando menú/pausa. El adaptador centraliza ESC y el bloqueo; las animaciones no hacen avanzar la red.

## Arquitectura y comportamiento

CooperativeIncident conserva la autoridad de tutorial, cronómetro, operaciones, tickets, puntuación, revisiones e historial. IncidentEngine evalúa los pasos. TicketRules valida estructura y coherencia educativa del reporte. RoleTerminalUI gestiona borrador, cursor y páginas locales. MissionPresentationConfig/TabletPresentation/MissionWorldPresentation gestionan presentación.

Los RPC comprueban emisor activo, rol, incidente, revisión, paso y proximidad cuando corresponde. Las solicitudes antiguas y duplicadas no vuelven a aplicar puntos. RPC_Hint exige el rol del paso, limita los bits e incrementa revisión. Un ticket no necesita proximidad para redactarse/enviarse; identificar, analizar, revisar en PC y operar sí.

El escenario y la reparación son simulados: no hay red externa de ataque, ejecución SQL ni reinicios reales. Shared no equivale a un servidor dedicado antitrampas.

## Verificación realizada

- C# compilado con referencias locales para Editor y Player: sin errores; advertencias previas de BugSimulator y SetupWizard.
- **51 comprobaciones reproducibles** en Tests/Mission: tutorial, permisos, ID, operaciones/cancelación, pistas/doble clic, reportes incorrectos, revisión/corrección, reparación, verificación, cronómetro, repetición e interrupción.
- Estas pruebas compilan los scripts reales contra sustitutos mínimos de Unity/Fusion. NO ejecutan transporte, física, Input System, IL weaving ni renderizado.
- Intento de build por línea de comandos: Unity exit 1; no se generó un ejecutable validado. El control de la ventana del Editor agotó el tiempo de respuesta, por lo que no se verificaron capturas de la UI.
- No se instalaron paquetes, no se hicieron commits, no se borraron assets ni se modificaron escenas de usuario durante esta etapa. Se conservaron cambios de metadatos .plastic aparecidos durante el trabajo.

## Pendientes y límites explícitos

- Es obligatorio validar importación, referencias reales del prefab, procesamiento Fusion y dos clientes. La compilación C# sola no demuestra esto.
- La postura remota al sostener tablet es procedural/provisional. Se reutiliza el modelo existente; falta ajustar artísticamente posición, orientación y brazos según la prueba visual. No se creó un rig nuevo de manos en primera persona.
- La malla del avatar propio pasa a ShadowsOnly en su instancia local para evitar ver rostro/interior. El compañero conserva el modelo completo. Falta probar mirada abajo, paredes, movimiento y escala de cámara en ambos.
- Historial replicado acotado: últimas 64 entradas (detalle de hasta 64 caracteres) y últimas 16 versiones de ticket; si se excede, se informa y los totales siguen conservados. No hay almacenamiento permanente entre sesiones.
- El código del ticket es una referencia de consulta, no una contraseña ni un mecanismo de seguridad.
- El flujo de tickets de esta entrega exige los siete pasos configurados; crear otro ataque requiere adaptar sus reglas/flujo, no solo cambiar su título.
- Falta revisar layout, tamaños y contraste a 1920×1080 y otras resoluciones, audio y uso simultáneo de chat/voz/tablet.

## Matriz mínima de aceptación real

Repetir con creador de sala como Pentester y luego Administrador:
- Dos avatares independientes; caminar/correr/saltar/cámara.
- Tutorial: uno preparado no inicia; dos sí. Omitir queda registrado.
- TAB/F1/ESC, campos de texto, chat y pausa sin bloqueos persistentes.
- ID incorrecto sin penalización; barra no da éxito al cancelar/alejarse.
- Ticket erróneo, devolución, corrección, código erróneo y aceptación.
- Doble clic en enviar/pista/aceptar/reparar: un único efecto.
- Roles no pueden usar las herramientas exclusivas del compañero.
- Contener → reparar → verificar; mismos tiempos/puntos/resultados en ambos.
- Informe scrollable y explicación final; repetición coordinada e intercambio.
- Desconexión de cualquiera durante tutorial, análisis y reparación: sesión interrumpida.
- Salir → menú → nueva sala, sin Runner duplicado ni estado residual.

## Correccion del bloqueo de seleccion de roles
Los registros de Unity y del ejecutable mostraron un AssertException al registrar RoleSystem: 87.724 bytes frente al limite de 32.768 de Fusion. Al no inicializarse RoleManager, los botones permanecian deshabilitados. Se redujeron las capacidades del historial y se agregaron validaciones del tamano tejido por Fusion antes del build y del tamano de cada NetworkObject al procesar escenas. Se conservaron los comentarios de tickets de 128 caracteres.
Verificacion: 47 pruebas de logica con sustitutos del motor aprobadas y compilacion C# con bibliotecas locales aprobada. La ejecucion batch de Unity fue rechazada por otra instancia con el proyecto abierto. Pendiente: recompilacion/weaving en Unity y recorrido con dos clientes nuevos; los ejecutables anteriores conservan el error. No se ha comprobado el clic en ejecucion tras este cambio.

## Tickets y tablet importada
- Corregido RPC_Ticket: el parametro NetworkString<_128> reservaba 516 bytes, y el mensaje completo excedia 512 bytes (552 observados). Ahora usa string UTF-8 limitado antes del envio y de nuevo al recibir a 128 caracteres UTF-16; el registro de red sigue usando NetworkString<_128>. Tambien se corrigio RPC_Error, que utilizaba el mismo almacenamiento fijo sobredimensionado.
- Asset utilizado: Assets/Security Systems/Free Version/Prefabs/TabletDisplay01.prefab, Tablet based Single Camera System 2.0. MissionPresentation.asset lo referencia. TabletPresentation adapta solo la instancia: desactiva los controles de demostracion dependientes de la camara global y de F12, ajusta el ancho a 32 cm, agrega pantalla de estado por rol y conserva la animacion Tab y el estado compartido TabletRaised.
- La pantalla fisica muestra estado; los formularios interactivos siguen en RoleTerminalUI (Tab/F1). No se ha integrado videovigilancia ni trasladado los formularios al cristal 3D. No se modificaron los archivos originales del asset. Posicion y rotacion siguen siendo ajustables en MissionPresentation.asset.
- Validacion: 51 pruebas de logica aprobadas (incluyen UTF-8, limite del comentario y pares sustitutos); compilacion C# de Editor y jugador con bibliotecas locales aprobada. Estas pruebas no ejecutan el transporte real ni validan el aspecto visual. Pendiente: verificar importacion/weaving en Unity, posicion y orientacion del modelo, y envio/recepcion del ticket en dos clientes recompilados.
- Prueba: detener Play, dejar compilar; crear build nuevo para ambos clientes. Elegir roles, completar tutorial, investigar y enviar ticket con comentario de 128 caracteres (tambien acentos). Comprobar codigo en tablet Admin y abrirlo en PC. Probar Tab/F1 en ambos, caminar con tablet en consulta y comprobar que no se abre la tablet del otro jugador. Confirmar ausencia de payload is too large y errores de TabletDisplayController.
## Interfaz interactiva en el cristal de la tablet
Actualiza la integracion anterior: RoleTerminalUI aloja ahora su panel completo dentro del Canvas WorldSpace de la tablet local (TabletPresentation.LocalScreen). El PC mantiene su vista de estacion. Las copias remotas solo muestran estado publico. No se crean camaras adicionales ni RenderTextures compartidas.

Corregida la perdida de foco: Refresh desactivaba y reactivaba el formulario y el identificador en cada fotograma. Ahora calcula su visibilidad y solo cambia el estado cuando corresponde; lo mismo aplica a los botones. Los campos tienen targetGraphic, cursor de escritura y bloqueo de movimiento al escribir. No se borran los borradores al cambiar entre consulta e interaccion.

TAB abre la tablet con cursor; F1 cambia entre interaccion y consulta para caminar. ESC primero termina la escritura, luego sale de interaccion y finalmente cierra la tablet. En modo interaccion la tablet se acerca y se centra suavemente. El formulario separa diagnostico/evidencias, destaca la pestana activa y utiliza el ancho disponible para lectura cuando no hay opciones laterales.

Verificado: compilacion C# con bibliotecas instaladas para Editor y jugador. Pendiente en Unity: correspondencia visual del Canvas con el cristal, clic y escritura prolongada en ID/comentario/codigo, seleccion y borrado de texto, scroll, abrir/cerrar/pasar al PC y volver, pausa y reconexion. Probar ambos roles con ejecutables nuevos; las 51 pruebas anteriores validan logica, no eventos de teclado ni raycasts 3D.