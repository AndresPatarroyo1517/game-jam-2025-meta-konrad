using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;

public class IntroManager : MonoBehaviour
{
    [Header("Referencias de Escena")]
    [SerializeField] private Transform xrOrigin;
    [SerializeField] private Transform ritualPosition;
    [SerializeField] private Transform postRitualPosition;
    [SerializeField] private GameObject shamanNPC;
    [SerializeField] private ParticleSystem fogataParticles;
    
    [Header("UI")]
    [SerializeField] private Image blackScreen;
    
    [Header("Audio")]
    [SerializeField] private AudioSource voiceSource;
    [SerializeField] private AudioClip shamanDialogue;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource ambientSource;
    
    [Header("Control del Jugador")]
    [SerializeField] private ContinuousMoveProvider moveProvider;
    [SerializeField] private ContinuousTurnProvider turnProvider;
    
    [Header("Configuración")]
    [SerializeField] private float fadeInDuration = 2f;
    [SerializeField] private float prayAnimationDuration = 3f;
    [SerializeField] private float fadeOutDuration = 1f;
    [SerializeField] private float finalFadeInDuration = 1.5f;
    [SerializeField] private bool enableBreathingEffect = true;
    
    private void Start()
    {
        StartCoroutine(PlayIntroSequence());
    }
    
    private IEnumerator PlayIntroSequence()
    {
        // === SETUP INICIAL ===
        SetupInitialState();
        
        // === FASE 1: FADE IN DESDE NEGRO ===
        yield return FadeScreen(1f, 0f, fadeInDuration);
        
        // === FASE 2: RITUAL - DIÁLOGO ===
        // Iniciar música y ambiente
        if (musicSource != null) musicSource.Play();
        if (ambientSource != null) ambientSource.Play();
        
        // Reproducir diálogo del chamán
        float dialogueDuration = 0f;
        if (shamanDialogue != null && voiceSource != null)
        {
            voiceSource.PlayOneShot(shamanDialogue);
            dialogueDuration = shamanDialogue.length;
        }
        else
        {
            // Si no hay audio, duración por defecto
            dialogueDuration = 5f;
        }
        
        // Efecto de respiración sutil durante el diálogo
        if (enableBreathingEffect)
        {
            StartCoroutine(SubtleBreathing(dialogueDuration));
        }
        
        // Esperar a que termine el diálogo
        yield return new WaitForSeconds(dialogueDuration);
        
        // === FASE 2.5: ORACIÓN DEL CHAMÁN ===
        // Pequeña pausa antes de la oración (opcional pero mejora el timing)
        yield return new WaitForSeconds(0.5f);
        
        // Reproducir animación de pray
        Animator shamanAnimator = shamanNPC != null ? shamanNPC.GetComponent<Animator>() : null;
        if (shamanAnimator != null)
        {
            shamanAnimator.SetTrigger("Praying");
        }
        
        // Esperar duración de la animación de pray
        yield return new WaitForSeconds(prayAnimationDuration);
        
        // === FASE 3: TRANSICIÓN ===
        // Fade a negro breve
        yield return FadeScreen(0f, 1f, fadeOutDuration);
        
        // Mientras está en negro, reposicionar y activar controles
        RepositionPlayer();
        EnablePlayerControl();
        
        // Chamán comienza a alejarse o desaparecer
        if (shamanNPC != null)
        {
            StartCoroutine(ShamanDeparture());
        }
        
        // Fade in final
        yield return FadeScreen(1f, 0f, finalFadeInDuration);
        
        // === FIN DE INTRO ===
        Debug.Log("Intro completada - Jugador tiene control total");
    }
    
    private void SetupInitialState()
    {
        // Posicionar jugador en posición ritual (de rodillas)
        if (xrOrigin != null && ritualPosition != null)
        {
            xrOrigin.position = ritualPosition.position;
            xrOrigin.rotation = ritualPosition.rotation;
        }
        
        // Pantalla negra inicial
        SetScreenAlpha(1f);
        
        // Desactivar controles de movimiento
        if (moveProvider != null) moveProvider.enabled = false;
        if (turnProvider != null) turnProvider.enabled = false;
        
        // Asegurar que fogata está activa
        if (fogataParticles != null) fogataParticles.Play();
        
        // Configurar volumen inicial de audio
        if (musicSource != null)
        {
            musicSource.volume = 0.3f;
            musicSource.loop = true;
        }
        
        if (ambientSource != null)
        {
            ambientSource.volume = 0.5f;
            ambientSource.loop = true;
        }
        
        if (voiceSource != null)
        {
            voiceSource.loop = false;
        }
    }
    
    private void RepositionPlayer()
    {
        // Mover a posición final (de pie)
        if (xrOrigin != null && postRitualPosition != null)
        {
            xrOrigin.position = postRitualPosition.position;
            xrOrigin.rotation = postRitualPosition.rotation;
        }
    }
    
    private void EnablePlayerControl()
    {
        // Activar sistemas de movimiento
        if (moveProvider != null) moveProvider.enabled = true;
        if (turnProvider != null) turnProvider.enabled = true;
    }
    
    private IEnumerator ShamanDeparture()
    {
        float duration = 4f;
        Vector3 startPos = shamanNPC.transform.position;
        Vector3 endPos = startPos + shamanNPC.transform.forward * 6f;
        
        // Obtener todos los renderers del chamán
        Renderer[] renderers = shamanNPC.GetComponentsInChildren<Renderer>();
        
        // Guardar colores originales
        Color[][] originalColors = new Color[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] mats = renderers[i].materials;
            originalColors[i] = new Color[mats.Length];
            for (int j = 0; j < mats.Length; j++)
            {
                originalColors[i][j] = mats[j].color;
            }
        }
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Mover al chamán
            shamanNPC.transform.position = Vector3.Lerp(startPos, endPos, t);
            
            // Fade out gradual del chamán
            float alpha = Mathf.Lerp(1f, 0f, t);
            for (int i = 0; i < renderers.Length; i++)
            {
                Material[] mats = renderers[i].materials;
                for (int j = 0; j < mats.Length; j++)
                {
                    Color c = originalColors[i][j];
                    c.a = alpha;
                    mats[j].color = c;
                }
            }
            
            yield return null;
        }
        
        // Desactivar chamán al final
        shamanNPC.SetActive(false);
    }
    
    private IEnumerator SubtleBreathing(float duration)
    {
        if (xrOrigin == null) yield break;
        
        float elapsed = 0f;
        Vector3 startPos = xrOrigin.position;
        
        while (elapsed < duration)
        {
            // Movimiento sinusoidal muy sutil (±1.5cm)
            float breath = Mathf.Sin(elapsed * 1.5f) * 0.015f;
            xrOrigin.position = startPos + Vector3.up * breath;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Restaurar posición original
        xrOrigin.position = startPos;
    }
    
    private IEnumerator FadeScreen(float startAlpha, float endAlpha, float duration)
    {
        if (blackScreen == null) yield break;
        
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Curva suave (ease in/out)
            float smoothT = t * t * (3f - 2f * t);
            float alpha = Mathf.Lerp(startAlpha, endAlpha, smoothT);
            
            SetScreenAlpha(alpha);
            yield return null;
        }
        
        SetScreenAlpha(endAlpha);
    }
    
    private void SetScreenAlpha(float alpha)
    {
        if (blackScreen != null)
        {
            Color c = blackScreen.color;
            c.a = Mathf.Clamp01(alpha);
            blackScreen.color = c;
        }
    }
}
