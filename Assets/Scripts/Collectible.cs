using UnityEngine;

public class Collectible : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Check if the object touching the cube is the player
        if (other.CompareTag("Player"))
        {
            // Tell the GameManager we grabbed one, then destroy the cube
            GameManager.instance.CollectibleGrabbed();
            Destroy(gameObject);
        }
    }
}