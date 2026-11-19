"""Genera una vista superior de una montaña como heightmap en escala de grises.

Requisito: numpy.
Ejecución en VSCode: python heightmap_montana.py
El PNG de 1024 x 1024 se guarda junto a este script.
"""
from pathlib import Path
import struct
import zlib

import numpy as np

SIZE = 1024
OUTPUT = Path(__file__).with_name("heightmap_montana.png")


def smooth_random_field(size, grid_size=65):
    """Ruido aleatorio de baja frecuencia para dar variación natural."""
    coarse = np.random.random((grid_size, grid_size)) - 0.5
    sample_points = np.linspace(0, grid_size - 1, size)

    across = np.array([
        np.interp(sample_points, np.arange(grid_size), row)
        for row in coarse
    ])
    down = np.array([
        np.interp(sample_points, np.arange(grid_size), across[:, column])
        for column in range(size)
    ]).T
    return down


def save_grayscale_png(pixels, path):
    """Guarda una matriz uint8 como PNG gris usando solo la biblioteca estándar."""
    height, width = pixels.shape

    def chunk(tag, data):
        checksum = zlib.crc32(tag + data) & 0xFFFFFFFF
        return struct.pack(">I", len(data)) + tag + data + struct.pack(">I", checksum)

    # Cada fila empieza con filtro PNG 0 (sin filtro).
    scanlines = b"".join(b"\x00" + pixels[row].tobytes() for row in range(height))
    header = struct.pack(">2I5B", width, height, 8, 0, 0, 0, 0)
    png_data = (
        b"\x89PNG\r\n\x1a\n"
        + chunk(b"IHDR", header)
        + chunk(b"IDAT", zlib.compress(scanlines, level=9))
        + chunk(b"IEND", b"")
    )
    path.write_bytes(png_data)


def main():
    # Malla cuadrada vista desde arriba; el centro representa la cima.
    axis = np.linspace(-1.0, 1.0, SIZE)
    x, y = np.meshgrid(axis, axis)

    # Un perfil cónico crea una sola montaña: cima blanca y base oscura.
    # La ligera elipse y el centro desplazado evitan una forma perfectamente geométrica.
    center_x, center_y = 0.035, -0.025
    radius = np.sqrt(((x - center_x) / 0.96) ** 2 + ((y - center_y) / 0.91) ** 2)
    slope = np.clip(1.0 - radius, 0.0, 1.0)
    height = slope ** 1.3

    # Ruido suave en varias escalas da pequeñas irregularidades a las laderas.
    np.random.seed(24)
    broad_noise = smooth_random_field(SIZE, grid_size=64)
    fine_noise = smooth_random_field(SIZE, grid_size=180)
    texture = 0.7 * broad_noise + 0.3 * fine_noise
    slope_detail = np.sqrt(np.clip(height * (1.0 - height), 0.0, 1.0))
    height += 0.22 * texture * slope_detail

    # La altura se guarda directamente como gris: negro = bajo, blanco = alto.
    height = np.maximum(height, 0.0)
    height -= height.min()
    if height.max() > 0:
        height /= height.max()

    pixels = np.round(height * 255).astype(np.uint8)
    save_grayscale_png(pixels, OUTPUT)
    print(f"Imagen guardada en: {OUTPUT}")


if __name__ == "__main__":
    main()
