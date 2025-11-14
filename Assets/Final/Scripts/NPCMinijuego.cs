using UnityEngine;
using System.Collections;
using UnityEngine.Events;

public class NPCMinijuego : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Transform de la cámara XR (jugador)")]
    public Transform camaraXR;
    
    [Tooltip("Script del minijuego (debe tener método para verificar si está completado)")]
    public MonoBehaviour scriptMinijuego;

    [Header("Audios de Diálogos")]
    [Tooltip("Audio que reproduce al acercarse por primera vez")]
    public AudioClip audioInicial;
    
    [Tooltip("Audio que reproduce cuando se completa el minijuego")]
    public AudioClip audioCompletado;
    
    [Tooltip("Audio de despedida final (después de completar)")]
    public AudioClip audioDespedida;

    [Header("Configuración de Proximidad")]
    [Range(0.5f, 10f)]
    [Tooltip("Distancia a la que se activa el diálogo inicial")]
    public float distanciaActivacion = 3f;
    
    [Tooltip("Solo activar diálogo inicial una vez")]
    public bool dialogoInicialUnaVez = true;
    
    [Range(0f, 5f)]
    [Tooltip("Tiempo de espera antes de reproducir audio de completado")]
    public float delayAudioCompletado = 1.5f;
    
    [Range(0f, 5f)]
    [Tooltip("Tiempo de espera antes de reproducir audio de despedida")]
    public float delayAudioDespedida = 1f;

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
    
    [Tooltip("Nombre del trigger de animación para celebrar")]
    public string triggerCelebrar = "Celebrar";

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

    [Header("Eventos (Para tu compañero)")]
    [Tooltip("Se invoca cuando el jugador se acerca por primera vez")]
    public UnityEvent OnJugadorAcerca;
    
    [Tooltip("Se invoca cuando el minijuego se completa")]
    public UnityEvent OnMinijuegoCompletado;
    
    [Tooltip("Se invoca después de la despedida")]
    public UnityEvent OnDespedidaFinal;

    [Header("Debug")]
    public bool mostrarDebug = true;
    public bool mostrarGizmos = true;

    // Estados internos
    private AudioSource audioSource;
    private bool dialogoInicialReproducido = false;
    private bool dialogoCompletadoReproducido = false;
    private bool dialogoDespedidaReproducido = false;
    private bool jugadorCerca = false;
    private bool minijuegoCompletado = false;
    
    // Interfaz para verificar completado (tu compañero implementará esto)
    private IMinijuegoCompletable interfazMinijuego;

    void Start()
    {
        InicializarComponentes();
        BuscarCamara();
        ConfigurarMinijuego();
        ValidarConfiguracion();
        
        // Ocultar icono al inicio
        if (iconoDialogo != null)
        {
            iconoDialogo.SetActive(false);
        }
        
        // Iniciar verificación de completado
        StartCoroutine(VerificarCompletado());
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

    void ConfigurarMinijuego()
    {
        // Intentar obtener la interfaz del minijuego
        if (scriptMinijuego != null)
        {
            interfazMinijuego = scriptMinijuego as IMinijuegoCompletable;
            
            if (interfazMinijuego == null && mostrarDebug)
            {
                Debug.LogWarning($"[NPCMinijuego] El script '{scriptMinijuego.GetType().Name}' no implementa IMinijuegoCompletable. " +
                    "Asegúrate de implementar la interfaz o usar el método público EstaCompletado()");
            }
        }
    }

    void ValidarConfiguracion()
    {
        if (camaraXR == null)
        {
            Debug.LogError($"[NPCMinijuego] No se encontró la cámara XR en '{gameObject.name}'");
        }

        if (audioInicial == null)
        {
            Debug.LogWarning($"[NPCMinijuego] No hay audio inicial asignado en '{gameObject.name}'");
        }

        if (audioCompletado == null)
        {
            Debug.LogWarning($"[NPCMinijuego] No hay audio de completado asignado en '{gameObject.name}'");
        }

        if (audioDespedida == null)
        {
            Debug.LogWarning($"[NPCMinijuego] No hay audio de despedida asignado en '{gameObject.name}'");
        }

        if (scriptMinijuego == null)
        {
            Debug.LogWarning($"[NPCMinijuego] No hay script de minijuego asignado en '{gameObject.name}'");
        }

        if (mostrarDebug)
        {
            Debug.Log($"[NPCMinijuego] '{gameObject.name}' configurado. Distancia activación: {distanciaActivacion}m");
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
            OnJugadorAcercarse();
        }
        else if (!jugadorCercaAhora && jugadorCerca)
        {
            jugadorCerca = false;
            OnJugadorAlejarse();
        }

        // Hacer que el NPC mire al jugador cuando está cerca
        if (jugadorCerca)
        {
            MirarAlJugador();
        }
    }

    void OnJugadorAcercarse()
    {
        if (mostrarDebug)
        {
            Debug.Log($"[NPCMinijuego] Jugador cerca de '{gameObject.name}'");
        }

        // Mostrar icono de diálogo
        if (iconoDialogo != null)
        {
            iconoDialogo.SetActive(true);
        }

        // Reproducir diálogo inicial si no se ha reproducido
        if (!dialogoInicialReproducido && audioInicial != null)
        {
            ReproducirDialogo(audioInicial, TipoDialogo.Inicial);
            
            if (dialogoInicialUnaVez)
            {
                dialogoInicialReproducido = true;
            }
            
            // Invocar evento
            OnJugadorAcerca?.Invoke();
        }
    }

    void OnJugadorAlejarse()
    {
        if (mostrarDebug)
        {
            Debug.Log($"[NPCMinijuego] Jugador se alejó de '{gameObject.name}'");
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
        while (!minijuegoCompletado)
        {
            // MÉTODO 1: Verificar usando la interfaz
            if (interfazMinijuego != null)
            {
                if (interfazMinijuego.EstaCompletado())
                {
                    minijuegoCompletado = true;
                    StartCoroutine(SecuenciaCompletado());
                }
            }
            // MÉTODO 2: Verificar usando Reflection (buscar método público "EstaCompletado")
            else if (scriptMinijuego != null)
            {
                var metodo = scriptMinijuego.GetType().GetMethod("EstaCompletado");
                if (metodo != null)
                {
                    bool completado = (bool)metodo.Invoke(scriptMinijuego, null);
                    if (completado)
                    {
                        minijuegoCompletado = true;
                        StartCoroutine(SecuenciaCompletado());
                    }
                }
            }
            
            yield return new WaitForSeconds(0.5f);
        }
    }

    IEnumerator SecuenciaCompletado()
    {
        if (mostrarDebug)
        {
            Debug.Log($"[NPCMinijuego] ¡Minijuego completado en '{gameObject.name}'!");
        }

        // Invocar evento de completado
        OnMinijuegoCompletado?.Invoke();

        // Esperar antes del audio de completado
        yield return new WaitForSeconds(delayAudioCompletado);

        // 1. Reproducir audio de completado
        if (audioCompletado != null && !dialogoCompletadoReproducido)
        {
            ReproducirDialogo(audioCompletado, TipoDialogo.Completado);
            dialogoCompletadoReproducido = true;
            
            // Esperar a que termine el audio
            yield return new WaitForSeconds(audioCompletado.length);
        }

        // Esperar antes de la despedida
        yield return new WaitForSeconds(delayAudioDespedida);

        // 2. Reproducir audio de despedida
        if (audioDespedida != null && !dialogoDespedidaReproducido)
        {
            ReproducirDialogo(audioDespedida, TipoDialogo.Despedida);
            dialogoDespedidaReproducido = true;
            
            // Esperar a que termine
            yield return new WaitForSeconds(audioDespedida.length);
            
            // Invocar evento final
            OnDespedidaFinal?.Invoke();
        }
    }

    void ReproducirDialogo(AudioClip clip, TipoDialogo tipo)
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
            Debug.Log($"[NPCMinijuego] Reproduciendo diálogo {tipo} de '{gameObject.name}' ({clip.length:F1}s)");
        }

        // Activar efectos visuales
        StartCoroutine(EfectosHablar(clip.length));

        // Activar animación según el tipo
        if (animatorNPC != null)
        {
            if (tipo == TipoDialogo.Completado && !string.IsNullOrEmpty(triggerCelebrar))
            {
                animatorNPC.SetTrigger(triggerCelebrar);
            }
            else if (!string.IsNullOrEmpty(triggerHablar))
            {
                animatorNPC.SetTrigger(triggerHablar);
            }
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

    #region Métodos Públicos para Control Manual
    
    /// <summary>
    /// Método público para reproducir el diálogo inicial manualmente
    /// </summary>
    public void ReproducirDialogoInicial()
    {
        if (audioInicial != null)
        {
            ReproducirDialogo(audioInicial, TipoDialogo.Inicial);
        }
    }

    /// <summary>
    /// Método público para reproducir el diálogo de completado manualmente
    /// </summary>
    public void ReproducirDialogoCompletado()
    {
        if (audioCompletado != null)
        {
            ReproducirDialogo(audioCompletado, TipoDialogo.Completado);
        }
    }

    /// <summary>
    /// Método público para reproducir el diálogo de despedida manualmente
    /// </summary>
    public void ReproducirDialogoDespedida()
    {
        if (audioDespedida != null)
        {
            ReproducirDialogo(audioDespedida, TipoDialogo.Despedida);
        }
    }

    /// <summary>
    /// Marcar el minijuego como completado manualmente (útil para pruebas)
    /// </summary>
    public void MarcarMinijuegoCompletado()
    {
        if (!minijuegoCompletado)
        {
            minijuegoCompletado = true;
            StartCoroutine(SecuenciaCompletado());
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
        dialogoDespedidaReproducido = false;
        minijuegoCompletado = false;
        jugadorCerca = false;
        
        StopAllCoroutines();
        
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        
        if (iconoDialogo != null)
        {
            iconoDialogo.SetActive(false);
        }
        
        // Reiniciar corrutina de verificación
        StartCoroutine(VerificarCompletado());
        
        Debug.Log($"[NPCMinijuego] NPC '{gameObject.name}' reseteado");
    }
    
    #endregion

    // Visualización en el editor
    void OnDrawGizmos()
    {
        if (!mostrarGizmos) return;

        // Dibujar esfera de activación
        Gizmos.color = jugadorCerca ? new Color(0f, 1f, 0f, 0.3f) : new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, distanciaActivacion);
        
        // Cambiar color si está completado
        if (minijuegoCompletado)
        {
            Gizmos.color = new Color(0f, 0.5f, 1f, 0.5f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, distanciaActivacion * 0.5f);
        }
        
        // Dibujar línea hacia el jugador si está cerca
        if (jugadorCerca && camaraXR != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position + Vector3.up, camaraXR.position);
        }

#if UNITY_EDITOR
        // Etiqueta con info
        string estado = minijuegoCompletado ? "COMPLETADO" : "Esperando";
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 2f, 
            $"NPC: {gameObject.name}\n" +
            $"Activación: {distanciaActivacion}m\n" +
            $"Estado: {estado}"
        );
#endif
    }

    void OnValidate()
    {
        distanciaActivacion = Mathf.Max(0.5f, distanciaActivacion);
        volumen = Mathf.Clamp01(volumen);
        delayAudioCompletado = Mathf.Max(0f, delayAudioCompletado);
        delayAudioDespedida = Mathf.Max(0f, delayAudioDespedida);
    }

    void OnDestroy()
    {
        StopAllCoroutines();
    }

    // Enumerador para tipos de diálogo
    private enum TipoDialogo
    {
        Inicial,
        Completado,
        Despedida
    }
}

// ============================================
// INTERFAZ PARA EL MINIJUEGO (Tu compañero usa esto)
// ============================================
/// <summary>
/// Interfaz que debe implementar el script del minijuego
/// </summary>
public interface IMinijuegoCompletable
{
    /// <summary>
    /// Retorna true cuando el minijuego está completado
    /// </summary>
    bool EstaCompletado();
}