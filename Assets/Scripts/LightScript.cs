using UnityEngine;

public class LightScript : MonoBehaviour
{
    public GameObject flashlight_ground, inticon, flashlight_player;
    public AudioSource light_sound;

    void OnTriggerStay(Collider other)
    {
        if(other.CompareTag("MainCamera"))
        {
            inticon.SetActive(true);
            if(Input.GetKeyDown(KeyCode.E))
            {
                flashlight_ground.SetActive(false);
                inticon.SetActive(false);
                flashlight_player.SetActive(true);
                light_sound.Play();
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("MainCamera"))
        {
            inticon.SetActive(false);
        }
    }
}
