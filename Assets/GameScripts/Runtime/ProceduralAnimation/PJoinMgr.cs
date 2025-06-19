using System.Reflection;
using UnityEngine;

public class PJoinMgr : MonoBehaviour
{
    public PJoin[] PJoins;

    public PJoin Controller;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
      
    }

    // Update is called once per frame
    void Update()
    {
        float acceleration = 1.0f;
        if (Input.GetKey(KeyCode.Space))
        {
            acceleration = 2.0f;
        }
        // if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.S)
        //                             || Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.W))
        // {
        //     Vector2 inputDirection = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        //     
        //     Controller.transform.forward += new Vector3(inputDirection.x, 0, inputDirection.y).normalized;
        //     
        //     Controller.transform.position += Controller.transform.forward * 0.1f;
        //     Controller.Calculate();
        //     for (int i = 0; i < PJoins.Length; i++)
        //     {
        //         PJoins[i].Calculate();
        //     }
        // }
        if (Input.GetKey(KeyCode.W))
        {
            // Vector3 forward = Controller.transform.forward;
            // Controller.transform.position += forward * Time.deltaTime;
            // Controller.Calculate();
            Controller.ForwardMove();
            for (int i = 0; i < PJoins.Length; i++)
            {
                PJoins[i].Calculate();
            }
        }
        
        if (Input.GetKey(KeyCode.Q))
        {
            Controller.transform.Rotate(Vector3.up, 0.5f * acceleration);
            Controller.Calculate();
            for (int i = 0; i < PJoins.Length; i++)
            {
                PJoins[i].Calculate();
            }
        }

        if (Input.GetKey(KeyCode.E))
        {
            Controller.transform.Rotate(Vector3.up, -0.5f * acceleration);
            Controller.Calculate();
            for (int i = 0; i < PJoins.Length; i++)
            {
                PJoins[i].Calculate();
            }
        }
    }
}
