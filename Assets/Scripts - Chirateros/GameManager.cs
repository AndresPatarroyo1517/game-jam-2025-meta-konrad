using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class GameManager : MonoBehaviour
{
    [Header("Configuración de Peces")]
    public List<GameObject> peces;
    public Color colorSecuencia = Color.blue;
    public float tiempoMostrarPez = 1f;
    public float tiempoEntrePeces = 0.5f;

    [Header("Configuración de Vasija")]
    public List<GameObject> pedazosVasija;
    public GameObject vasijaCompleta;

    private List<GameObject> secuenciaPeces = new List<GameObject>();
    private int indiceSecuencia = 0;
    private bool jugadorPuedeSeleccionar = false;
    private bool juegoIniciado = false;

    // MaterialPropertyBlock para modificar el color sin afectar la textura
    private MaterialPropertyBlock propertyBlock;
    private int colorPropertyId;

    void Start()
    {
        propertyBlock = new MaterialPropertyBlock();
        colorPropertyId = Shader.PropertyToID("_Color");
    }

    public void IniciarJuego()
    {
        if (!juegoIniciado)
        {
            juegoIniciado = true;
            GenerarSecuencia();
            StartCoroutine(MostrarSecuencia());
        }
    }

    void GenerarSecuencia()
    {
        secuenciaPeces.Clear();
        for (int i = 0; i < 3; i++)
        {
            int indiceAleatorio = Random.Range(0, peces.Count);
            secuenciaPeces.Add(peces[indiceAleatorio]);
        }
    }

    IEnumerator MostrarSecuencia()
    {
        jugadorPuedeSeleccionar = false;
        foreach (GameObject pez in secuenciaPeces)
        {
            CambiarColorPez(pez, colorSecuencia);
            yield return new WaitForSeconds(tiempoMostrarPez);
            RestaurarColorOriginal(pez);
            yield return new WaitForSeconds(tiempoEntrePeces);
        }
        jugadorPuedeSeleccionar = true;
    }

    public void SeleccionarPez(GameObject pezSeleccionado)
    {
        if (!jugadorPuedeSeleccionar || !juegoIniciado) return;

        if (pezSeleccionado == secuenciaPeces[indiceSecuencia])
        {
            CambiarColorPez(pezSeleccionado, Color.green);
            indiceSecuencia++;
            if (indiceSecuencia == secuenciaPeces.Count)
            {
                StartCoroutine(MostrarVasijaCompleta());
            }
        }
        else
        {
            CambiarColorPez(pezSeleccionado, Color.red);
            StartCoroutine(ReiniciarJuego());
        }
    }

    IEnumerator MostrarVasijaCompleta()
    {
        jugadorPuedeSeleccionar = false;
        foreach (GameObject pedazo in pedazosVasija)
        {
            pedazo.SetActive(true);
        }
        if (vasijaCompleta != null)
        {
            vasijaCompleta.SetActive(true);
        }
        yield return new WaitForSeconds(3f);
        StartCoroutine(ReiniciarJuego());
    }

    IEnumerator ReiniciarJuego()
    {
        yield return new WaitForSeconds(2f);
        foreach (GameObject pez in peces)
        {
            RestaurarColorOriginal(pez);
        }
        foreach (GameObject pedazo in pedazosVasija)
        {
            pedazo.SetActive(false);
        }
        if (vasijaCompleta != null)
        {
            vasijaCompleta.SetActive(false);
        }
        indiceSecuencia = 0;
        juegoIniciado = false;
        FindObjectOfType<Carpet>().GetComponent<Collider>().enabled = true;
    }

    // Método para cambiar el color de un pez usando MaterialPropertyBlock
    void CambiarColorPez(GameObject pez, Color nuevoColor)
    {
        Renderer renderer = pez.GetComponent<Renderer>();
        renderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(colorPropertyId, nuevoColor);
        renderer.SetPropertyBlock(propertyBlock);
    }

    // Método para restaurar el color original del pez
    void RestaurarColorOriginal(GameObject pez)
    {
        Renderer renderer = pez.GetComponent<Renderer>();
        renderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(colorPropertyId, Color.white); // Color original (puedes ajustarlo si es necesario)
        renderer.SetPropertyBlock(propertyBlock);
    }
}
