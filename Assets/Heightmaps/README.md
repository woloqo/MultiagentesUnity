# Montañas desde heightmap

El archivo `heightmap_montana.png` es un mapa de altura en escala de grises: negro representa la base, blanco representa la altura máxima y los tonos intermedios forman las laderas. El desvanecido hacia negro en los bordes hace que cada montaña se una visualmente al suelo.

## Generar la geometría en Unity

1. Abre el proyecto `Reto_Unity` con Unity 6.6. El paquete ProBuilder ya está en `Packages/manifest.json`.
2. Para generar montañas, abre `Assets/Scenes/SampleScene.unity` o una escena de trabajo que todavía no tenga el grupo `Montanas_Heightmap`. La escena `SampleScene_ObstaculosDemo.unity` ya contiene el resultado y sirve para visualizarlo junto con los obstáculos; no vuelvas a generar allí.
3. Espera a que Unity termine de importar los archivos de `Assets/Heightmaps` y compilar el script de `Assets/Editor`.
4. En la barra superior, elige **Tools > Reto Unity > Generar montañas desde heightmap**.
5. Unity crea el grupo `Montanas_Heightmap` con tres mallas ProBuilder y sus colliders, detrás de la cuadrícula de la granja. La falda usa `MaterialFaldaMontana`; las zonas altas usan `MaterialPicoMontana`.
6. Guarda la escena con **File > Save As…** y elige `Assets/Scenes/SampleScene_Heightmap.unity` para conservar intacta la escena original.

El generador usa 65 vértices por lado en cada montaña. La altura de cada vértice se calcula como `gris × altura máxima`; las tres cumbres tienen alturas máximas distintas. El umbral de 0.48 asigna las caras superiores al material de roca.

## Cambiar la forma

- Para variar la silueta, edita o reemplaza `heightmap_montana.png`. La imagen debe ser cuadrada y en escala de grises; confirma que su borde exterior sea negro para que la malla llegue a altura cero.
- En el Inspector de importación del PNG, deja **Read/Write** habilitado, **sRGB (Color Texture)** deshabilitado y **Compression** en **None**. El generador también aplica esos ajustes al ejecutarse.
- Para ajustar el tamaño y el relieve, cambia `Resolucion`, `Profundidad`, `InicioZ` y los argumentos de `CrearCumbre` en `GeneradorMontanasHeightmap.cs`.
- Para regenerar tras editar el PNG, elimina o renombra primero `Montanas_Heightmap` en la escena y vuelve a ejecutar el menú.

Los colores se controlan en `Assets/Materials/MaterialFaldaMontana.mat` y `Assets/Materials/MaterialPicoMontana.mat` desde **Base Map > Color** en el Inspector.

## Regenerar el PNG (opcional)

`generar_heightmap.py` crea una montaña radial con una cima clara, un desvanecido oscuro y ruido suave. Requiere Python y NumPy. Desde una terminal situada en `Assets/Heightmaps`, ejecuta:

```powershell
python -m pip install numpy
python generar_heightmap.py
```

Unity volverá a importar el PNG cuando se genere.
