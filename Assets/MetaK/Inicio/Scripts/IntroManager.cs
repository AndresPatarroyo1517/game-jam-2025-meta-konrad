using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;

public class IntroManager : MonoBehaviour
{
    [Header("Referencias de Escena")]
    [SerializeField] private Transform xrOrigin;
    [SerializeField] private Transform playerFinalPosition;
    [SerializeField] private GameObject shamanNPC;
    [SerializeField] private Transform shamanStartPosition; // Posición inicial lejana
    [SerializeField] private Transform shamanEndPosition; // Frente al jugador
    [SerializeField] private ParticleSystem fogataParticles;
    [SerializeField] private ParticleSystem blessingParticles; // Opcional
    
    [Header("UI")]
    [SerializeField] private Image blackScreen;
    
    [Header("Audio - Fase 1: Naturaleza")]
    [SerializeField] private AudioSource natureVoiceSource;
    [SerializeField] private AudioClip natureDialogue;
    
    [Header("Audio - Fase 2 y 3: Chamán")]
    [SerializeField] private AudioSource shamanVoiceSource;
    [SerializeField] private AudioClip shamanIntroDialogue; // "Yo soy..." mientras camina
    [SerializeField] private AudioClip shamanMissionDialogue; // "Tienes que..." cuando llega
    
    [Header("Audio - Ambiente")]
    [SerializeField] private AudioSource ambientSource;
    [SerializeField] private AudioSource footstepsSource;
    [SerializeField] private AudioSource musicSource;
    
    [Header("Control del Jugador")]
    [SerializeField] private ContinuousMoveProvider moveProvider;
    [SerializeField] private ContinuousTurnProvider turnProvider;
    
    [Header("Configuración de Tiempos")]
    [SerializeField] private float fadeInDuration = 3f;
    [SerializeField] private float shamanWalkDuration = 4f; // Ajusta según tu audio de presentación
    [SerializeField] private float prayAnimationDuration = 6.15f;
    [SerializeField] private float pauseBeforePraying = 0.5f;
    
    [Header("Opcionales")]
    [SerializeField] private bool enableBreathingEffect = true;
    [SerializeField] private bool shamanStaysAfterIntro = true;
    
    private void Start()
    {
        StartCoroutine(PlayIntroSequence());
    }
    
    private IEnumerator PlayIntroSequence()
    {
        // === SETUP INICIAL ===
        SetupInitialState();
        
        // === FASE 1: VOZ DE LA NATURALEZA (en negro → fade gradual) ===
        float natureDuration = 0f;
        
        // Iniciar audio ambiente y música suave
        if (ambientSource != null) 
        {
            ambientSource.volume = 0.3f;
            ambientSource.Play();
        }
        
        if (musicSource != null)
        {
            musicSource.volume = 0.2f;
            musicSource.Play();
        }
        
        // Reproducir voz de la naturaleza
        if (natureDialogue != null && natureVoiceSource != null)
        {
            natureVoiceSource.PlayOneShot(natureDialogue);
            natureDuration = natureDialogue.length;
        }
        else
        {
            natureDuration = 5f;
        }
        
        // Fade gradual desde negro mientras habla la naturaleza
        StartCoroutine(FadeScreen(1f, 0f, fadeInDuration));
        
        // Efecto de respiración opcional
        if (enableBreathingEffect)
        {
            StartCoroutine(SubtleBreathing(natureDuration));
        }
        
        // Esperar a que termine el audio de naturaleza
        yield return new WaitForSeconds(natureDuration);
        
        // Asegurar que el fade terminó
        yield return new WaitForSeconds(Mathf.Max(0, fadeInDuration - natureDuration));
        
        // === FASE 2: CHAMÁN APARECE, CAMINA Y SE PRESENTA ===
        
        // Colocar chamán en posición inicial (lejos)
        if (shamanNPC != null && shamanStartPosition != null)
        {
            shamanNPC.transform.position = shamanStartPosition.position;
            shamanNPC.transform.rotation = shamanStartPosition.rotation;
            shamanNPC.SetActive(true);
            
            // Hacer que mire hacia el jugador
            
        }
        
        // AUDIO 1: Chamán se presenta ("Yo soy...")
        float introDialogueDuration = 0f;
        if (shamanIntroDialogue != null && shamanVoiceSource != null)
        {
            shamanVoiceSource.PlayOneShot(shamanIntroDialogue);
            introDialogueDuration = shamanIntroDialogue.length;
        }
        else
        {
            introDialogueDuration = 4f; // Fallback
        }
        
        // Ajustar duración de caminata al audio (o viceversa)
        float actualWalkDuration = Mathf.Max(shamanWalkDuration, introDialogueDuration);
        
        // INICIAR CAMINATA (en paralelo con el audio de presentación)
        StartCoroutine(ShamanWalkToPlayer(actualWalkDuration));
        
        // Esperar a que termine la caminata Y el audio de presentación
        yield return new WaitForSeconds(actualWalkDuration);
        
        // === FASE 3: CHAMÁN EXPLICA LA MISIÓN ===
        
        // AUDIO 2: Misión ("Tienes que...")
        float missionDialogueDuration = 0f;
        if (shamanMissionDialogue != null && shamanVoiceSource != null)
        {
            shamanVoiceSource.PlayOneShot(shamanMissionDialogue);
            missionDialogueDuration = shamanMissionDialogue.length;
        }
        else
        {
            missionDialogueDuration = 10f; // Fallback
        }
        
        // Esperar a que termine el diálogo de misión
        yield return new WaitForSeconds(missionDialogueDuration);
        
        // === FASE 4: RITUAL DE BENDICIÓN ===
        yield return new WaitForSeconds(pauseBeforePraying);
        
        Animator shamanAnimator = shamanNPC != null ? shamanNPC.GetComponentInChildren<Animator>() : null;
        if (shamanAnimator != null)
        {
            shamanAnimator.SetTrigger("Praying");
        }
        
        // Activar partículas de bendición
        if (blessingParticles != null)
        {
            blessingParticles.Play();
        }
        
        // Aumentar volumen de música durante ritual
        if (musicSource != null)
        {
            StartCoroutine(FadeAudioVolume(musicSource, musicSource.volume, 0.5f, prayAnimationDuration * 0.5f));
        }
        
        yield return new WaitForSeconds(prayAnimationDuration);
        
        // === FASE 5: ACTIVACIÓN DEL JUGADOR ===
        EnablePlayerControl();
        
        // Decidir si el chamán se queda o se va
        if (!shamanStaysAfterIntro && shamanNPC != null)
        {
            StartCoroutine(ShamanDeparture());
        }
        
        Debug.Log("Intro completada - Jugador tiene control total");
    }
    
