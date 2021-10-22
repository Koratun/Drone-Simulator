using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Drone : MonoBehaviour
{

    //public int secondsToSimulate;

    public int runs;

    public SaveCamera cam;

    [HideInInspector]
    public int runCounter;

    public int framesToSimulate;

    private int framesCounter;

    private Vector3 direction;

    private float distanceToTravel;

    private float rotationAmount;

    [HideInInspector]
    public string dataPath;

    public PathChecker pathChecker;

    public GameObject spawnParent;

    private Transform[] spawnPoints;

    private bool collided = false;

    // Start is called before the first frame update
    void Start()
    {
        spawnPoints = spawnParent.GetComponentsInChildren<Transform>();

        int i = Random.Range(0, spawnPoints.Length);

        transform.position = spawnPoints[i].position;
        transform.Rotate(0, Random.Range(0, 360), 0);

        pathChecker.transform.position = transform.position;

        dataPath = "D:/OneDrive/Documents/UAT/Production Studio/Deep Sight/Data/";

    }

    //NOTE: 10 in game units is approximately 5cm; 2:1 ratio

    // Update is called once per frame
    void Update()
    {
        //Reset run when necessary
        if(framesCounter >= framesToSimulate)
        {
            framesCounter = 0;
            runCounter++;

            int i = Random.Range(0, spawnPoints.Length);
            transform.position = spawnPoints[i].position;
            transform.Rotate(0, Random.Range(0, 360), 0);

            pathChecker.transform.position = transform.position;

            cam.resetFileCount();
        }


        if(runCounter >= runs)
        {
            UnityEditor.EditorApplication.isPlaying = false;
        }

        //If we are at the start of a run, then create the necessary directories.
        if(!Directory.Exists(dataPath + "Run" + runCounter))
        {
            Directory.CreateDirectory(dataPath + "Run" + runCounter);
            Directory.CreateDirectory(dataPath + "Run" + runCounter + "/photos");
            //Save current Y-pos
            File.WriteAllText(dataPath + "Run" + runCounter + "/yPos.txt", transform.position.y/2 + "\n"); //Divide by 2 to scale game units to cm
        }
        else
        {
            //Save current Y-pos
            File.AppendAllText(dataPath + "Run" + runCounter + "/yPos.txt", transform.position.y/2 + "\n");
        }


        if (distanceToTravel <= 0)
        {
            int collisions = 0;
            do
            {
                pathChecker.transform.position = transform.position;
                collided = false;
                if (collisions > 0)
                {
                    Debug.Log("Collided! " + collisions);
                }

                distanceToTravel = Random.Range(5f * 2, 20f * 2);
                float unitsPerSec = map(distanceToTravel, 5 * 2, 20 * 2, 2, 10); // 1 - 5 cm/s

                direction = Random.onUnitSphere * unitsPerSec; //To travel about 10 in game units every 60 frames or 1 second.
                rotationAmount = Random.Range(-15f, 15f) / distanceToTravel;

                //Move path checking sphere and see if it hits anything. If it does, collision() will be called.
                pathChecker.transform.position += direction.normalized * distanceToTravel;

                collisions++;

                //If the direction and distance we randomly pick intersects with the map
                //OR 
                //if the future position is less than 10 units away from the map mesh
                //THEN we want to randomize it again.
                //mapMesh.meshClose(direction.normalized * distanceToTravel + transform.position, 10);
            } while (Physics.Raycast(transform.position, direction, distanceToTravel + 10) || collided);// || mapMesh.meshClose(direction.normalized * distanceToTravel + transform.position, 10));
        }

        transform.position += direction;
        distanceToTravel -= direction.magnitude;
        transform.Rotate(0, rotationAmount, 0);

        Camera.main.Render();
        framesCounter++;
        
        
    }


    public void collision()
    {
        collided = true;
    }


    private float map(float input, float originalLowerBound, float originalHigherBound, float newLowBound, float newHighBound)
    {
        input = (input - originalLowerBound) / (originalHigherBound - originalLowerBound);
        return input * (newHighBound - newLowBound) + newLowBound;
    }

}
