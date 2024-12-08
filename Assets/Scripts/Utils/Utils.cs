using UnityEngine;

public static class Utils
{
    // Finds closest vertex index to ray that is at most 0.42 units away, otherwise returns -1
    // TODO: it would probably be better to check whether the ray intersects the mesh instead of checking the distance to the closest vertex
    public static int FindClosestVertex(Ray ray, Particle[] particles, float distanceThreshold = 0.42f)
    {
        int closestIndex = -1;
        float closestDistance = Mathf.Infinity;

        for (int i = 0; i < particles.Length; i++)
        {
            float distance = Vector3.Cross(ray.direction, particles[i].X - ray.origin).magnitude;

            if (distance < closestDistance && distance < distanceThreshold)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    public static Vector3 FindClosestPointOnRay(Ray ray, Particle[] particles)
    {
        float closestDistance = Mathf.Infinity;
        Vector3 closestPointOnRay = Vector3.zero;

        for (int i = 0; i < particles.Length; i++)
        {
            Vector3 originToVertex = particles[i].X - ray.origin;
            float projectionLength = Vector3.Dot(originToVertex, ray.direction.normalized);
            Vector3 pointOnRay = ray.origin + ray.direction.normalized * projectionLength;
            float distance = Vector3.Distance(pointOnRay, particles[i].X);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPointOnRay = pointOnRay;
            }
        }

        return closestPointOnRay;
    }
}
