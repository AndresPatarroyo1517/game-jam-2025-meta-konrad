using UnityEngine;
using System.Collections.Generic;

public class ZonaActivacionUI : MonoBehaviour
{
    [Header("Configuración de la Zona")]
    [Tooltip("Radio del área circular de activación")]
    [Range(0.5f, 10f)]
    public float radioZona = 3f;
    
    [Tooltip("Altura de la zona (cilindro)")]
    [Range(0.5f, 5f)]
    public float alturaZona = 2.5f;
    
    [Tooltip("Manager del espiral asociado a esta zona")]
    public EspiralManager espiralManager;

    [Header("Referencias del jugador")]
    [Tooltip("Transform de la cámara XR (jugador)")]
    public Transform camaraXR;
    
    [Tooltip("Buscar automáticamente la cámara al inicio")]
    public bool buscarCamaraAutomaticamente = true;

    [Header("Visualización en Editor")]
    [Tooltip("Mostrar el área de activación en Scene view")]
    public bool mostrarGizmos = true;
    
    [Tooltip("Color del gizmo cuando está inactivo")]
    public Color colorGizmoInactivo = new Color(1f, 1f, 0f, 0.3f);
    
    [Tooltip("Color del gizmo cuando está activo")]
    public Color colorGizmoActivo = new Color(0f, 1f, 0f, 0.5f);

    [Header("Transiciones suaves")]
    [Tooltip("Usar fade in/out para la UI")]
    public bool usarFadeTransicion = true;
    
    [Range(0.1f, 2f)]
    [Tooltip("Velocidad del fade")]
    public float velocidadFade = 0.5f;

    [Header("Debug")]
    public bool mostrarDebug = false;

    private bool jugadorEnZona = false;
    private bool uiActiva = false;
    private CanvasGroup canvasGroup;
    private Coroutine fadeCoroutine;

    void Start()
    {
        // Buscar cámara XR automáticamente
        if (buscarCamaraAutomaticamente && camaraXR == null)
        {
            GameObject cameraRig = GameObject.FindGameObjectWithTag("MainCamera");
            if (cameraRig != null)
            {
                camaraXR = cameraRig.transform;
            }
            else
            {
                // Intentar encontrar por nombre común
                cameraRig = GameObject.Find("Main Camera") ?? GameObject.Find("XR Camera");
                if (cameraRig != null)
                {
                    camaraXR = cameraRig.transform;
                }
            }
        }

        // Validar configuración
        if (camaraXR == null)
        {
            Debug.LogError($"[ZonaActivacionUI] No se encontró la cámara XR en {gameObject.name}. Asigna 'camaraXR' manualmente.");
        }

        if (espiralManager == null)
        {
            Debug.LogWarning($"[ZonaActivacionUI] No hay EspiralManager asignado en {gameObject.name}");
        }

        // Configurar CanvasGroup para fade si está disponible
        if (usarFadeTransicion && espiralManager != null && espiralManager.canvasProgreso != null)
        {
            canvasGroup = espiralManager.canvasProgreso.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = espiralManager.canvasProgreso.gameObject.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = 0f;
        }
    }

    void Update()
    {
        if (camaraXR == null || espiralManager == null) return;

        // Verificar si el jugador está en la zona
        bool dentroDeZona = EstaEnZona(camaraXR.position);

        // Si cambia el estado
        if (dentroDeZona != jugadorEnZona)
        {
            jugadorEnZona = dentroDeZona;
            
            if (mostrarDebug)
            {
                Debug.Log($"[ZonaActivacionUI] Jugador {(dentroDeZona ? "entró" : "salió")} de la zona '{gameObject.name}'");
            }

            // Activar/desactivar UI
            if (dentroDeZona)
            {
                ActivarUI();
            }
            else
            {
                DesactivarUI();
            }
        }
    }

