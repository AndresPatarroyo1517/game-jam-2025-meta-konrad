using UnityEngine;

public class Carpet : MonoBehaviour
{
    public GameManager gameManager;

    private void OnTriggerEnter(Collider other)
    {
        // Verifica si el objeto que entró es el jugador (puedes usar tags o layers)
        if (other.CompareTag("Player"))
        {
            gameManager.IniciarJuego();

            Debug.Log("Colisiona");
            // Opcional: Desactivar el collider del tapete para que no se active repetidamente
            GetComponent<Collider>().enabled = false;
        }
    }
}
