using UnityEngine;

public class KeyScript : MonoBehaviour
{
    public GameObject inticon, key, sign1, sign2;
    public AudioSource keys;

    void OnTriggerStay(Collider other)
    {
        if(other.CompareTag("MainCamera"))
        {
            inticon.SetActive(true);
            if(Input.GetKeyDown(KeyCode.E))
            {
                key.SetActive(false);
                Door.keyfound = true;
                inticon.SetActive(false);
                sign1.SetActive(true);
                sign2.SetActive(true);
                keys.Play();
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
