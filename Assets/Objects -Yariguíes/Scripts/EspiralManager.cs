using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
//using UnityEngine.XR.Interaction.Toolkit.Interactors;
//using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections;
using System.Collections.Generic;

public class EspiralManager : MonoBehaviour
{
    [Header("Sockets donde van las semillas")]
    [Tooltip("Lista de XR Socket Interactors que deben llenarse")]
    public UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor socket1;
public UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor socket2;
//public UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor socket3;
//public UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor socket4;
//public UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor socket5;
//public UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor socket6;
//public UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor socket7;

private List<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor> sockets = new List<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();

    [Header("Validación de Semillas")]
    [Tooltip("Tag que deben tener los objetos válidos")]
    public string tagSemilla = "Semilla";
    
    [Tooltip("Color del socket cuando está vacío")]
    public Color colorSocketVacio = new Color(1f, 0.5f, 0.5f, 0.3f);
    
    [Tooltip("Color del socket cuando tiene semilla")]
    public Color colorSocketLleno = new Color(0.5f, 1f, 0.5f, 0.5f);

    [Header("Espiral que se activa al completar")]
    [Tooltip("GameObject del espiral de agua que aparecerá")]
    public GameObject espiralFinal;

    [Header("Efectos de Audio")]
    public AudioClip sonidoColocarSemilla;
    public AudioClip sonidoSemillaIncorrecta;
    public AudioClip sonidoCompletado;
    
    [Range(0f, 1f)]
    public float volumenEfectos = 0.7f;

    [Header("Efectos Visuales")]
    public ParticleSystem efectoColocarSemilla;
    public ParticleSystem efectoCompletado;
    public GameObject prefabChispasSemilla;

    [Header("Feedback Háptico")]
    [Range(0f, 1f)]
    [Tooltip("Intensidad del haptic al colocar semilla correcta")]
    public float hapticIntensidadCorrecto = 0.5f;
    
    [Range(0f, 1f)]
    [Tooltip("Intensidad del haptic al colocar semilla incorrecta")]
    public float hapticIntensidadIncorrecto = 0.3f;
    
    [Range(0f, 1f)]
    [Tooltip("Intensidad del haptic al completar puzzle")]
    public float hapticIntensidadCompletado = 0.8f;
    
    [Range(0f, 1f)]
    [Tooltip("Duración del pulso háptico")]
    public float hapticDuracion = 0.1f;

    [Header("Configuración de animación")]
    [Range(0.5f, 5f)]
    [Tooltip("Duración de la aparición del espiral")]
    public float duracionAparicion = 2f;
    
    [Range(0f, 360f)]
    [Tooltip("Velocidad de rotación durante aparición")]
    public float velocidadRotacion = 120f;
    
    [Tooltip("Altura extra al aparecer el espiral")]
    public float alturaAparicion = 0.5f;

    [Header("UI y Feedback")]
    [Tooltip("Canvas UI para mostrar progreso (se activa/desactiva por zona)")]
    public Canvas canvasProgreso;
    
    [Tooltip("Texto UI para mostrar progreso")]
    public TMPro.TextMeshProUGUI textoProgreso;
    
    [Tooltip("Mostrar mensajes en consola")]
    public bool mostrarDebug = true;

    private AudioSource audioSrc;
    private bool completado = false;
    private int socketsLlenos = 0;
    private Dictionary<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor, MeshRenderer> socketRenderers = new Dictionary<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor, MeshRenderer>();

    // Propiedad pública para el sistema de zonas
    public bool UIVisible { get; private set; } = false;

    void Start()
    {
         sockets.Clear();
    sockets.Add(socket1);
    sockets.Add(socket2);
    //sockets.Add(socket3);
    //sockets.Add(socket4);
    //sockets.Add(socket5);
    //sockets.Add(socket6);
    //sockets.Add(socket7);
        InicializarComponentes();
        ConfigurarSockets();
        ValidarConfiguracion();
        
        if (espiralFinal != null)
        {
            espiralFinal.SetActive(false);
        }
        
        // Ocultar UI al inicio
        if (canvasProgreso != null)
        {
            canvasProgreso.gameObject.SetActive(false);
        }
        
        ActualizarTextoUI();
        StartCoroutine(VerificarSockets());
    }

