using UnityEngine;
using System.Collections;

public class NPCDialogos : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Transform de la cámara XR (jugador)")]
    public Transform camaraXR;
    
    [Tooltip("EspiralManager del puzzle asociado")]
    public EspiralManager espiralManager;

    [Header("Audios de Diálogos")]
    [Tooltip("Audio que reproduce al acercarse por primera vez")]
    public AudioClip audioInicial;
    
    [Tooltip("Audio que reproduce cuando se completa el puzzle")]
    public AudioClip audioCompletado;

    [Header("Configuración de Proximidad")]
    [Range(0.5f, 10f)]
    [Tooltip("Distancia a la que se activa el diálogo inicial")]
    public float distanciaActivacion = 3f;
    
    [Tooltip("Solo activar una vez (evita repeticiones)")]
    public bool dialogoInicialUnaVez = true;

    [Header("Efectos Visuales (Opcional)")]
    [Tooltip("Icono de diálogo sobre el NPC")]
    public GameObject iconoDialogo;
    
    [Tooltip("Partículas al hablar")]
    public ParticleSystem particulasHablar;
    
    [Tooltip("Luz que parpadea al hablar")]
    public Light luzIndicadora;

    [Header("Animación (Opcional)")]
    [Tooltip("Animator del NPC")]
    public Animator animatorNPC;
    
    [Tooltip("Nombre del trigger de animación para hablar")]
    public string triggerHablar = "Hablar";

    [Header("Audio 3D")]
    [Range(0f, 1f)]
    public float volumen = 0.8f;
    
    [Range(1f, 20f)]
    [Tooltip("Distancia máxima para escuchar el audio")]
    public float maxDistanciaAudio = 15f;

    [Header("Corrección de Rotación")]
    [Tooltip("Offset de rotación del modelo (si está rotado en el prefab)")]
    [Range(-180f, 180f)]
    public float offsetRotacionModelo = 45f;

    [Header("Debug")]
    public bool mostrarDebug = true;
    public bool mostrarGizmos = true;

    private AudioSource audioSource;
    private bool dialogoInicialReproducido = false;
    private bool dialogoCompletadoReproducido = false;
    private bool jugadorCerca = false;
    private bool puzzleCompletado = false;

    void Start()
    {
        InicializarComponentes();
        BuscarCamara();
        ValidarConfiguracion();
        
        // Ocultar icono al inicio
        if (iconoDialogo != null)
        {
            iconoDialogo.SetActive(false);
        }
        
        // Suscribirse al evento de completado del puzzle
        if (espiralManager != null)
        {
            StartCoroutine(VerificarCompletado());
        }
    }

    void InicializarComponentes()
    {
        // Configurar AudioSource 3D
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // Audio 3D completo
        audioSource.volume = volumen;
        audioSource.maxDistance = maxDistanciaAudio;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.dopplerLevel = 0f; // Desactivar efecto doppler en VR
    }

    void BuscarCamara()
    {
        if (camaraXR == null)
        {
            GameObject mainCam = GameObject.FindGameObjectWithTag("MainCamera");
            if (mainCam != null)
            {
                camaraXR = mainCam.transform;
            }
            else
            {
                mainCam = GameObject.Find("Main Camera") ?? GameObject.Find("XR Camera");
                if (mainCam != null)
                {
                    camaraXR = mainCam.transform;
                }
            }
        }
    }

    void ValidarConfiguracion()
    {
        if (camaraXR == null)
        {
            Debug.LogError($"[NPCDialogos] No se encontró la cámara XR en '{gameObject.name}'");
        }

        if (audioInicial == null)
        {
            Debug.LogWarning($"[NPCDialogos] No hay audio inicial asignado en '{gameObject.name}'");
        }

        if (audioCompletado == null)
        {
            Debug.LogWarning($"[NPCDialogos] No hay audio de completado asignado en '{gameObject.name}'");
        }

        if (espiralManager == null)
        {
            Debug.LogWarning($"[NPCDialogos] No hay EspiralManager asignado en '{gameObject.name}'");
        }

        if (mostrarDebug)
        {
            Debug.Log($"[NPCDialogos] '{gameObject.name}' configurado. Distancia activación: {distanciaActivacion}m");
        }
    }

    void Update()
    {
        if (camaraXR == null) return;

        // Calcular distancia al jugador
        float distancia = Vector3.Distance(transform.position, camaraXR.position);
        bool jugadorCercaAhora = distancia <= distanciaActivacion;

        // Detectar cuando el jugador entra en rango
        if (jugadorCercaAhora && !jugadorCerca)
        {
            jugadorCerca = true;
            OnJugadorCerca();
        }
        else if (!jugadorCercaAhora && jugadorCerca)
        {
            jugadorCerca = false;
            OnJugadorLejos();
        }

        // Hacer que el NPC mire al jugador cuando está cerca
        if (jugadorCerca)
        {
            MirarAlJugador();
        }
    }

    void OnJugadorCerca()
    {
        if (mostrarDebug)
        {
            Debug.Log($"[NPCDialogos] Jugador cerca de '{gameObject.name}'");
        }

        // Mostrar icono de diálogo
        if (iconoDialogo != null)
        {
            iconoDialogo.SetActive(true);
        }

        // Reproducir diálogo inicial si no se ha reproducido
        if (!dialogoInicialReproducido && audioInicial != null)
        {
            ReproducirDialogo(audioInicial, true);
            
            if (dialogoInicialUnaVez)
            {
                dialogoInicialReproducido = true;
            }
        }
    }

    void OnJugadorLejos()
    {
        if (mostrarDebug)
        {
            Debug.Log($"[NPCDialogos] Jugador se alejó de '{gameObject.name}'");
        }

        // Ocultar icono
        if (iconoDialogo != null)
        {
            iconoDialogo.SetActive(false);
        }
    }

    void MirarAlJugador()
    {
        // Hacer que el NPC rote solo en Y (mantener vertical)
        Vector3 direccion = camaraXR.position - transform.position;
        direccion.y = 0; // Ignorar diferencia de altura
        
        if (direccion != Vector3.zero)
        {
            Quaternion rotacionObjetivo = Quaternion.LookRotation(direccion);
            
            // Aplicar offset de corrección para el modelo
            rotacionObjetivo *= Quaternion.Euler(0f, offsetRotacionModelo, 0f);
            
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacionObjetivo, Time.deltaTime * 2f);
        }
    }

    IEnumerator VerificarCompletado()
    {
        while (!puzzleCompletado)
        {
            // Verificar si el puzzle se completó (puedes ajustar esta lógica)
            if (espiralManager != null && espiralManager.espiralFinal != null)
            {
                if (espiralManager.espiralFinal.activeInHierarchy && !dialogoCompletadoReproducido)
                {
                    puzzleCompletado = true;
                    
                    // Esperar un momento antes de hablar
                    yield return new WaitForSeconds(1.5f);
                    
                    OnPuzzleCompletado();
                }
            }
            
            yield return new WaitForSeconds(0.5f);
        }
    }

    void OnPuzzleCompletado()
    {
        if (mostrarDebug)
        {
            Debug.Log($"[NPCDialogos] Puzzle completado! Reproduciendo audio de '{gameObject.name}'");
        }

        if (audioCompletado != null && !dialogoCompletadoReproducido)
        {
            ReproducirDialogo(audioCompletado, false);
            dialogoCompletadoReproducido = true;
        }
    }

    void ReproducirDialogo(AudioClip clip, bool esInicial)
    {
        if (clip == null || audioSource == null) return;

        // Detener audio actual si está reproduciendo
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        // Reproducir nuevo audio
        audioSource.clip = clip;
        audioSource.Play();

        if (mostrarDebug)
        {
            string tipo = esInicial ? "inicial" : "completado";
            Debug.Log($"[NPCDialogos] Reproduciendo diálogo {tipo} de '{gameObject.name}' ({clip.length:F1}s)");
        }

        // Activar efectos visuales
        StartCoroutine(EfectosHablar(clip.length));

        // Activar animación si existe
        if (animatorNPC != null && !string.IsNullOrEmpty(triggerHablar))
        {
            animatorNPC.SetTrigger(triggerHablar);
        }
    }

    IEnumerator EfectosHablar(float duracion)
    {
        // Activar partículas
        if (particulasHablar != null)
        {
            particulasHablar.Play();
        }

        // Parpadeo de luz
        if (luzIndicadora != null)
        {
            float tiempoTranscurrido = 0f;
            float intensidadOriginal = luzIndicadora.intensity;
            
            while (tiempoTranscurrido < duracion)
            {
                // Parpadeo sincronizado con el habla
                float parpadeo = Mathf.PingPong(Time.time * 8f, 1f);
                luzIndicadora.intensity = intensidadOriginal * (0.5f + parpadeo * 0.5f);
                
                tiempoTranscurrido += Time.deltaTime;
                yield return null;
            }
            
            luzIndicadora.intensity = intensidadOriginal;
        }

        // Detener partículas
        if (particulasHablar != null)
        {
            particulasHablar.Stop();
        }
    }

    /// <summary>
    /// Método público para reproducir el diálogo inicial manualmente
    /// </summary>
    public void ReproducirDialogoInicial()
    {
        if (audioInicial != null)
        {
            ReproducirDialogo(audioInicial, true);
        }
    }

    /// <summary>
    /// Método público para reproducir el diálogo de completado manualmente
    /// </summary>
    public void ReproducirDialogoCompletado()
    {
        if (audioCompletado != null)
        {
            ReproducirDialogo(audioCompletado, false);
        }
    }

    /// <summary>
    /// Resetear el estado del NPC
    /// </summary>
    [ContextMenu("Resetear NPC")]
    public void ResetearNPC()
    {
        dialogoInicialReproducido = false;
        dialogoCompletadoReproducido = false;
        puzzleCompletado = false;
        jugadorCerca = false;
        
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        
        if (iconoDialogo != null)
        {
            iconoDialogo.SetActive(false);
        }
        
        Debug.Log($"[NPCDialogos] NPC '{gameObject.name}' reseteado");
    }

    // Visualización en el editor
    void OnDrawGizmos()
    {
        if (!mostrarGizmos) return;

        // Dibujar esfera de activación
        Gizmos.color = jugadorCerca ? new Color(0f, 1f, 0f, 0.3f) : new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, distanciaActivacion);
        
        // Dibujar línea hacia el jugador si está cerca
        if (jugadorCerca && camaraXR != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position + Vector3.up, camaraXR.position);
        }

#if UNITY_EDITOR
        // Etiqueta con info
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 2f, 
            $"NPC: {gameObject.name}\nActivación: {distanciaActivacion}m"
        );
#endif
    }

    void OnValidate()
    {
        distanciaActivacion = Mathf.Max(0.5f, distanciaActivacion);
        volumen = Mathf.Clamp01(volumen);
    }

    void OnDestroy()
    {
        StopAllCoroutines();
    }
}