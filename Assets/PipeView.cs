using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PipeView : MonoBehaviour, ModelView {

	LineRenderer line;

	// Design variables
	public int nElements;
	public ModelView source;
	public ModelView sink;

	// Display variables
	public Transform[] attachments;
	public bool snapAttachments;
	Vector3[] vertices;
	float[] lengths;

	// Model
	Pipe _model;
	public ModelObject model { get => _model; }

	private void Start() {
		line = GetComponent<LineRenderer>();
		if (vertices == null) {
			vertices = new Vector3[line.positionCount];
			line.GetPositions(vertices);
			SetVertices(vertices);
		}
		if (attachments == null) {
			attachments = new Transform[nElements];
		} else if (attachments.Length < nElements) {
			// Locate which element each attachment is attached to; if multiple are in the same location, we do
			// first-come-first-serve, and bump any latecomers down.
			Transform[] unlocated = attachments;
			attachments = new Transform[nElements];
			for (int i = 0; i < unlocated.Length; i++) {
				int j = Mathf.RoundToInt(Project(unlocated[i].position) * nElements - 0.5f); // new location
				j = Mathf.Clamp(j, 0, nElements - 1);
				while (attachments[j % nElements] != null) j++;
				attachments[j % nElements] = unlocated[i];
			}
		}
	}

	private void Update() {
		if (snapAttachments) {
			snapAttachments = false;
			for (int i = 0; i < nElements; i++)
				if (attachments[i] != null)
					SnapToLine(attachments[i], parameter: (i + 0.5f) / nElements);
		}
	}

	public void InstantiateModel() {
		_model = new Pipe(nElements);
	}
	public void LinkModel() {
		_model.receiver = (sink.model is FluidReceiver) ? (FluidReceiver) sink.model : null;
	}

	public void SetVertices(Vector3[] vertices) {
		this.vertices = vertices;
		lengths = new float[vertices.Length];
		for (int i = 0; i < vertices.Length; i++)
			lengths[i] = i == 0 ? 0 : ((vertices[i] - vertices[i - 1]).magnitude + lengths[i - 1]);
		for (int i = 0; i < vertices.Length; i++)
			lengths[i] /= lengths[lengths.Length - 1];
		line.positionCount = vertices.Length;
		line.SetPositions(vertices);
	}

	public Vector3 PointOnLine(float parameter) {
		if (parameter < 0) return vertices[0];
		if (parameter > 1) return vertices[vertices.Length - 1];
		Specify(parameter, out int segment, out float specificParam);
		return PointOnSegment(segment, specificParam);
	}

	public Vector3 PointOnSegment(int segment, float specificParam)
		=> transform.TransformPoint(Vector3.Lerp(vertices[segment], vertices[segment + 1], specificParam));

	public Vector3 DirectionOfSegment(int segment) => transform.TransformDirection(vertices[segment + 1] - vertices[segment]);

	// specificParam returns the lerp parameter between vertices[i] and vertices[i+1].
	public bool ProjectSpecific(Vector3 point, out int segment, out float specificParam) {

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

	public float Project(Vector3 point) {
		ProjectSpecific(point, out int segment, out float specificParam);
		return Despecify(segment, specificParam);
	}

	public float Despecify(int segment, float specificParam) => (lengths[segment + 1] - lengths[segment]) * specificParam + lengths[segment];
	public void Specify(float parameter, out int segment, out float specificParam) {
		segment = 1;
		while (parameter > lengths[segment]) segment++;
		segment--;
		specificParam = (parameter - lengths[segment]) / (lengths[segment + 1] - lengths[segment]);
	}

	public void SnapToLine(Transform t, int segment = -1, float specificParam = -1, float parameter = -1) {
		if (segment == -1 || specificParam == -1) {
			if (parameter == -1)
				ProjectSpecific(t.position, out segment, out specificParam);
			else
				Specify(parameter, out segment, out specificParam);
		}
		t.position = PointOnSegment(segment, specificParam);
		t.rotation = Quaternion.FromToRotation(Vector3.right, DirectionOfSegment(segment));
	}

}