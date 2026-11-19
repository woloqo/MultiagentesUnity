# Movimiento demo del tractor

Este servidor devuelve coordenadas en JSON para Unity. El tractor 1 recorre una ruta corta en bucle; los tractores 2 y 3 permanecen en su posición.

## Iniciar

Desde PowerShell, en la carpeta raíz del proyecto, instala las dependencias (solo la primera vez):

```powershell
py -m pip install -r ServidorPython/requirements.txt
```

Inicia el servidor en esa misma terminal:

```powershell
py -m uvicorn ServidorPython.api_unity:app --host 127.0.0.1 --port 8000
```

Deja la terminal abierta. Puedes comprobar que Uvicorn inició visitando `http://127.0.0.1:8000/docs`; después abre `Assets/Scenes/SampleScene_ObstaculosDemo.unity` en Unity y pulsa **Play**. Unity consulta `/estado` una vez por segundo y anima cada paso con una curva Bézier. No uses **Try it out** en `/estado` durante la comprobación, ya que cada consulta avanza la ruta.

La ruta demo usa coordenadas `(x, z)` donde `x` es la columna y `z` la fila. Sus posiciones permanecen dentro del grid 15×15 y evitan las celdas del archivo `Assets/obstaculos_10porciento_semilla0.txt`.

Si el modelo gira hacia un lado incorrecto, selecciona el objeto `ConexionPython` y ajusta **Ajuste Rotacion Y** en el componente `ComunicacionPython`. El valor inicial es 0° y supone que el frente del tractor apunta al eje local +Z.

Para reiniciar el recorrido, detén el servidor con **Ctrl+C** y vuelve a iniciarlo. La ruta vuelve a su primera celda cuando se reinicia el proceso.
