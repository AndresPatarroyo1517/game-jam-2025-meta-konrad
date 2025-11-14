using UnityEngine;

public class MainTrailPath : MonoBehaviour
{
    [Header("Configuración")]
    public GameObject trailObject;
    public float pathLength = 50f;
    public float speed = 3f;
    
    [Header("Movimiento Espectacular")]
    public float spiralAmplitude = 3f;
    public float spiralFrequency = 4f;
    public float verticalWave = 2f;
    public float twistIntensity = 1.5f;
    
    private Vector3 startPosition;
    private Quaternion startRotation;
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
        startRotation = trailObject.transform.rotation;
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
            
            // Crea puntos en ESPACIO LOCAL (como si mirara hacia adelante en Z)
            float localZ = pathLength * progress;
            
            // Espiral con amplitud creciente
            float angle = progress * Mathf.PI * 2 * spiralFrequency;
            float currentAmplitude = spiralAmplitude * Mathf.Sin(progress * Mathf.PI);
            
            float localX = Mathf.Cos(angle) * currentAmplitude * twistIntensity;
            
            // Movimiento vertical ondulante
            float localY = Mathf.Sin(progress * Mathf.PI * 2) * verticalWave;
            
            Vector3 localPoint = new Vector3(localX, localY, localZ);
            
            // CONVIERTE de espacio local a espacio mundial usando la rotación inicial
            pathPoints[i] = startPosition + startRotation * localPoint;
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
            
            // Reiniciar posición y rotación instantáneamente
            trailObject.transform.position = startPosition;
            trailObject.transform.rotation = startRotation;
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
    
    // Método para regenerar el path si cambias la rotación en el editor
    void OnValidate()
    {
        if (Application.isPlaying && trailObject != null)
        {
            startRotation = trailObject.transform.rotation;
            GeneratePath();
        }
    }
}