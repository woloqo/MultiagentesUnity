# Dependencias de Asset Store para la escena de obstáculos

## Escena principal de la granja

`Assets/Scenes/SampleScene.unity` también referencia assets gratuitos de terceros. Sus archivos originales no se publican en este repositorio público. Cada integrante debe añadir los paquetes a su cuenta de Unity y realizar una **importación selectiva**, conservando los nombres, carpetas y archivos `.meta` que entrega el paquete. Los GUID de esos metadatos permiten que la escena encuentre sus referencias.

1. En **Window > Package Manager > My Assets**, descarga [Farm Life Lite: Free Animated Low Poly Farm Animals](https://assetstore.unity.com/packages/3d/characters/animals/farm-life-lite-free-animated-low-poly-farm-animals-chicken-goat-418282). Al importar, selecciona las carpetas `Chicken` y `Goat` completas, y en `Scripts` los archivos `AnimalCoat.cs` y `Backrock.FarmLife.asmdef`, junto con sus metadatos. Omite las escenas de demostración y el script de showcase. Los controladores y las texturas de esas carpetas son dependencias de los prefabs.
2. Descarga [Fantasy Skybox FREE](https://assetstore.unity.com/packages/2d/textures-materials/sky/fantasy-skybox-free-18353). En el importador pulsa **None** y selecciona `Panoramics/FS002/FS002_Day.mat` y `Panoramics/FS002/FS002_Day.png`, con los metadatos que Unity incluya.
3. Descarga [Low poly Trees and other nature](https://assetstore.unity.com/packages/3d/vegetation/trees/low-poly-trees-and-other-nature-175336). La escena principal usa `Materials/LowpolyMaterial.mat`, `Materials/nature.png`, y los modelos y prefabs `Birch2_summer`, `Bush1_summer`, `Bush2_summer`, `Tree1_summer`, `Tree3_summer` y `Tree5_summer`. Incluye sus archivos `.meta` y cualquier dependencia que indique el importador.
4. Abre `Assets/Scenes/SampleScene.unity` y comprueba que los animales, árboles y el cielo aparezcan correctamente. Si una referencia falta, revisa los GUID y las dependencias desde el importador; no regeneres manualmente los archivos `.meta`.

La descarga del paquete puede incluir el paquete completo en la caché de Unity; la selección anterior reduce lo que se **importa al proyecto**, no necesariamente lo que Unity descarga. Los materiales, mallas y scripts propios del entorno se encuentran en `Assets/Environment`.

Las condiciones de uso de cada paquete siguen siendo aplicables. Consulta la [EULA de Asset Store](https://unity.com/legal/as-terms) y su [FAQ de licencias](https://assetstore.unity.com/browse/eula-faq).

La escena `Assets/Scenes/SampleScene_ObstaculosDemo.unity` usa referencias a prefabs de estos paquetes:

- [3D Low-Poly Rocks](https://assetstore.unity.com/packages/3d/vegetation/3d-low-poly-rocks-400552)
- [Low poly Trees and other nature](https://assetstore.unity.com/packages/3d/vegetation/trees/low-poly-trees-and-other-nature-175336)

## Preparar el proyecto en otra computadora

1. Inicia sesión en Unity con una cuenta que tenga acceso/licencia a ambos paquetes.
2. En Unity, abre **Window > Package Manager > My Assets**.
3. Busca cada paquete, pulsa **Download** y luego **Import**.
4. Conserva la estructura de carpetas y los archivos `.meta` que entrega Unity. La escena depende de los GUID de esos assets.
5. Abre `Assets/Scenes/SampleScene_ObstaculosDemo.unity`.

Los archivos originales de esos paquetes se excluyen del repositorio público. Unity limita la redistribución de assets de la Asset Store; consulta la [EULA](https://unity.com/legal/as-terms) y su [FAQ de licencias](https://assetstore.unity.com/browse/eula-faq). El repositorio conserva la escena y sus referencias, pero cada colaborador debe importar los paquetes desde su propia cuenta.
