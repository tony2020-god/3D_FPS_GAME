using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("子彈的傷害值")]
    public float attack;

    private void OnCollisionEnter(Collision collision)
    {
        Destroy(collision.gameObject);
    }

}
