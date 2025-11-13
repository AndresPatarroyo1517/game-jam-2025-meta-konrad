using UnityEngine;
using System.Collections;

public class SideTrailsController : MonoBehaviour
{
    [Header("Estelas Secundarias")]
    public GameObject trailLeft;
    public GameObject trailCenter;
    public GameObject trailRight;

    [Header("Configuración General")]
    public float moveTime = 4f;
    public float pathLength = 10f;
    public float waitBetween = 1.2f;

    private Vector3 origin;

    void Start()
    {
        origin = transform.position;
        StartCoroutine(SequentialTrails());
    }

    IEnumerator SequentialTrails()
    {
        while (true)
        {
            if (trailLeft) yield return StartCoroutine(MoveTrail(trailLeft, -55f));
            yield return new WaitForSeconds(waitBetween);

            if (trailCenter) yield return StartCoroutine(MoveTrail(trailCenter, 0f));
            yield return new WaitForSeconds(waitBetween);

            if (trailRight) yield return StartCoroutine(MoveTrail(trailRight, 55f));
            yield return new WaitForSeconds(waitBetween);
        }
    }

    IEnumerator MoveTrail(GameObject trail, float yAngle)
    {
        Vector3 startPos = origin;
        Quaternion rot = Quaternion.Euler(0, yAngle, 0);
        Vector3 dir = rot * Vector3.forward;

        Vector3[] path = new Vector3[]
        {
            startPos,
            startPos + dir * (pathLength * 0.3f) + transform.up * 0.1f,
            startPos + dir * (pathLength * 0.6f) + transform.up * 0.15f,
            startPos + dir * pathLength
        };

        iTween.MoveTo(trail, iTween.Hash(
            "path", path,
            "time", moveTime,
            "easetype", iTween.EaseType.easeInOutSine,
            "looptype", iTween.LoopType.none
        ));

        yield return new WaitForSeconds(moveTime + 0.1f);
        trail.transform.position = startPos;
    }
}
