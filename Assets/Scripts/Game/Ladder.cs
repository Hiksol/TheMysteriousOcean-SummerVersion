using UnityEngine;

public class Ladder : MonoBehaviour
{
    public Vector3 ladderSegmentBottom;
    public float ladderSegmentLength;

    public Transform bottomReleasePoint;
    public Transform topReleasePoint;

    public Vector3 BottomAnchorPoint => transform.position + transform.TransformVector(ladderSegmentBottom);
    public Vector3 TopAnchorPoint => transform.position + transform.TransformVector(ladderSegmentBottom) + (transform.up * ladderSegmentLength);

    public Vector3 ClosestPointOnLadderSegment(Vector3 fromPoint, out float onSegmentState) {
        Vector3 segment = TopAnchorPoint - BottomAnchorPoint;            
        Vector3 segmentPoint1ToPoint = fromPoint - BottomAnchorPoint;
        float pointProjectionLength = Vector3.Dot(segmentPoint1ToPoint, segment.normalized);

        // When higher than bottom point
        if(pointProjectionLength > 0) {
            // If we are not higher than top point
            if (pointProjectionLength <= segment.magnitude) {
                onSegmentState = 0;
                return BottomAnchorPoint + (segment.normalized * pointProjectionLength);
            }
            // If we are higher than top point
            else {
                onSegmentState = pointProjectionLength - segment.magnitude;
                return TopAnchorPoint;
            }
        }
        // When lower than bottom point
        else {
            onSegmentState = pointProjectionLength;
            return BottomAnchorPoint;
        }
    }

    void OnDrawGizmos() {
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(BottomAnchorPoint, TopAnchorPoint);
    }
}