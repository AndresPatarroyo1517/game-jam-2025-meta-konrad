using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class WaterFlow : MonoBehaviour
{
    [Header("Velocidad del flujo (ejes UV)")]
    public Vector2 flowSpeed = new Vector2(0.05f, 0.02f);

    [Header("Nombre de la textura en el shader")]
    public string textureName = "_MainTex";

    private Renderer rend;
    private Vector2 offset;

    void Start()
    {
        rend = GetComponent<Renderer>();
    }

    void Update()
    {
        // Calcula desplazamiento UV en función del tiempo
        offset += flowSpeed * Time.deltaTime;

        // Actualiza el desplazamiento de la textura
        rend.material.SetTextureOffset(textureName, offset);
    }
}