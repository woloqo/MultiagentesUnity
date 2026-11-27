using UnityEngine;

// Decorative meshes are confined to their Python grid cell; they never affect navigation.
public static class FarmObstacleVisuals
{
    private static Material Material(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var material = new Material(shader) { color = color };
        return material;
    }

    private static GameObject Shape(Transform parent, PrimitiveType type, Vector3 position, Vector3 scale, Color color)
    {
        var item = GameObject.CreatePrimitive(type);
        item.transform.SetParent(parent, false);
        item.transform.localPosition = position;
        item.transform.localScale = scale;
        Object.Destroy(item.GetComponent<Collider>());
        item.GetComponent<Renderer>().material = Material(color);
        return item;
    }

    public static void CreateObstacle(int row, int col, GameObject rockPrefab, Transform parent)
    {
        var root = new GameObject($"ObstacleVisual_{row}_{col}");
        root.transform.SetParent(parent, false);
        root.transform.position = new Vector3(col, 0.05f, row);
        int hash = (row * 73856093 ^ col * 19349663) & int.MaxValue;
        root.transform.rotation = Quaternion.Euler(0, (hash % 4) * 90, 0);
        switch (hash % 4)
        {
            case 0:
                var rock = Shape(root.transform, PrimitiveType.Sphere, new Vector3(0, .24f, 0), new Vector3(.62f, .46f, .53f), new Color(.46f, .46f, .42f));
                rock.transform.localRotation = Quaternion.Euler(10, 0, 12);
                break;
            case 1:
                // A ring of dark low-poly tire segments with an open center.
                for (int i = 0; i < 12; i++)
                {
                    float a = i * Mathf.PI * 2 / 12;
                    var tread = Shape(root.transform, PrimitiveType.Cube, new Vector3(Mathf.Cos(a) * .23f, .11f, Mathf.Sin(a) * .23f), new Vector3(.15f, .20f, .15f), new Color(.10f, .11f, .10f));
                    tread.transform.localRotation = Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0);
                }
                break;
            case 2:
                var log = Shape(root.transform, PrimitiveType.Cylinder, new Vector3(0, .16f, 0), new Vector3(.28f, .32f, .28f), new Color(.32f, .19f, .10f));
                log.transform.localRotation = Quaternion.Euler(0, 0, 90);
                var end = Shape(root.transform, PrimitiveType.Cylinder, new Vector3(.326f, .16f, 0), new Vector3(.23f, .006f, .23f), new Color(.65f, .46f, .27f));
                end.transform.localRotation = Quaternion.Euler(0, 0, 90);
                break;
            default:
                var sheet = Shape(root.transform, PrimitiveType.Cube, new Vector3(0, .07f, 0), new Vector3(.68f, .06f, .48f), new Color(.42f, .20f, .12f));
                sheet.transform.localRotation = Quaternion.Euler(0, 15, 5);
                for (int i = -2; i <= 2; i++)
                    Shape(sheet.transform, PrimitiveType.Cube, new Vector3(0, .55f, i * .16f), new Vector3(.95f, .25f, .035f), new Color(.55f, .30f, .16f));
                break;
        }
    }

    public static void CreateStation(string kind, int row, int col)
    {
        var root = new GameObject("StationVisual_" + kind);
        root.transform.position = new Vector3(col, .05f, row);
        if (kind == "gas")
        {
            Shape(root.transform, PrimitiveType.Cube, new Vector3(0, .32f, 0), new Vector3(.42f, .64f, .32f), new Color(.72f, .12f, .08f));
            Shape(root.transform, PrimitiveType.Cube, new Vector3(0, .43f, -.17f), new Vector3(.30f, .20f, .02f), new Color(.88f, .88f, .75f));
            Shape(root.transform, PrimitiveType.Cube, new Vector3(0, .68f, 0), new Vector3(.48f, .08f, .36f), new Color(.25f, .25f, .23f));
        }
        else
        {
            Shape(root.transform, PrimitiveType.Cube, new Vector3(0, .18f, 0), new Vector3(.62f, .36f, .48f), new Color(.57f, .36f, .16f));
            Shape(root.transform, PrimitiveType.Cube, new Vector3(0, .38f, 0), new Vector3(.66f, .07f, .52f), new Color(.77f, .57f, .29f));
            Shape(root.transform, PrimitiveType.Sphere, new Vector3(-.13f, .48f, 0), new Vector3(.22f, .22f, .18f), new Color(.84f, .75f, .48f));
            Shape(root.transform, PrimitiveType.Sphere, new Vector3(.13f, .48f, 0), new Vector3(.22f, .22f, .18f), new Color(.84f, .75f, .48f));
        }
    }
}
