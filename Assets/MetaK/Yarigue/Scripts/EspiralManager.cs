using UnityEngine;

using System.Collections;
using System.Collections.Generic;

public class EspiralSimple : MonoBehaviour
{
    [Header("Sockets donde van las semillas (XR Socket Interactor)")]
    public List<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor> sockets = new List<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();

    [Header("Espiral que se activa al completar")]
    public GameObject espiralFinal;

    [Header("Efectos opcionales")]
    public ParticleSystem efectoFinal;
    public AudioClip sonidoFinal;

    private AudioSource audioSrc;
    private bool completado = false;

    void Start()
    {
        audioSrc = gameObject.AddComponent<AudioSource>();
        StartCoroutine(VerificarSockets());
    }

    IEnumerator VerificarSockets()
    {
        while (!completado)
        {
            bool todosLlenos = true;

            foreach (var s in sockets)
            {
                if (!s.hasSelection)
                {
                    todosLlenos = false;
                    break;
                }
            }

            if (todosLlenos)
            {
                ActivarEspiral();
                completado = true;
            }

            yield return new WaitForSeconds(0.5f);
        }
    }

    void ActivarEspiral()
    {
        if (espiralFinal != null)
        {
            espiralFinal.SetActive(true);
            StartCoroutine(Aparecer());
        }

        if (efectoFinal != null)
            efectoFinal.Play();

        if (sonidoFinal != null)
            audioSrc.PlayOneShot(sonidoFinal);
    }

    IEnumerator Aparecer()
    {
        espiralFinal.transform.localScale = Vector3.zero;
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / 2f;
            espiralFinal.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);
            espiralFinal.transform.Rotate(Vector3.up * 60 * Time.deltaTime);
            yield return null;
        }
    }
}
