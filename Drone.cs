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

    private float distanceForCycle;

    private Vector3 maxVelocity;

    private Vector3 velocity;

    private bool cycleUp = true;

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
            //Save current positional data
            //Divide by 2 to scale game units to cm
            File.WriteAllText(dataPath + "Run" + runCounter + "/xPos.txt", transform.position.x / 2 + "\n");
            File.WriteAllText(dataPath + "Run" + runCounter + "/yPos.txt", transform.position.y / 2 + "\n"); 
            File.WriteAllText(dataPath + "Run" + runCounter + "/zPos.txt", transform.position.z / 2 + "\n");
            //Save the rotational data.
            Vector3 rotation = transform.rotation.eulerAngles;
            File.WriteAllText(dataPath + "Run" + runCounter + "/rotation.txt", rotation.x + ", " + rotation.y + ", " + rotation.z + "\n");
        }
        else
        {
            //Save current positional data
            File.AppendAllText(dataPath + "Run" + runCounter + "/xPos.txt", transform.position.x / 2 + "\n");
            File.AppendAllText(dataPath + "Run" + runCounter + "/yPos.txt", transform.position.y / 2 + "\n");
            File.AppendAllText(dataPath + "Run" + runCounter + "/zPos.txt", transform.position.z / 2 + "\n");
            //Save the rotational data.
            Vector3 rotation = transform.rotation.eulerAngles;
            File.AppendAllText(dataPath + "Run" + runCounter + "/rotation.txt", rotation.x + ", " + rotation.y + ", " + rotation.z + "\n");
        }


        if (distanceToTravel <= 0)
        {
            Debug.Log("Frame: " + framesCounter);
            do
            {
                pathChecker.transform.position = transform.position;
                collided = false;

                distanceToTravel = Random.Range(5f * 2, 20f * 2);
                float unitsPerSec = map(distanceToTravel, 5 * 2, 20 * 2, 2.0f/60, 10.0f/60); // 1 - 5 cm/s (2 - 10 game units / 60 frames)

                direction = Random.onUnitSphere * unitsPerSec; //To travel about 10 in game units every 60 frames or 1 second.
                rotationAmount = Random.Range(-15f, 15f) / distanceToTravel;

                //Move path checking sphere and see if it hits anything. If it does, collision() will be called.
                pathChecker.transform.position += direction.normalized * distanceToTravel;

                //If the direction and distance we randomly pick intersects with the map
                //OR 
                //if the future position is less than 10 units away from the map mesh
                //THEN we want to randomize it again.
                //mapMesh.meshClose(direction.normalized * distanceToTravel + transform.position, 10);
            } while (Physics.Raycast(transform.position, direction, distanceToTravel + 10) || collided);// || mapMesh.meshClose(direction.normalized * distanceToTravel + transform.position, 10));
            maxVelocity = direction;
            distanceForCycle = distanceToTravel;
        }

        //IF NOT at max velocity for given distance, THEN
        //  Accelerate proportional to max velocity
        //  Rotate drone based on acceleration applied
        //ELSE IF closer than distance needed to decelerate to 0, THEN
        //  Accelerate proportional to max velocity in the opposite direction
        //  Rotate drone based on acceleration applied.
        //Else do nothing

        // accel = maxVelocity^2 / (distance * .2 * 2)
        Vector3 accel = new Vector3(maxVelocity.x * maxVelocity.x, maxVelocity.y * maxVelocity.y, maxVelocity.z * maxVelocity.z) / (distanceForCycle * 0.4f);

        //If we are in the first 20% of the travel cycle, and the velocity still needs to accelerate, then do so
        if (distanceToTravel > distanceForCycle * .8 && velocity.magnitude < maxVelocity.magnitude)
        {
            velocity += accel;
        }//If we are about to enter the last 20% of the travel cycle, then lower the velocity
        else if (distanceToTravel - velocity.magnitude <= distanceToTravel * .2 && velocity.magnitude > 0)
        {
            //If there is a bit of error (velocity should never be less than acceleration in theory,
            //but in practice it happens frequently) then round to zero.
            if (velocity.magnitude <= accel.magnitude)
            {
                velocity = Vector3.zero;
            }
            else
            {
                velocity -= accel;
            }
        }

        if(velocity == Vector3.zero && distanceToTravel > 0)
        {
            Debug.Log("Warning! Velocity is 0 and we had " + distanceToTravel + " left!");
            distanceToTravel = 0;
        }

        //Implement tilting of drone to simulate real movement.


        transform.position += velocity;
        distanceToTravel -= velocity.magnitude;
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
