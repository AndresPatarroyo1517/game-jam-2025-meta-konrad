using UnityEngine;
using System.Collections;

public class MainTrailController : MonoBehaviour
{
    [Header("Estela Principal")]
    public GameObject trailMain;

    [Header("Configuración")]
    public float length = 22f;
    public float moveTime = 6f;

    private Vector3 origin;

    void Start()
    {
        origin = transform.position;
        StartCoroutine(LoopMainTrail());
    }

    IEnumerator LoopMainTrail()
    {
        while (true)
        {
            Vector3[] path = new Vector3[]
            {
                origin,
                origin + transform.forward * (length * 0.3f),
                origin + transform.forward * (length * 0.6f) + transform.up * 0.1f,
                origin + transform.forward * length
            };

            iTween.MoveTo(trailMain, iTween.Hash(
                "path", path,
                "time", moveTime,
                "easetype", iTween.EaseType.easeInOutSine,
                "looptype", iTween.LoopType.none
            ));

            yield return new WaitForSeconds(moveTime + 0.2f);
            trailMain.transform.position = origin;
        }
    }
}
