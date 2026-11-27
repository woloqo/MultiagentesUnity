# Visualización de la simulación Python

Abre `Assets/Scenes/SampleScene.unity` en Unity. El objeto `ConexionPython` contiene `FarmSimulationBridge`, que consulta el estado del servidor y solicita un tick por paso. La simulación, sus reglas, A* y Blackboard se ejecutan en Python.

Inicia el servidor desde el repositorio de tractores siguiendo `UNITY_INTEGRATION.md`, y pulsa Play. El panel izquierdo muestra el avance y los recursos; los botones de la derecha eligen la vista orbital, la panorámica o el seguimiento de cada tractor. La siembra, los obstáculos y las estaciones siguen el estado recibido del modelo.

Para activar Q-learning, inicia el servidor Python con la política de demostración incluida en su repositorio, como se indica en `UNITY_INTEGRATION.md`. El panel de Unity indica si Q-learning está activado y muestra A* como método de navegación. Sin la opción de política, se usa la estrategia improved original.

La escena utiliza paquetes gratuitos de terceros que cada colaborador debe importar por separado. Consulta `ASSET_STORE_DEPENDENCIES.md` para instalar los modelos de árboles, animales y cielo y conservar sus GUID. Los obstáculos adicionales se generan en tiempo de ejecución con geometría propia.
