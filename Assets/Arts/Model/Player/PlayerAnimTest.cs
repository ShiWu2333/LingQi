using UnityEngine;

public class PlayerAnimTest : MonoBehaviour
{
    Animator anim;

    void Start()
    {
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        float speed = new Vector2(h, v).magnitude;
        anim.SetFloat("Speed", speed);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            anim.SetTrigger("Dodge");
        }
    }
}
