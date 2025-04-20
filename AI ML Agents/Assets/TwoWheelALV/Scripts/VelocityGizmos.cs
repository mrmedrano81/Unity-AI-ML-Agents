using UnityEngine;

public class VelocityGizmo : MonoBehaviour
{
    public Color arrowColor = Color.red; // Color of the arrow
    public float arrowScale = 1f; // Scale of the arrow
    public float arrowOffset;

    public Transform player;
    public CarAgent motor;
    public Transform target;

    private void Start()
    {

    }

    void OnDrawGizmos()
    {
        if (player == null || target == null || motor == null)
        {
            return;
        }

        Vector3 velocity = Vector3.zero;

        // Get the velocity vector
        if (motor != null && motor.rigidBody != null)
        {
            velocity = motor.rigidBody.linearVelocity;
        }

        // If the velocity is significant, draw the arrow
        if (velocity.magnitude > 0.1f)
        {
            Gizmos.color = arrowColor;

            // Draw a line representing the direction of velocity
            Gizmos.DrawLine(player.position + player.forward * arrowOffset, player.position + velocity * arrowScale + player.forward * arrowOffset);

            // Draw an arrowhead
            DrawArrowHead(player.position + velocity * arrowScale + player.forward * arrowOffset, velocity.normalized, 0.5f * arrowScale);
        }

        Gizmos.color = Color.cyan;

        // Draw a line representing the direction of velocity
        Gizmos.DrawLine(player.position, target.position);

        // Draw an arrowhead
        DrawArrowHead(target.position, (target.position - player.position).normalized, 0.5f * arrowScale);
    }

    private void DrawArrowHead(Vector3 position, Vector3 direction, float size)
    {
        // Calculate two perpendicular vectors to create an arrowhead
        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 135, 0) * Vector3.forward * size;
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -135, 0) * Vector3.forward * size;

        // Draw the arrowhead lines
        Gizmos.DrawLine(position, position + right);
        Gizmos.DrawLine(position, position + left);
    }
}
