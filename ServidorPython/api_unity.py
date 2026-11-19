from fastapi import FastAPI

app = FastAPI()

@app.get("/estado")
def obtener_estado():
    return {
        "tractores": [
            {"id": 1, "x": 3, "z": 2},
            {"id": 2, "x": 7, "z": 2},
            {"id": 3, "x": 12, "z": 2}
        ]
    }
