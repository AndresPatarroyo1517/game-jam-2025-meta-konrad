using UnityEngine;

public class MainTrailPath : MonoBehaviour
{
    [Header("Configuración")]
    public GameObject trailObject;
    public float pathLength = 50f;
    public float speed = 3f;
    
    [Header("Movimiento Espectacular")]
    public float spiralAmplitude = 3f;      // Amplitud de la espiral
    public float spiralFrequency = 4f;       // Vueltas de la espiral
    public float verticalWave = 2f;          // Movimiento vertical
    public float twistIntensity = 1.5f;      // Intensidad del giro
    
    private Vector3 startPosition;
    private Vector3[] pathPoints;
    private int currentPointIndex = 0;
    private TrailRenderer trailRenderer;
    
    void Start()
    {
        if (trailObject == null)
        {
            Debug.LogError("Trail Object no asignado!");
            return;
        }
        
        startPosition = trailObject.transform.position;
        trailRenderer = trailObject.GetComponent<TrailRenderer>();
        
        GeneratePath();
        MoveToNextPoint();
    }
    
    void GeneratePath()
    {
        int pointCount = 25;
        pathPoints = new Vector3[pointCount];
        
        for (int i = 0; i < pointCount; i++)
        {
            float progress = (float)i / (pointCount - 1);
            float z = startPosition.z + (pathLength * progress);
            
            // Espiral con amplitud creciente
            float angle = progress * Mathf.PI * 2 * spiralFrequency;
            float currentAmplitude = spiralAmplitude * Mathf.Sin(progress * Mathf.PI);
            
            float x = startPosition.x + Mathf.Cos(angle) * currentAmplitude * twistIntensity;
            
            // Movimiento vertical ondulante
            float y = startPosition.y + Mathf.Sin(progress * Mathf.PI * 2) * verticalWave;
            
            pathPoints[i] = new Vector3(x, y, z);
        }
    }
    
    void MoveToNextPoint()
    {
        if (currentPointIndex >= pathPoints.Length)
        {
            // Ocultar el trail antes de reiniciar
            if (trailRenderer != null)
            {
                trailRenderer.Clear();
                trailRenderer.enabled = false;
            }
            
            // Reiniciar posición instantáneamente
            trailObject.transform.position = startPosition;
            currentPointIndex = 0;
            
            // Reactivar el trail
            if (trailRenderer != null)
            {
                trailRenderer.enabled = true;
            }
            
            MoveToNextPoint();
            return;
        }
        
        Vector3 targetPoint = pathPoints[currentPointIndex];
        float distance = Vector3.Distance(trailObject.transform.position, targetPoint);
        float duration = distance / speed;
        
        iTween.MoveTo(trailObject, iTween.Hash(
            "position", targetPoint,
            "time", duration,
            "easetype", iTween.EaseType.linear,
            "oncomplete", "OnPointReached",
            "oncompletetarget", gameObject
        ));
        
        currentPointIndex++;
    }
    
    void OnPointReached()
    {
        MoveToNextPoint();
    }
}