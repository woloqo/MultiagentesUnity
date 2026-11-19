# Dependencias de Asset Store para la escena de obstáculos

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
