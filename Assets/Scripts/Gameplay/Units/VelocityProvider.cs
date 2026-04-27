using UnityEngine;

public class VelocityProvider : MonoBehaviour
{
    private float currentSpeed;
    private Vector3? targetPosition = null;
    private float stoppingDistance = 0.05f;

    public void SetTarget(Vector3 target, float speed)
    {
        targetPosition = target;
        currentSpeed = speed;
    }

    public bool HasReachedTarget()
    {
        if (!targetPosition.HasValue) return true;
        return Vector3.Distance(transform.position, targetPosition.Value) <= stoppingDistance;
    }

    private void Update()
    {
        if (targetPosition.HasValue)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition.Value,
                currentSpeed * Time.deltaTime
            );

            if (HasReachedTarget())
            {
                transform.position = targetPosition.Value;
                targetPosition = null;
            }
        }
    }
}