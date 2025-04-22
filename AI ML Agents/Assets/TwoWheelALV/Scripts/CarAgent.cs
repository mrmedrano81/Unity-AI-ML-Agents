using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

public class CarAgent : Agent
{
    [Header("Car Properties")]
    public float motorTorque = 2000f;
    public float brakeTorque = 2000f;
    public float maxSpeed = 20f;
    public float steeringRange = 30f;
    public float steeringRangeAtMaxSpeed = 10f;
    public float centreOfGravityOffset = -1f;

    [Header("Floor Properties")]
    public Vector2 floorDimensions = new Vector2(100, 100);
    public Vector2 outOfBoundsDimensions = new Vector2(100, 100);
    public float floorDebugOffsetY = 2f;
    public LayerMask groundMask;

    private WheelControl[] wheels;
    [HideInInspector] public Rigidbody rigidBody;

    private Vector3 centerOfGravity = Vector3.zero;

    public float fallingY = -0.6f;
    public float minDistance = 1.42f;

    [Header("Reward Parameters")]
    public GameObject target;
    public float maxTimeToReachTarget = 30f;
    public float rewardForReachingTarget = 1f;
    public float rewardMultiplierForSpeed = 1f;

    [Header("DEBUG")]
    public bool toggleDebugMode = false;
    public float VelocityMagnitude = 0f;
    public float fastestTimeToReachTarget = -1f;
    public float timeToTarget = 0f;
    public float closestDistanceToTarget = 0f;
    private float startingDistanceToTarget;
    private Vector3 previousPosition = Vector3.zero;

    private float targetRadius;

    float hIn;
    float vIn;

    const string MouseXInput = "Mouse X";
    private const string MouseYInput = "Mouse Y";
    private const string MouseScrollInput = "Mouse ScrollWheel";

    public CameraManager cameraManager;
    public UIDebug uiDebug;

    // Start is called before the first frame update
    void Start()
    {
        rigidBody = GetComponent<Rigidbody>();

        // Adjust center of mass to improve stability and prevent rolling
        Vector3 centerOfMass = rigidBody.centerOfMass;
        centerOfMass.y += centreOfGravityOffset;
        rigidBody.centerOfMass = centerOfMass;

        // Get all wheel components attached to the car
        wheels = GetComponentsInChildren<WheelControl>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        targetRadius = target.GetComponent<SphereCollider>().radius*1.5f;

        if (toggleDebugMode)
        {
            cameraManager?.SetFollowTransform(transform);
            uiDebug = FindFirstObjectByType<UIDebug>();
        }
    }

    private void Update()
    {
        centerOfGravity = rigidBody.centerOfMass;
    }

    private void FixedUpdate()
    {
        VelocityMagnitude = rigidBody.linearVelocity.magnitude;
    }

    private void LateUpdate()
    {

        if (toggleDebugMode)
        {
            float rawMouseX = Input.GetAxisRaw(MouseXInput);
            float rawMouseY = Input.GetAxisRaw(MouseYInput);

            Vector3 rotation = new Vector3(rawMouseX, rawMouseY, 0);

            float scrollInput = Input.GetAxis(MouseScrollInput);

            cameraManager.UpdateWithInput(Time.deltaTime, scrollInput, rotation);
        }
    }


    public override void OnEpisodeBegin()
    {
        timeToTarget = 0f;
        closestDistanceToTarget = Vector3.Distance(transform.localPosition, target.transform.localPosition);
        previousPosition = transform.localPosition;
        startingDistanceToTarget = closestDistanceToTarget;


        // If the Agent fell, zero its momentum

        if (transform.localPosition.y < fallingY || OutsideFloorBounds())
        {
            rigidBody.linearVelocity = Vector3.zero;
            rigidBody.angularVelocity = Vector3.zero;
            transform.localPosition = new Vector3(0, 1.5f, 0);

            transform.localRotation = Quaternion.Euler(0, Random.Range(-180, 180), 0);
        }



        Vector3 targetSpawnPoint = new Vector3(
            Random.Range(-floorDimensions.x / 2, floorDimensions.x / 2),
            30,
            Random.Range(-floorDimensions.y / 2, floorDimensions.y / 2)
            );

        // Spawn target near the edges more often
        target.transform.localPosition = new Vector3(
            targetSpawnPoint.x,
            GetTargetSpawnHeight(),
            targetSpawnPoint.z
            );
    }

