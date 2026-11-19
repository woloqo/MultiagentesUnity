using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;

public static class GeneradorMontanasHeightmap
{
    private const string RutaHeightmap = "Assets/Heightmaps/heightmap_montana.png";
    private const string RutaMaterialPradera = "Assets/Materials/MaterialFaldaMontana.mat";
    private const string RutaMaterialRoca = "Assets/Materials/MaterialPicoMontana.mat";
    private const string NombreGrupo = "Montanas_Heightmap";
    private const int Resolucion = 65;
    private const float Profundidad = 11.5f;
    private const float InicioZ = 15.5f;

    [MenuItem("Tools/Reto Unity/Generar montañas desde heightmap")]
    private static void Generar()
    {
        Texture2D heightmap = PrepararHeightmap();
        if (heightmap == null)
            return;

        Material materialPradera = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialPradera);
        Material materialRoca = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialRoca);
        if (materialPradera == null || materialRoca == null)
        {
            EditorUtility.DisplayDialog(
                "Faltan materiales",
                "No se encontraron MaterialFaldaMontana o MaterialPicoMontana en Assets/Materials.",
                "Aceptar");
            return;
        }

        if (GameObject.Find(NombreGrupo) != null)
        {
            EditorUtility.DisplayDialog(
                "Montañas ya generadas",
                "La escena ya contiene Montanas_Heightmap. Elimina o renombra ese grupo antes de generar otra copia.",
                "Aceptar");
            return;
        }

        GameObject grupo = new GameObject(NombreGrupo);
        Undo.RegisterCreatedObjectUndo(grupo, "Generar montañas desde heightmap");

        CrearCumbre(grupo.transform, heightmap, materialPradera, materialRoca,
            "Montana_1", new Vector3(-3f, 0f, InicioZ), 7f, Profundidad, 4.8f);
        CrearCumbre(grupo.transform, heightmap, materialPradera, materialRoca,
            "Montana_2", new Vector3(4f, 0f, InicioZ), 7f, Profundidad, 6.6f);
        CrearCumbre(grupo.transform, heightmap, materialPradera, materialRoca,
            "Montana_3", new Vector3(11f, 0f, InicioZ), 7f, Profundidad, 5.2f);

        EditorSceneManager.MarkSceneDirty(grupo.scene);
        Selection.activeGameObject = grupo;
        EditorGUIUtility.PingObject(grupo);
        Debug.Log("Se generó una cadena de tres montañas ProBuilder desde heightmap_montana.png.", grupo);
    }

    private static Texture2D PrepararHeightmap()
    {
        TextureImporter importer = AssetImporter.GetAtPath(RutaHeightmap) as TextureImporter;
        if (importer == null)
        {
            EditorUtility.DisplayDialog(
                "No se encontró el heightmap",
                "Verifica que exista Assets/Heightmaps/heightmap_montana.png.",
                "Aceptar");
            return null;
        }

        bool requiereImportacion = !importer.isReadable
            || importer.sRGBTexture
            || importer.textureCompression != TextureImporterCompression.Uncompressed
            || importer.wrapMode != TextureWrapMode.Clamp;

        if (requiereImportacion)
        {
            importer.isReadable = true;
            importer.sRGBTexture = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }

        Texture2D heightmap = AssetDatabase.LoadAssetAtPath<Texture2D>(RutaHeightmap);
        if (heightmap == null || !heightmap.isReadable)
        {
            EditorUtility.DisplayDialog(
                "Heightmap no legible",
                "Unity no pudo leer los píxeles del PNG. Revisa Read/Write en sus opciones de importación.",
                "Aceptar");
            return null;
        }

        return heightmap;
    }

    private static void CrearCumbre(
        Transform padre,
        Texture2D heightmap,
        Material materialPradera,
        Material materialRoca,
        string nombre,
        Vector3 posicion,
        float ancho,
        float profundidad,
        float alturaMaxima)
    {
        var vertices = new List<Vector3>(Resolucion * Resolucion);
        var alturas = new float[Resolucion, Resolucion];

        for (int z = 0; z < Resolucion; z++)
        {
            float v = z / (float)(Resolucion - 1);
            for (int x = 0; x < Resolucion; x++)
            {
                float u = x / (float)(Resolucion - 1);
                float alturaNormalizada = heightmap.GetPixelBilinear(u, v).grayscale;
                alturas[z, x] = alturaNormalizada;
                vertices.Add(new Vector3(u * ancho, alturaNormalizada * alturaMaxima, v * profundidad));
            }
        }

        var caras = new List<Face>((Resolucion - 1) * (Resolucion - 1));
        for (int z = 0; z < Resolucion - 1; z++)
        {
            for (int x = 0; x < Resolucion - 1; x++)
            {
                int abajoIzquierda = z * Resolucion + x;
                int abajoDerecha = abajoIzquierda + 1;
                int arribaIzquierda = abajoIzquierda + Resolucion;
                int arribaDerecha = arribaIzquierda + 1;

                // Este orden deja las normales de la superficie orientadas hacia arriba.
                var cara = new Face(new[]
                {
                    abajoIzquierda, arribaDerecha, abajoDerecha,
                    abajoIzquierda, arribaIzquierda, arribaDerecha
                });

                cara.smoothingGroup = 1;
                float alturaMedia = (
                    alturas[z, x]
                    + alturas[z, x + 1]
                    + alturas[z + 1, x]
                    + alturas[z + 1, x + 1]) * 0.25f;
                cara.submeshIndex = alturaMedia >= 0.48f ? 1 : 0;
                caras.Add(cara);
            }
        }

        ProBuilderMesh malla = ProBuilderMesh.Create();
        malla.gameObject.name = nombre;
        Undo.RegisterCreatedObjectUndo(malla.gameObject, "Crear montaña ProBuilder");
        malla.GetComponent<MeshRenderer>().sharedMaterials = new[] { materialPradera, materialRoca };
        malla.RebuildWithPositionsAndFaces(vertices, caras);
        Undo.SetTransformParent(malla.transform, padre, "Agrupar montaña ProBuilder");
        malla.transform.localPosition = posicion;
        malla.ToMesh();
        malla.Refresh();

        MeshCollider collider = Undo.AddComponent<MeshCollider>(malla.gameObject);
        collider.sharedMesh = malla.GetComponent<MeshFilter>().sharedMesh;
    }
}