    void InicializarComponentes()
    {
        audioSrc = GetComponent<AudioSource>();
        if (audioSrc == null)
        {
            audioSrc = gameObject.AddComponent<AudioSource>();
        }
        audioSrc.playOnAwake = false;
        audioSrc.spatialBlend = 1f;
        audioSrc.volume = volumenEfectos;
    }

    void ConfigurarSockets()
    {
        foreach (var socket in sockets)
        {
            if (socket == null) continue;

            socket.selectEntered.AddListener(OnSemillaColocada);
            socket.selectExited.AddListener(OnSemillaRemovida);
            socket.hoverEntered.AddListener(OnHoverSocket);
            socket.socketActive = true;
            
            MeshRenderer renderer = socket.GetComponentInChildren<MeshRenderer>();
            if (renderer != null)
            {
                socketRenderers[socket] = renderer;
                ActualizarColorSocket(socket, false);
            }
        }
    }

    void ValidarConfiguracion()
    {
        if (sockets == null || sockets.Count == 0)
        {
            Debug.LogWarning($"[EspiralManager] No hay sockets asignados en {gameObject.name}");
        }

        if (espiralFinal == null)
        {
            Debug.LogWarning($"[EspiralManager] No hay espiral final asignado en {gameObject.name}");
        }

        sockets.RemoveAll(s => s == null);
        
        if (mostrarDebug)
        {
            Debug.Log($"[EspiralManager] Configurado con {sockets.Count} sockets. Tag requerido: '{tagSemilla}'");
        }
    }

    void OnSemillaColocada(SelectEnterEventArgs args)
    {
        var objetoColocado = args.interactableObject.transform.gameObject;
        var socket = args.interactorObject as UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor;
        
        // Validar que tenga el tag correcto
        if (!objetoColocado.CompareTag(tagSemilla))
        {
            if (mostrarDebug)
            {
                Debug.Log($"[EspiralManager] Objeto incorrecto colocado: {objetoColocado.name}");
            }
            
            StartCoroutine(RechazarObjeto(socket, objetoColocado, args));
            ReproducirSonido(sonidoSemillaIncorrecta);
            EnviarHaptic(args.interactorObject, hapticIntensidadIncorrecto);
            return;
        }

        // Semilla correcta colocada
        if (mostrarDebug)
        {
            Debug.Log($"[EspiralManager] Semilla correcta colocada en socket");
        }

        ActualizarColorSocket(socket, true);
        ReproducirSonido(sonidoColocarSemilla);
        EnviarHaptic(args.interactorObject, hapticIntensidadCorrecto);
        
        if (efectoColocarSemilla != null)
        {
            efectoColocarSemilla.transform.position = objetoColocado.transform.position;
            efectoColocarSemilla.Play();
        }

        if (prefabChispasSemilla != null)
        {
            GameObject chispas = Instantiate(prefabChispasSemilla, objetoColocado.transform.position, Quaternion.identity);
            Destroy(chispas, 2f);
        }
    }

    void OnSemillaRemovida(SelectExitEventArgs args)
    {
        var socket = args.interactorObject as UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor;
        ActualizarColorSocket(socket, false);
        
        if (mostrarDebug)
        {
            Debug.Log($"[EspiralManager] Semilla removida de socket");
        }
    }

    void OnHoverSocket(HoverEnterEventArgs args)
    {
        // Feedback sutil al acercar objeto al socket
        EnviarHaptic(args.interactorObject, 0.1f, 0.05f);
    }

