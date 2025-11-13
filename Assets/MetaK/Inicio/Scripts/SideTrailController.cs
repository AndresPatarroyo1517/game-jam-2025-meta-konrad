using UnityEngine;
using System.Collections;

public class SideTrailsPath : MonoBehaviour
{
    [Header("Trail Objects")]
    public GameObject leftTrail;
    public GameObject middleTrail;
    public GameObject rightTrail;
    
    [Header("Configuración General")]
    public float pathLength = 50f;
    public float speed = 2.5f;
    
    [Header("Patrones de Movimiento")]
    public float leftZigzagAmplitude = 3f;
    public float middleWaveAmplitude = 2.5f;
    public float rightSpiralAmplitude = 3.5f;
    
    private Vector3 leftStartPos;
    private Vector3 middleStartPos;
    private Vector3 rightStartPos;
    
    private TrailRenderer leftTrailRenderer;
    private TrailRenderer middleTrailRenderer;
    private TrailRenderer rightTrailRenderer;
    
    private bool isLeftComplete = false;
    private bool isMiddleComplete = false;
    
    void Start()
    {
        // Guardar posiciones iniciales
        if (leftTrail)
        {
            leftStartPos = leftTrail.transform.position;
            leftTrailRenderer = leftTrail.GetComponent<TrailRenderer>();
        }
        if (middleTrail)
        {
            middleStartPos = middleTrail.transform.position;
            middleTrailRenderer = middleTrail.GetComponent<TrailRenderer>();
        }
        if (rightTrail)
        {
            rightStartPos = rightTrail.transform.position;
            rightTrailRenderer = rightTrail.GetComponent<TrailRenderer>();
        }
        
        // Iniciar secuencia: primero izquierdo
        AnimateLeftTrail();
    }
    
    // ============= CAMINO IZQUIERDO (Zigzag) =============
    void AnimateLeftTrail()
    {
        if (leftTrail == null) return;
        
        int pointCount = 15;
        Vector3[] pathPoints = new Vector3[pointCount];
        
        // Rotación de -55 grados en Y
        Quaternion rotation = Quaternion.Euler(0, -55, 0);
        
        for (int i = 0; i < pointCount; i++)
        {
            float progress = (float)i / (pointCount - 1);
            float forwardDist = pathLength * progress;
            
            // Zigzag: alterna izquierda-derecha con suavizado
            float sideways = Mathf.Sin(i * 0.8f) * leftZigzagAmplitude;
            
            Vector3 localPos = new Vector3(sideways, 0, forwardDist);
            Vector3 worldPos = leftStartPos + rotation * localPos;
            
            pathPoints[i] = worldPos;
        }
        
        iTween.MoveTo(leftTrail, iTween.Hash(
            "path", pathPoints,
            "time", pathLength / speed,
            "easetype", iTween.EaseType.linear,
            "oncomplete", "OnLeftComplete",
            "oncompletetarget", gameObject
        ));
    }
    
    void OnLeftComplete()
    {
        isLeftComplete = true;
        
        // Ocultar trail y reiniciar posición
        if (leftTrailRenderer != null)
        {
            leftTrailRenderer.Clear();
            leftTrailRenderer.enabled = false;
        }
        leftTrail.transform.position = leftStartPos;
        if (leftTrailRenderer != null)
        {
            leftTrailRenderer.enabled = true;
        }
        
        // Iniciar el camino del medio
        AnimateMiddleTrail();
    }
    
    // ============= CAMINO MEDIO (Ondulante Suave) =============
    void AnimateMiddleTrail()
    {
        if (middleTrail == null) return;
        
        int pointCount = 20;
        Vector3[] pathPoints = new Vector3[pointCount];
        
        for (int i = 0; i < pointCount; i++)
        {
            float progress = (float)i / (pointCount - 1);
            float z = middleStartPos.z + (pathLength * progress);
            
            // Onda sinusoidal doble para más dinamismo
            float x = middleStartPos.x + 
                     (Mathf.Sin(progress * Mathf.PI * 3) * middleWaveAmplitude) +
                     (Mathf.Cos(progress * Mathf.PI * 5) * middleWaveAmplitude * 0.5f);
            
            pathPoints[i] = new Vector3(x, middleStartPos.y, z);
        }
        
        iTween.MoveTo(middleTrail, iTween.Hash(
            "path", pathPoints,
            "time", pathLength / speed,
            "easetype", iTween.EaseType.linear,
            "oncomplete", "OnMiddleComplete",
            "oncompletetarget", gameObject
        ));
    }
    
    void OnMiddleComplete()
    {
        isMiddleComplete = true;
        
        // Ocultar trail y reiniciar posición
        if (middleTrailRenderer != null)
        {
            middleTrailRenderer.Clear();
            middleTrailRenderer.enabled = false;
        }
        middleTrail.transform.position = middleStartPos;
        if (middleTrailRenderer != null)
        {
            middleTrailRenderer.enabled = true;
        }
        
        // Iniciar el camino derecho
        AnimateRightTrail();
    }
    
    // ============= CAMINO DERECHO (Serpenteo) =============
    void AnimateRightTrail()
    {
        if (rightTrail == null) return;
        
        int pointCount = 18;
        Vector3[] pathPoints = new Vector3[pointCount];
        
        // Rotación de +55 grados en Y
        Quaternion rotation = Quaternion.Euler(0, 55, 0);
        
        for (int i = 0; i < pointCount; i++)
        {
            float progress = (float)i / (pointCount - 1);
            float forwardDist = pathLength * progress;
            
            // Serpenteo tipo "S" intenso
            float sideways = Mathf.Sin(progress * Mathf.PI * 4.5f) * rightSpiralAmplitude * 
                            (0.5f + progress * 0.5f);
            
            Vector3 localPos = new Vector3(sideways, 0, forwardDist);
            Vector3 worldPos = rightStartPos + rotation * localPos;
            
            pathPoints[i] = worldPos;
        }
        
        iTween.MoveTo(rightTrail, iTween.Hash(
            "path", pathPoints,
            "time", pathLength / speed,
            "easetype", iTween.EaseType.linear,
            "oncomplete", "OnRightComplete",
            "oncompletetarget", gameObject
        ));
    }
    
    void OnRightComplete()
    {
        // Ocultar trail y reiniciar posición
        if (rightTrailRenderer != null)
        {
            rightTrailRenderer.Clear();
            rightTrailRenderer.enabled = false;
        }
        rightTrail.transform.position = rightStartPos;
        if (rightTrailRenderer != null)
        {
            rightTrailRenderer.enabled = true;
        }
        
        // Reiniciar el ciclo completo desde el izquierdo
        isLeftComplete = false;
        isMiddleComplete = false;
        AnimateLeftTrail();
    }
}