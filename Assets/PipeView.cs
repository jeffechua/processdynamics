using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PipeView : MonoBehaviour {

	LineRenderer line;

	// Design variables
	public int length;
	public Source source;
	public Sink sink;

	// Display variables
	public Vector3[] vertices;
	public bool updateDisplay;
	public Transform[] alignees;
	float[] lengths;

	// Operating variables
	public Pipe model;

	private void Start() {
		model = new Pipe(length);
		line = GetComponent<LineRenderer>();
		if (vertices.Length == 0) {
			vertices = new Vector3[line.positionCount];
			line.GetPositions(vertices);
		}
	}

	private void Update() {
		if (updateDisplay) {
			updateDisplay = false;
			// Update line
			lengths = new float[vertices.Length];
			for (int i = 0; i < vertices.Length; i++)
				lengths[i] = i == 0 ? 0 : ((vertices[i] - vertices[i - 1]).magnitude + lengths[i - 1]);
			for (int i = 0; i < vertices.Length - 1; i++)
				lengths[i] /= lengths[lengths.Length - 1];
			line.positionCount = vertices.Length;
			line.SetPositions(vertices);
			// Align alignees
			foreach (Transform alignee in alignees) SnapToLine(alignee);
		}
	}

	public Vector3 LineParamToWorldPoint(float parameter) {
		if (parameter < 0) return vertices[0];
		if (parameter > 1) return vertices[vertices.Length - 1];
		int i = 0;
		while (parameter > lengths[i]) i++;
		return transform.TransformPoint(Vector3.Lerp(vertices[i - 1], vertices[i], (parameter - lengths[i - 1]) / (lengths[i] - lengths[i - 1])));
	}

	// specificParam returns the lerp parameter between vertices[i] and vertices[i+1].
	private bool LocaliseWorldPoint(Vector3 point, out int segment, out float specificParam) {

		point = transform.InverseTransformPoint(point);

		if (vertices.Length <= 1) {
			segment = -1;
			specificParam = -1;
			return false;
		}

		int closest = 0;
		float closestMinDistance = -1;
		float specificParamOfClosest = 0;
		for (int i = 0; i < vertices.Length - 1; i++) {
			Vector3 dir = vertices[i + 1] - vertices[i];
			float minDistance;
			float param = Vector3.Dot(point - vertices[i], dir) / dir.sqrMagnitude; // 0 to 1 on the segment
			/* If the point is not between the two vertices, use distance to the closer vertex instead of perp. distance
			 *
			 *      *     *
			 *      |    /       ~ : the line segment being considered
			 *      |   /        | : the min distance derived
			 *    ~~~~~~
			 */
			if (param < 0) {
				minDistance = (vertices[i] - point).magnitude;
			} else if (param > 1) {
				minDistance = (vertices[i + 1] - point).magnitude;
			} else {
				minDistance = Mathf.Abs(Vector3.Dot(point - vertices[i], Vector2.Perpendicular(dir).normalized));
			}
			if (closestMinDistance == -1 || minDistance < closestMinDistance) {
				closest = i;
				closestMinDistance = minDistance;
				specificParamOfClosest = param;
			}
		}

		segment = closest;
		specificParam = specificParamOfClosest;
		return true;

	}

	public float WorldPointToLineParam(Vector3 point) {
		LocaliseWorldPoint(point, out int segment, out float specificParam);
		return (lengths[segment + 1] - lengths[segment]) * specificParam + lengths[segment];
	}

	public void SnapToLine(Transform t) {
		LocaliseWorldPoint(t.position, out int segment, out float specificParam);
		t.position = transform.TransformPoint(Vector3.Lerp(vertices[segment], vertices[segment + 1], specificParam));
		t.rotation = Quaternion.FromToRotation(Vector3.right, transform.TransformDirection(vertices[segment + 1] - vertices[segment]));
	}

}