    bool EstaEnZona(Vector3 posicionJugador)
    {
        // Calcular distancia horizontal (ignorando Y)
        Vector3 posicionZona = transform.position;
        Vector2 posJugadorXZ = new Vector2(posicionJugador.x, posicionJugador.z);
        Vector2 posZonaXZ = new Vector2(posicionZona.x, posicionZona.z);
        
        float distanciaHorizontal = Vector2.Distance(posJugadorXZ, posZonaXZ);
        
        // Verificar si está dentro del radio
        if (distanciaHorizontal > radioZona)
        {
            return false;
        }

        // Verificar altura (cilindro)
        float diferenciaAltura = Mathf.Abs(posicionJugador.y - posicionZona.y);
        if (diferenciaAltura > alturaZona / 2f)
        {
            return false;
        }

        return true;
    }

    void ActivarUI()
    {
        if (uiActiva) return;
        
        uiActiva = true;
        espiralManager.MostrarUI(true);

        // Fade in
        if (usarFadeTransicion && canvasGroup != null)
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            fadeCoroutine = StartCoroutine(FadeCanvasGroup(canvasGroup, 1f));
        }
    }

    void DesactivarUI()
    {
        if (!uiActiva) return;
        
        uiActiva = false;

        // Fade out
        if (usarFadeTransicion && canvasGroup != null)
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            fadeCoroutine = StartCoroutine(FadeCanvasGroup(canvasGroup, 0f, () => 
            {
                espiralManager.MostrarUI(false);
            }));
        }
        else
        {
            espiralManager.MostrarUI(false);
        }
    }

    System.Collections.IEnumerator FadeCanvasGroup(CanvasGroup cg, float targetAlpha, System.Action onComplete = null)
    {
        float startAlpha = cg.alpha;
        float tiempo = 0f;

        while (tiempo < velocidadFade)
        {
            tiempo += Time.deltaTime;
            float progreso = tiempo / velocidadFade;
            cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, progreso);
            yield return null;
        }

        cg.alpha = targetAlpha;
        onComplete?.Invoke();
    }

    // Visualización en el editor
    void OnDrawGizmos()
    {
        if (!mostrarGizmos) return;

        Color gizmoColor = jugadorEnZona ? colorGizmoActivo : colorGizmoInactivo;
        Gizmos.color = gizmoColor;

        // Dibujar cilindro (círculo + líneas verticales)
        Vector3 centro = transform.position;
        
        // Círculo superior
        DrawCircle(centro + Vector3.up * (alturaZona / 2f), radioZona);
        
        // Círculo inferior
        DrawCircle(centro - Vector3.up * (alturaZona / 2f), radioZona);
        
        // Líneas verticales
        for (int i = 0; i < 8; i++)
        {
            float angulo = i * 45f * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angulo), 0f, Mathf.Sin(angulo)) * radioZona;
            Vector3 puntoSuperior = centro + offset + Vector3.up * (alturaZona / 2f);
            Vector3 puntoInferior = centro + offset - Vector3.up * (alturaZona / 2f);
            Gizmos.DrawLine(puntoSuperior, puntoInferior);
        }

        // Dibujar etiqueta
        if (espiralManager != null)
        {
            UnityEditor.Handles.Label(centro + Vector3.up * (alturaZona / 2f + 0.5f), 
                $"Zona: {gameObject.name}\nRadio: {radioZona}m");
        }
    }

    void DrawCircle(Vector3 centro, float radio, int segmentos = 32)
    {
        Vector3 puntoAnterior = centro + new Vector3(radio, 0f, 0f);
        
        for (int i = 1; i <= segmentos; i++)
        {
            float angulo = (i / (float)segmentos) * 360f * Mathf.Deg2Rad;
            Vector3 puntoActual = centro + new Vector3(Mathf.Cos(angulo) * radio, 0f, Mathf.Sin(angulo) * radio);
            Gizmos.DrawLine(puntoAnterior, puntoActual);
            puntoAnterior = puntoActual;
        }
    }

    void OnValidate()
    {
        // Asegurar valores mínimos
        radioZona = Mathf.Max(0.5f, radioZona);
        alturaZona = Mathf.Max(0.5f, alturaZona);
    }
}