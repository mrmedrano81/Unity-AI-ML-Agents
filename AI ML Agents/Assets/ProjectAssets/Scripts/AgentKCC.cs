using UnityEngine;
using KinematicCharacterController;

public class AgentKCC : MonoBehaviour, ICharacterController
{
    public KinematicCharacterMotor Motor;

    [Header("Settings")]
    public Vector3 gravity;
    public float moveSpeed;
    public Collider[] ignoredColliders;

    private void Awake()
    {
        Motor = GetComponent<KinematicCharacterMotor>();
        Motor.CharacterController = this;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
    }

    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        if (IsGrounded())
        {

        }
        else
        {
            currentVelocity += gravity * deltaTime;
        }

    }
    public void AfterCharacterUpdate(float deltaTime)
    {
    }

    public void BeforeCharacterUpdate(float deltaTime)
    {
    }

    public bool IsColliderValidForCollisions(Collider coll)
    {
        if (ignoredColliders != null)
        {
            for (int i = 0; i < ignoredColliders.Length; i++)
            {
                if (coll == ignoredColliders[i])
                {
                    return false;
                }
            }
        }

        return true;
    }

    public void OnDiscreteCollisionDetected(Collider hitCollider)
    {
    }

    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {
    }

    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {
    }

    public void PostGroundingUpdate(float deltaTime)
    {
    }

    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
    {
    }

    #region --- Internal Functions ---
    private bool IsGrounded()
    {
        return Motor.GroundingStatus.IsStableOnGround;
    }

    #endregion
}