    private float GetTargetSpawnHeight()
    {
        Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, Mathf.Infinity);

        Vector3 spawnPosition = hit.point + Vector3.up * targetRadius;

        return spawnPosition.y;
    }


    public override void CollectObservations(VectorSensor sensor)
    {
        // Target and Agent positions
        sensor.AddObservation((target.transform.localPosition - transform.localPosition).normalized);
        sensor.AddObservation(Vector3.Distance(transform.localPosition, target.transform.localPosition));

        // Agent velocity
        sensor.AddObservation(transform.rotation);
        sensor.AddObservation(rigidBody.linearVelocity);
        sensor.AddObservation(rigidBody.linearVelocity.magnitude);
        sensor.AddObservation(rigidBody.angularVelocity);
        sensor.AddObservation(transform.forward);

        sensor.AddObservation(hIn);
        sensor.AddObservation(vIn);

        sensor.AddObservation(IsAccelerating);
    }

    //public override void Heuristic(in ActionBuffers actionsOut)
    //{
    //    // Get the action buffer for continuous actions
    //    var continuousActions = actionsOut.ContinuousActions;

    //    // Set the values for forward/backward and steering inputs
    //    continuousActions[1] = Input.GetAxis("Vertical"); // Forward/backward input
    //    continuousActions[0] = Input.GetAxis("Horizontal"); // Steering input
    //}

    public override void OnActionReceived(ActionBuffers actions)
    {
        hIn = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f); // Steering
        vIn = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f); // Acceleration/Brake

        HandleMovement(vIn, hIn);

        float currentDistanceToTarget = Vector3.Distance(transform.localPosition, target.transform.localPosition);
        timeToTarget += Time.deltaTime;

        // === Directional Reward ===
        Vector3 toTarget = (target.transform.localPosition - transform.localPosition);
        toTarget.y = 0;

        Vector3 horizontalVelocity = rigidBody.linearVelocity;
        horizontalVelocity.y = 0;


        float directionAlignment = Vector3.Dot(horizontalVelocity.normalized, toTarget.normalized);
        float turnSpeed = Mathf.Abs(rigidBody.angularVelocity.y);

        if (toggleDebugMode)
        {
            Debug.Log("Turnspeed: " + turnSpeed);
            Debug.Log("Direction Alignment: " + directionAlignment);
        }


        float facingAlignment = Vector3.Dot(transform.forward, toTarget.normalized);

        AddReward(0.01f * rigidBody.linearVelocity.magnitude * (directionAlignment)); // Encourage facing the target
        AddReward(0.001f * facingAlignment);


        // Avoid division by zero with a small epsilon
        float minTurnSpeed = 0.01f;
        float turnFactor = turnSpeed < minTurnSpeed ? 1f : Mathf.Clamp(1f / turnSpeed, 0f, 1f);

        // Penalize moving away from target, with higher penalty for fast turning
        // Use Mathf.Abs(directionAlignment) to make penalty proportional to misalignment

        if (turnSpeed >= 0.01f)
        {
            AddReward(-0.01f * (turnSpeed * Mathf.Abs(hIn)));
        }

        // prevent idling
        if (rigidBody.linearVelocity.magnitude > 5f)
        {
            AddReward(0.01f); // Reward for keeping speed
        }
        else
        {
            AddReward(-0.01f); // Penalize idling
        }


        // === Distance Improvement ===

        if (currentDistanceToTarget < closestDistanceToTarget)
        {
            // Normalize the improvement relative to starting distance for consistent scale
            float normalizedImprovement = (closestDistanceToTarget - currentDistanceToTarget) / startingDistanceToTarget;

            // Scale reward based on how significant this improvement is
            AddReward(0.1f * normalizedImprovement);

            // Check if the agent has moved
            Vector3 moveDirection = transform.localPosition - previousPosition;
            moveDirection.y = 0; // Ignore vertical movement
            float positionDelta = moveDirection.magnitude;

            //if (positionDelta > 0.001f) // Only if there's meaningful movement
            //{
            //    // Reward/penalize based on movement direction relative to target
            //    float directionDot = Vector3.Dot(moveDirection.normalized, toTarget.normalized);

            //    // This will be positive when moving toward target, negative when moving away
            //    AddReward(0.1f * directionDot * positionDelta);
            //}


            // Adjust the scale_factor to control the rate of decay.
            // Smaller values mean the reward drops off faster with distance.
            float scale_factor = 10.0f; // Example value, needs tuning
            float exponentialReward = Mathf.Exp(-currentDistanceToTarget / scale_factor);

            // Adjust the reward_strength to control the overall magnitude of the reward
            float reward_strength = 0.1f; // Example value, needs tuning

            AddReward(reward_strength * exponentialReward);

            // Update nearest distance
            closestDistanceToTarget = currentDistanceToTarget;
        }
        else
        {
            //// Check if the agent has moved
            //Vector3 moveDirection = transform.localPosition - previousPosition;
            //moveDirection.y = 0; // Ignore vertical movement
            //float positionDelta = moveDirection.magnitude;

            //if (positionDelta > 0.001f) // Only if there's meaningful movement
            //{
            //    // Reward/penalize based on movement direction relative to target
            //    float directionDot = Vector3.Dot(moveDirection.normalized, toTarget.normalized);

            //    AddReward(0.01f * directionDot * positionDelta);
            //}

            // Small penalty for not improving distance
            AddReward(-0.001f);
        }

        // Add a separate overall progress reward independent of distance improvement
        //float overallProgressRatio = 1f - (currentDistanceToTarget / startingDistanceToTarget);

        //AddReward(0.001f * overallProgressRatio);

        previousPosition = transform.localPosition;

        // === Punishment for being stuck too long ===
        //if (timeToTarget >= maxTimeToReachTarget)
        //{
        //    AddReward(-0.001f); // Timeout penalty
        //    //EndEpisode();
        //}

        // === Fall or Out of Bounds ===
        if (transform.localPosition.y < fallingY || OutsideFloorBounds())
        {
            AddReward(-5f);
            EndEpisode();
            return;
        }

        // === Target Reached ===
        if (currentDistanceToTarget < minDistance)
        {
            float speedBonus = Mathf.Clamp01(1 - timeToTarget / maxTimeToReachTarget);
            float totalReward = rewardForReachingTarget + (speedBonus * rewardMultiplierForSpeed);
            AddReward(totalReward);

            if (fastestTimeToReachTarget < 0 || timeToTarget < fastestTimeToReachTarget)
            {
                fastestTimeToReachTarget = timeToTarget;
            }

            EndEpisode();
            return;
        }

        AddReward(-0.001f);
    }


    //public override void OnActionReceived(ActionBuffers actions)
    //{
    //    hIn = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f); // Forward/backward input
    //    vIn = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);
    //    //hIn = Input.GetAxis("Vertical"); // Forward/backward input
    //    //vIn = Input.GetAxis("Horizontal");

    //    HandleMovement(vIn, hIn);

    //    // Incremental reward for getting nearer
    //    float currentDistanceToTarget = Vector3.Distance(transform.localPosition, target.transform.localPosition);

    //    if (currentDistanceToTarget < nearestDistanceToTarget)
    //    {
    //        float distanceReward = Mathf.Clamp((startingDistanceToTarget - currentDistanceToTarget) / startingDistanceToTarget, 0, 1);
    //        AddReward(0.0002f * distanceReward);
    //        nearestDistanceToTarget = currentDistanceToTarget;
    //    }

    //    if (currentDistanceToTarget >= nearestDistanceToTarget)
    //    {
    //        float distanceReward = -Mathf.Clamp((currentDistanceToTarget - nearestDistanceToTarget) / currentDistanceToTarget, 0, 1);
    //        AddReward(0.0001f * distanceReward);
    //    }

    //    timeToTarget += Time.deltaTime;


    //    // Punishment for going too slow
    //    if (timeToTarget >= maxTimeToReachTarget)
    //    {
    //        AddReward(-0.0001f);
    //    }

    //    //// Punishment for accelerating too often
    //    //if (IsAccelerating && rigidBody.linearVelocity.magnitude < 5f)
    //    //{
    //    //    AddReward(-0.01f);
    //    //}

    //    float targetDirDotproduct = Vector3.Dot(Vector3.ProjectOnPlane(rigidBody.linearVelocity, transform.up).normalized,
    //                                   Vector3.ProjectOnPlane((target.transform.localPosition - transform.localPosition), transform.up).normalized);
    //    //float targetDirDotproduct = Vector3.Dot(rigidBody.linearVelocity, 
    //    //                               Vector3.ProjectOnPlane((target.transform.localPosition - transform.localPosition), transform.up).normalized);

    //    float forwardVelocityDotProduct = Vector3.Dot(rigidBody.linearVelocity.normalized, transform.forward);
    //    bool isFacingForwardWhileMoving = Mathf.Sign(forwardVelocityDotProduct) == 1;

    //    if (toggleDebugMode)
    //    {
    //        Debug.Log("Target Direction Dot Product: " + targetDirDotproduct);
    //        Debug.Log("forwardVelocityDotProduct: " + forwardVelocityDotProduct);
    //    }

    //    //If direction of movement is towards the target, reward
    //    if (targetDirDotproduct > 0.8f)
    //    {
    //        if (isFacingForwardWhileMoving)
    //        {
    //            AddReward(0.0002f * targetDirDotproduct);
    //        }
    //        else
    //        {
    //            AddReward(0.0001f * targetDirDotproduct);
    //        }
    //    }


    //    // Reward for reaching the target
    //    if (currentDistanceToTarget < minDistance)
    //    {
    //        float timeBonus = rewardMultiplierForSpeed * (1 - Mathf.Clamp01(timeToTarget / maxTimeToReachTarget));
    //        AddReward(rewardForReachingTarget + timeBonus);
    //        EndEpisode();
    //    }

    //    else if (transform.localPosition.y < fallingY || OutsideFloorBounds())
    //    {
    //        EndEpisode();
    //    }
    //}

    private bool OutsideFloorBounds()
    {
        Vector3 position = transform.localPosition;
        return Mathf.Abs(position.x) > (outOfBoundsDimensions.x) / 2 || Mathf.Abs(position.z) > (outOfBoundsDimensions.y)/ 2;
    }

    private bool IsAccelerating;
    private void HandleMovement(float verticalInput, float horizontalInput)
    {
        // Calculate current speed along the car's forward axis
        float forwardSpeed = Vector3.Dot(transform.forward, rigidBody.linearVelocity);
        float speedFactor = Mathf.InverseLerp(0, maxSpeed, Mathf.Abs(forwardSpeed)); // Normalized speed factor

        // Reduce motor torque and steering at high speeds for better handling
        float currentMotorTorque = Mathf.Lerp(motorTorque, 0, speedFactor);
        float currentSteerRange = Mathf.Lerp(steeringRange, steeringRangeAtMaxSpeed, speedFactor);

        // Determine if the player is accelerating or trying to reverse
        bool isAccelerating = Mathf.Sign(verticalInput) == Mathf.Sign(forwardSpeed);

        IsAccelerating = isAccelerating;

        if (toggleDebugMode)
        {
            uiDebug.SetDebugValues(new Vector2(verticalInput, horizontalInput), rigidBody.linearVelocity);
            //Debug.Log($"Speed: {forwardSpeed}, Angular Velocity: {rigidBody.angularVelocity}, Steer: {currentSteerRange}");
        }

        foreach (var wheel in wheels)
        {
            // Apply steering to wheels that support steering
            if (wheel.steerable)
            {
                wheel.WheelCollider.steerAngle = horizontalInput * currentSteerRange;
            }

            if (isAccelerating)
            {
                // Apply torque to motorized wheels
                if (wheel.motorized)
                {
                    wheel.WheelCollider.motorTorque = verticalInput * currentMotorTorque;
                }
                // Release brakes when accelerating
                wheel.WheelCollider.brakeTorque = 0f;
            }
            else
            {
                // Apply brakes when reversing direction
                wheel.WheelCollider.motorTorque = 0f;
                wheel.WheelCollider.brakeTorque = Mathf.Abs(vIn) * brakeTorque;
            }
        }
    }

    #region --- Gizmos ---
    private void OnDrawGizmos()
    {
        DrawFloorBoundsGizmos(floorDimensions, Color.green);
        DrawFloorBoundsGizmos(outOfBoundsDimensions, Color.red);
    }

    void DrawFloorBoundsGizmos(Vector3 boundsDimensions, Color color)
    {
        Gizmos.color = color;

        // Use the agent's parent transform (e.g., platform or environment center), or Vector3.zero if none
        Vector3 center = transform.parent ? transform.parent.position + Vector3.up * floorDebugOffsetY : Vector3.zero;
        float halfWidth = boundsDimensions.x / 2f;
        float halfHeight = boundsDimensions.y / 2f;

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


    #endregion
}
