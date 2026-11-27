# Interfaz de la simulación de tractores

## Ejecutar y usar

1. Inicia el servidor Python del repositorio de simulación, que incluye `POST /reset`:

   ```powershell
   cd ..\Reto_Multiagentes
   .\.venv\Scripts\python.exe -m src.unity_server --architecture improved --density 0.10 --seed 0 --q-policies config/qlearning_demo_policies.json
   ```

   El argumento `--q-policies` carga las tablas ya entrenadas. Si no se carga una política, desactiva Q-learning en la pantalla antes de iniciar.
2. Abre `Assets/Scenes/SampleScene.unity` y pulsa Play.
3. En la pantalla inicial elige densidad (0 %, 5 %, 10 %), semilla, inicios fijos o aleatorios y si se usa Q-learning. La arquitectura de esta pantalla es `improved`, que conserva Blackboard y A*.
4. **Iniciar episodio con esta selección** envía la configuración a Python mediante `POST /reset`. Python genera el escenario, responde con su snapshot real de tick 0 y permanece como autoridad de las decisiones. Unity solicita los ticks mediante `POST /step`.
5. **Conectar con el episodio activo de Python** solo consulta `GET /state`; no reinicia el modelo. Si el estado recibido ya tiene `finished: true`, el panel muestra que terminó.
6. En el panel del episodio puedes ajustar la velocidad visual, pausar o reanudar y avanzar un tick manualmente. **Reiniciar** vuelve a crear el episodio actual usando sus parámetros (densidad, semilla, modo, arquitectura y Q-learning). También funciona si hay una solicitud HTTP activa: espera esa respuesta y luego reinicia.
7. El panel de cámara conserva las vistas orbital, panorámica y de seguimiento T1, T2 y T3. Día y noche siguen deshabilitados; la iluminación no está implementada.

## Datos mostrados

El HUD lee el snapshot de Python: tick, cobertura, densidad, semilla, arquitectura, estado Q-learning, tamaño del campo, obstáculos, celdas sembrables y, por tractor, modo, posición, dirección, semillas y combustible. También muestra colisiones y acciones rechazadas cuando `metrics` está presente. Las posiciones iniciales reales son las filas/columnas de cada tractor en el snapshot del tick 0.

## Contrato HTTP

- `GET /health`: comprueba que el servidor responde.
- `GET /state`: devuelve el snapshot vigente sin avanzar.
- `POST /step`: avanza un tick si el episodio sigue activo y devuelve el snapshot.
- `POST /reset`: reemplaza el episodio con los parámetros recibidos y devuelve el snapshot real de tick 0.

Unity envía un objeto JSON de esta forma:

```json
{
  "density": 0.05,
  "seed": 37,
  "start_mode": "random",
  "q_learning": true,
  "architecture": "improved"
}
```

La semilla hace reproducibles el mapa y los inicios aleatorios. `start_mode` acepta `fixed` o `random`; las posiciones efectivas se devuelven en `tractors`. Si el servicio activo no tiene la ruta `/reset`, Unity mostrará el error HTTP de Python y no creará el episodio. El servidor del checkout local antiguo que estaba atendiendo el puerto 8000 no tenía esa ruta; para esta integración debe ejecutarse la copia con reset del comando anterior.

La reproducción visual la marca Unity, pero la simulación avanza en Python solo cuando recibe `POST /step`. Las coordenadas son `row`/`col` en Python y se convierten en Unity a `x = col`, `z = row`.

## Cambios y verificación

- Se conectó `FarmSimulationUI` al método `FarmSimulationBridge.StartConfiguredEpisode`; dejó de mostrar “pendiente de API”.
- Se agregó el control de semilla y se conectó el toggle de Q-learning. El botón **Reiniciar** envía el escenario actual a `/reset`.
- Si el episodio activo ya terminó, **Conectar** mostrará su estado final. Usa **Iniciar** o **Reiniciar** para crear un episodio nuevo mediante POST `/reset`.
- El endpoint se verificó en un puerto temporal: reset aleatorio devolvió tick 0, `GET /state` mantuvo tick 0 y `POST /step` avanzó a tick 1. Desde el Editor se verificó el reset con densidad 5 %, semilla 37, posiciones aleatorias y Q-learning activo: tick 0, 11 obstáculos y tres tractores en posiciones distintas.
- Los scripts compilaron sin errores y la consola no registró errores nuevos. No se cambió la escena en esta corrección.