    private void SetupInitialState()
    {
        // Posicionar jugador
        if (xrOrigin != null && playerFinalPosition != null)
        {
            xrOrigin.position = playerFinalPosition.position;
            xrOrigin.rotation = playerFinalPosition.rotation;
        }
        
        // Pantalla negra inicial
        SetScreenAlpha(1f);
        
        // Desactivar controles
        if (moveProvider != null) moveProvider.enabled = false;
        if (turnProvider != null) turnProvider.enabled = false;
        
        // Ocultar chamán inicialmente
        if (shamanNPC != null) shamanNPC.SetActive(false);
        
        // Fogata activa
        if (fogataParticles != null) fogataParticles.Play();
        
        // Configurar audio
        if (natureVoiceSource != null) natureVoiceSource.loop = false;
        if (shamanVoiceSource != null) shamanVoiceSource.loop = false;
        if (footstepsSource != null) 
        {
            footstepsSource.loop = true;
            footstepsSource.volume = 0.4f;
        }
    }
    
    private IEnumerator ShamanWalkToPlayer(float duration)
    {
        if (shamanNPC == null || shamanEndPosition == null) yield break;
        
        Vector3 startPos = shamanNPC.transform.position;
        Vector3 endPos = shamanEndPosition.position;
        Quaternion startRot = shamanNPC.transform.rotation;
        Quaternion endRot = shamanEndPosition.rotation;
        
        // Iniciar animación de caminar
        Animator animator = shamanNPC.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.SetBool("IsWalking", true);
        }
        
        // Reproducir sonido de pasos
        if (footstepsSource != null) footstepsSource.Play();
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Movimiento suave con ease in-out
            float smoothT = t * t * (3f - 2f * t);
            
            shamanNPC.transform.position = Vector3.Lerp(startPos, endPos, smoothT);
            shamanNPC.transform.rotation = Quaternion.Lerp(startRot, endRot, smoothT);
            
            yield return null;
        }
        
        // Detener animación de caminar
        if (animator != null)
        {
            animator.SetBool("IsWalking", false);
        }
        
        // Detener pasos
        if (footstepsSource != null) footstepsSource.Stop();
        
        shamanNPC.transform.position = endPos;
        shamanNPC.transform.rotation = endRot;
    }
    
    private void EnablePlayerControl()
    {
        if (moveProvider != null) moveProvider.enabled = true;
        if (turnProvider != null) turnProvider.enabled = true;
    }
    
    private IEnumerator ShamanDeparture()
    {
        float duration = 5f;
        Vector3 startPos = shamanNPC.transform.position;
        Vector3 endPos = startPos + shamanNPC.transform.forward * 8f;
        
        Animator animator = shamanNPC.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.SetBool("IsWalking", true);
        }
        
        if (footstepsSource != null) footstepsSource.Play();
        
        // Obtener renderers
        Renderer[] renderers = shamanNPC.GetComponentsInChildren<Renderer>();
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
            
            shamanNPC.transform.position = Vector3.Lerp(startPos, endPos, t);
            
            // Fade out en los últimos 2 segundos
            if (t > 0.6f)
            {
                float fadeT = (t - 0.6f) / 0.4f;
                float alpha = Mathf.Lerp(1f, 0f, fadeT);
                
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
            }
            
            yield return null;
        }
        
        shamanNPC.SetActive(false);
    }
    
    private IEnumerator SubtleBreathing(float duration)
    {
        if (xrOrigin == null) yield break;
        
        float elapsed = 0f;
        Vector3 startPos = xrOrigin.position;
        
        while (elapsed < duration)
        {
            float breath = Mathf.Sin(elapsed * 1.5f) * 0.015f;
            xrOrigin.position = startPos + Vector3.up * breath;
            elapsed += Time.deltaTime;
            yield return null;
        }
        
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
            float smoothT = t * t * (3f - 2f * t);
            float alpha = Mathf.Lerp(startAlpha, endAlpha, smoothT);
            SetScreenAlpha(alpha);
            yield return null;
        }
        
        SetScreenAlpha(endAlpha);
    }
    
    private IEnumerator FadeAudioVolume(AudioSource source, float startVol, float endVol, float duration)
    {
        if (source == null) yield break;
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(startVol, endVol, elapsed / duration);
            yield return null;
        }
        
        source.volume = endVol;
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
