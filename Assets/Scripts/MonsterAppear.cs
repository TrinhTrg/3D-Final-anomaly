using UnityEngine;

public class MonsterAppear : MonoBehaviour
{
    public GameObject monster;
    public Collider collision1;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            monster.SetActive(true);
            collision1.enabled = false;
        }
    }
}