    IEnumerator RechazarObjeto(UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor socket, GameObject objeto, SelectEnterEventArgs args)
    {
        yield return new WaitForSeconds(0.3f);
        
        // Forzar salida del socket
        if (socket.hasSelection)
        {
            socket.interactionManager.SelectExit(socket, socket.interactablesSelected[0]);
        }
    }

    IEnumerator VerificarSockets()
    {
        while (!completado)
        {
            int contadorLlenos = 0;

            foreach (var socket in sockets)
            {
                if (socket != null && socket.hasSelection)
                {
                    var objetoEnSocket = socket.interactablesSelected[0].transform.gameObject;
                    if (objetoEnSocket.CompareTag(tagSemilla))
                    {
                        contadorLlenos++;
                    }
                }
            }

            if (contadorLlenos != socketsLlenos)
            {
                socketsLlenos = contadorLlenos;
                ActualizarTextoUI();
                
                if (mostrarDebug)
                {
                    Debug.Log($"[EspiralManager] Progreso: {socketsLlenos}/{sockets.Count} semillas colocadas");
                }
            }

            if (socketsLlenos == sockets.Count && sockets.Count > 0)
            {
                ActivarEspiral();
                completado = true;
            }

            yield return new WaitForSeconds(0.5f);
        }
    }

    void ActivarEspiral()
    {
        if (mostrarDebug)
        {
            Debug.Log("[EspiralManager] ¡Todas las semillas colocadas! Activando espiral...");
        }

        if (espiralFinal != null)
        {
            espiralFinal.SetActive(true);
            StartCoroutine(AnimarAparicion());
        }

        if (efectoCompletado != null)
        {
            efectoCompletado.Play();
        }

        if (sonidoCompletado != null && audioSrc != null)
        {
            audioSrc.PlayOneShot(sonidoCompletado);
        }

        // Haptic feedback a todos los controladores cercanos
        EnviarHapticATodos(hapticIntensidadCompletado, hapticDuracion * 2f);
        
        // Actualizar UI final
        if (textoProgreso != null && UIVisible)
        {
            textoProgreso.text = "¡COMPLETADO!";
            textoProgreso.color = Color.green;
        }
    }

    IEnumerator AnimarAparicion()
    {
        Transform espiralTransform = espiralFinal.transform;
        Vector3 escalaOriginal = espiralTransform.localScale;
        Vector3 posicionOriginal = espiralTransform.position;
        Vector3 posicionInicial = posicionOriginal + Vector3.up * alturaAparicion;
        
        espiralTransform.localScale = Vector3.zero;
        espiralTransform.position = posicionInicial;

        float tiempoTranscurrido = 0f;

        while (tiempoTranscurrido < duracionAparicion)
        {
            tiempoTranscurrido += Time.deltaTime;
            float progreso = tiempoTranscurrido / duracionAparicion;
            float curvaProgreso = 1f - Mathf.Pow(1f - progreso, 3f);
            
            espiralTransform.localScale = Vector3.Lerp(Vector3.zero, escalaOriginal, curvaProgreso);
            espiralTransform.position = Vector3.Lerp(posicionInicial, posicionOriginal, curvaProgreso);
            espiralTransform.Rotate(Vector3.up, velocidadRotacion * Time.deltaTime, Space.Self);

            yield return null;
        }

        espiralTransform.localScale = escalaOriginal;
        espiralTransform.position = posicionOriginal;
    }

    void ActualizarColorSocket(UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor socket, bool lleno)
    {
        if (socketRenderers.ContainsKey(socket))
        {
            var renderer = socketRenderers[socket];
            if (renderer != null && renderer.material != null)
            {
                Color targetColor = lleno ? colorSocketLleno : colorSocketVacio;
                renderer.material.color = targetColor;
            }
        }
    }

    void ActualizarTextoUI()
    {
        if (textoProgreso != null && UIVisible)
        {
            textoProgreso.text = $"Semillas: {socketsLlenos}/{sockets.Count}";
            
            if (socketsLlenos == sockets.Count && sockets.Count > 0)
            {
                textoProgreso.color = Color.green;
            }
            else if (socketsLlenos > 0)
            {
                textoProgreso.color = Color.yellow;
            }
            else
            {
                textoProgreso.color = Color.white;
            }
        }
    }

