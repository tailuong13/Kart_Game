using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MysteryBox : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float rotationSpeed = 100f; 

    [Header("Debug")]
    public bool enableDebug = true; 

    private void Update()
    {
        RotateBox();
    }

    private void RotateBox()
    {
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
    }

    private void OnTriggerEnter(Collider other)
    {
       
        if (other.CompareTag("Player")) 
        {
            if (enableDebug)
            {
                Debug.Log("Mystery Box touched by the player!");
            }
            
            HandlePlayerCollision();
        }
    }

    private void HandlePlayerCollision()
    {
        Debug.Log("Mystery Box effect activated!");
        
        gameObject.SetActive(false);
    }

    private void RespawnBox()
    {
        gameObject.SetActive(true);
    }
}
