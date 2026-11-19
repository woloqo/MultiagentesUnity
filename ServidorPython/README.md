# Movimiento demo del tractor

Este servidor devuelve coordenadas en JSON para Unity. El tractor 1 recorre una ruta corta en bucle; los tractores 2 y 3 permanecen en su posición.

## Iniciar

Desde PowerShell, en la carpeta raíz del proyecto:

```powershell
py -m pip install fastapi uvicorn
py -m uvicorn ServidorPython.api_unity:app --reload
```

Después abre `Assets/Scenes/SampleScene_ObstaculosDemo.unity` en Unity y pulsa **Play**. Unity consulta `/estado` una vez por segundo y anima cada paso con una curva Bézier.

La ruta demo usa coordenadas `(x, z)` donde `x` es la columna y `z` la fila. Sus posiciones permanecen dentro del grid 15×15 y evitan las celdas del archivo `Assets/obstaculos_10porciento_semilla0.txt`.

Si el modelo gira hacia un lado incorrecto, selecciona el objeto `ConexionPython` y ajusta **Ajuste Rotacion Y** en el componente `ComunicacionPython`. El valor inicial es 0° y supone que el frente del tractor apunta al eje local +Z.
