using UnityEngine;

public static class QuaternionExtensions
{
    /// <summary>
    /// Decomposes a rotation into swing and twist components around a normalized axis.
    /// </summary>
    /// <param name="rotation">The original input rotation.</param>
    /// <param name="twistAxis">The direction vector to isolate (must be normalized).</param>
    /// <param name="swing">Output rotation perpendicular to the axis.</param>
    /// <param name="twist">Output rotation strictly around the axis.</param>
    public static void DecomposeSwingTwist(this Quaternion rotation, Vector3 twistAxis, out Quaternion swing, out Quaternion twist) {
        // Project the quaternion's vector part (x, y, z) onto the twist axis
        Vector3 rotationAxis = new(rotation.x, rotation.y, rotation.z);
        Vector3 projection = Vector3.Project(rotationAxis, twistAxis);

        // Reconstruct the twist quaternion
        twist = new Quaternion(projection.x, projection.y, projection.z, rotation.w);
        twist = Quaternion.Normalize(twist); 

        // Handle the singularity condition (180-degree rotation)
        if (twist.w * twist.w + twist.x * twist.x + twist.y * twist.y + twist.z * twist.z < 0.0001f) {
            // If twist is unstable, fallback to identity twist and full swing
            twist = Quaternion.identity;
            swing = rotation;
            return;
        }

        // Calculate Swing: Swing = Total * Inverse(Twist)
        swing = rotation * Quaternion.Inverse(twist);
    }
}