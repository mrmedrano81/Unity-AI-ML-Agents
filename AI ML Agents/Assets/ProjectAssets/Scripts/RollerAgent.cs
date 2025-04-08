using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class RollerAgent : Agent
{
    private Rigidbody rBody;
    public Transform target;
    public Vector2 floorDimensions;
    public float fallingY = -0.6f;
    public float minDistance = 1.42f;
    public float forceMultiplier = 10f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rBody = GetComponent<Rigidbody>();
    }

    public override void OnEpisodeBegin()
    {
        if (transform.localPosition.y < fallingY)
        {
            // If the Agent fell, zero its momentum
            rBody.angularVelocity = Vector3.zero;
            rBody.linearVelocity = Vector3.zero;
            transform.localPosition = new Vector3(0, 0.6f, 0);
        }

        // Spawn target near the edges more often
        target.localPosition = new Vector3(
            GetEdgeBiasedRandom(floorDimensions.x / 2),
            0.9f,
            GetEdgeBiasedRandom(floorDimensions.y / 2)
        );
    }

    // Returns a random number between -range and range, biased toward the edges
    private float GetEdgeBiasedRandom(float range)
    {
        // Random.value gives 0 to 1. Subtract 0.5 to center it around 0
        float raw = Random.value - 0.5f;

        // Bias away from center using exponential growth (sharper = more edge-biased)
        float edgeBias = Mathf.Sign(raw) * Mathf.Pow(Mathf.Abs(raw) * 2, 1.7f); // Exponent > 1 for edge bias

        return edgeBias * range;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Target and Agent positions
        sensor.AddObservation(target.localPosition);
        sensor.AddObservation(transform.localPosition);

        // Agent velocity
        sensor.AddObservation(rBody.linearVelocity.x);
        sensor.AddObservation(rBody.linearVelocity.z);
    }


    public override void OnActionReceived(ActionBuffers actions)
    {
        Vector3 controlSignal = Vector3.zero;
        controlSignal.x = actions.ContinuousActions[0];
        controlSignal.z = actions.ContinuousActions[1];
        rBody.AddForce(controlSignal * forceMultiplier);

        float distanceToTarget = Vector3.Distance(transform.localPosition, target.localPosition);

        if (distanceToTarget < minDistance)
        {
            SetReward(1.0f);
            EndEpisode();
        }
        //
        else if (transform.localPosition.y < fallingY)
        {
            EndEpisode();
        }
    }

    void OnDrawGizmos()
    {
        DrawFloorBoundsGizmos();
    }

    void DrawFloorBoundsGizmos()
    {
        Gizmos.color = Color.green;

        // Use the agent's parent transform (e.g., platform or environment center), or Vector3.zero if none
        Vector3 center = transform.parent ? transform.parent.position + Vector3.up: Vector3.zero;
        float halfWidth = floorDimensions.x / 2f;
        float halfHeight = floorDimensions.y / 2f;

        // Draw the bounding box as a wireframe square on the XZ plane at y = 0
        Vector3 bottomLeft = center + new Vector3(-halfWidth, 0, -halfHeight);
        Vector3 topLeft = center + new Vector3(-halfWidth, 0, halfHeight);
        Vector3 topRight = center + new Vector3(halfWidth, 0, halfHeight);
        Vector3 bottomRight = center + new Vector3(halfWidth, 0, -halfHeight);

        Gizmos.DrawLine(bottomLeft, topLeft);
        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
    }

}
