using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public GameObject mainCam;
    public GameObject topdownCam;

    [Range(10f, 100f)]
    public float jumpForce;
    [Range(0.05f, 1.0f)]
    public float speed;

    public float maxYRotation = 80;
    public float minYRotation = -80;

    private float rotationY = 0;
    void Update()
    {
        float rotationX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * 10;

        rotationY += Input.GetAxis("Mouse Y") * 10;
        rotationY = Mathf.Clamp (rotationY, minYRotation, maxYRotation);

        transform.localEulerAngles = new Vector3(-rotationY, rotationX, 0);

        if(Input.GetKey(KeyCode.W))
        {
            transform.position += Quaternion.Euler(0, rotationX, 0) * Vector3.forward*speed;
        }
        if(Input.GetKey(KeyCode.S))
        {
            transform.position += Quaternion.Euler(0, rotationX, 0) * Vector3.back*speed;
        }
        if(Input.GetKey(KeyCode.A))
        {
            transform.position += Quaternion.Euler(0, rotationX, 0) * Vector3.left*speed;
        }
        if(Input.GetKey(KeyCode.D))
        {
            transform.position += Quaternion.Euler(0, rotationX, 0) * Vector3.right*speed;
        }
        if(Input.GetKey(KeyCode.Q))
        {
            transform.position += Vector3.up*speed;
        }
        if(Input.GetKey(KeyCode.E))
        {
            transform.position += Vector3.down*speed;
        }
        
        if(Input.GetKeyDown(KeyCode.Space))
        {
            GetComponent<Rigidbody>().AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            GetComponent<Rigidbody>().useGravity = true;
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            mainCam.SetActive(false);
            topdownCam.SetActive(true);
        }
        if(Input.GetKeyUp(KeyCode.C))
        {
            mainCam.SetActive(true);
            topdownCam.SetActive(false);
        }
    }
}
