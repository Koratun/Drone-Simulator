using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathChecker : MonoBehaviour
{

    public Drone droneController;

    //Calls the drone controller and tells it a collision occurred.
    private void OnTriggerEnter(Collider other)
    {
        droneController.collision();
    }

}
