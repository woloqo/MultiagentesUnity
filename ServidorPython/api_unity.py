from fastapi import FastAPI

app = FastAPI()

# Ruta cuadrada corta y libre dentro del sembradío: (x=columna, z=fila).
# Cada consulta de Unity avanza un punto; las celdas están separadas por un paso.
RUTA_DEMO_TRACTOR_1 = [
    (3, 2),
    (3, 3),
    (4, 3),
    (4, 2),
]
indice_ruta_tractor_1 = 0

@app.get("/estado")
def obtener_estado():
    global indice_ruta_tractor_1

    x_tractor_1, z_tractor_1 = RUTA_DEMO_TRACTOR_1[indice_ruta_tractor_1]
    indice_ruta_tractor_1 = (indice_ruta_tractor_1 + 1) % len(RUTA_DEMO_TRACTOR_1)

    return {
        "tractores": [
            {"id": 1, "x": x_tractor_1, "z": z_tractor_1},
            {"id": 2, "x": 7, "z": 2},
            {"id": 3, "x": 12, "z": 2}
        ]
    }