    void ReproducirSonido(AudioClip clip)
    {
        if (clip != null && audioSrc != null)
        {
            audioSrc.PlayOneShot(clip, volumenEfectos);
        }
    }

    void EnviarHaptic(UnityEngine.XR.Interaction.Toolkit.Interactors.IXRInteractor interactor, float intensidad, float duracion = -1f)
    {
        if (duracion < 0) duracion = hapticDuracion;
        
        // Convertir a MonoBehaviour para acceder al GameObject
        MonoBehaviour interactorMono = interactor as MonoBehaviour;
        if (interactorMono == null) return;
        
        // Buscar ActionBasedController o XRController en el mismo GameObject o padres
        var actionController = interactorMono.GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.ActionBasedController>();
        if (actionController != null)
        {
            actionController.SendHapticImpulse(intensidad, duracion);
            return;
        }
        
        // Fallback: buscar XRController (para sistemas más antiguos)
        var xrController = interactorMono.GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.XRController>();
        if (xrController != null)
        {
            xrController.SendHapticImpulse(intensidad, duracion);
        }
    }

    void EnviarHapticATodos(float intensidad, float duracion)
    {
        // Buscar todos los ActionBasedController en la escena
        var actionControllers = FindObjectsOfType<UnityEngine.XR.Interaction.Toolkit.ActionBasedController>();
        foreach (var controller in actionControllers)
        {
            controller.SendHapticImpulse(intensidad, duracion);
        }
        
        // Fallback: buscar XRController si no hay ActionBased
        if (actionControllers.Length == 0)
        {
            var xrControllers = FindObjectsOfType<UnityEngine.XR.Interaction.Toolkit.XRController>();
            foreach (var controller in xrControllers)
            {
                controller.SendHapticImpulse(intensidad, duracion);
            }
        }
    }

    /// <summary>
    /// Llamado por ZonaActivacionUI para mostrar/ocultar el canvas
    /// </summary>
    public void MostrarUI(bool mostrar)
    {
        UIVisible = mostrar;
        
        if (canvasProgreso != null)
        {
            canvasProgreso.gameObject.SetActive(mostrar);
            
            if (mostrar)
            {
                ActualizarTextoUI();
            }
        }
        
        if (mostrarDebug)
        {
            Debug.Log($"[EspiralManager] UI {(mostrar ? "activada" : "desactivada")}");
        }
    }

    [ContextMenu("Resetear Puzzle")]
    public void ResetearPuzzle()
    {
        completado = false;
        socketsLlenos = 0;
        
        if (espiralFinal != null)
        {
            espiralFinal.SetActive(false);
        }
        
        if (efectoCompletado != null && efectoCompletado.isPlaying)
        {
            efectoCompletado.Stop();
        }
        
        foreach (var socket in sockets)
        {
            ActualizarColorSocket(socket, false);
        }
        
        StopAllCoroutines();
        StartCoroutine(VerificarSockets());
        ActualizarTextoUI();
        
        Debug.Log("[EspiralManager] Puzzle reseteado");
    }

    [ContextMenu("Forzar Activación")]
    public void ForzarActivacion()
    {
        if (!completado)
        {
            ActivarEspiral();
            completado = true;
        }
    }

    void OnValidate()
    {
        if (sockets != null)
        {
            sockets.RemoveAll(s => s == null);
        }
    }

    void OnDestroy()
    {
        StopAllCoroutines();
        
        foreach (var socket in sockets)
        {
            if (socket != null)
            {
                socket.selectEntered.RemoveListener(OnSemillaColocada);
                socket.selectExited.RemoveListener(OnSemillaRemovida);
                socket.hoverEntered.RemoveListener(OnHoverSocket);
            }
        }
    }
}