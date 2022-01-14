using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PipeView : ModelView {

	LineRenderer line;

	// Design variables
	public int nElements;
	public ModelView receiver;

	// Display variables
	public ModelView[] attached;
	public bool snapAttachments;
	Vector3[] vertices;
	float[] lengths;

	// Model
	public PipeModel pipeModel;
	public override ModelObject model { get => pipeModel; }

	public override void InitModel() {
		pipeModel = new PipeModel(nElements);
	}
	public override void LinkModel() {
		pipeModel.receiver = Util.TryCast<FluidReceiver>(receiver?.model);
		pipeModel.attachments = attached
			.Select((att, i) => new PipeAttachment((PipeAttachable)att?.model, i))
			.Where((attachment) => attachment.attached != null).ToArray(); // Inefficient, but not a big deal.
	}

	private void Start() {
		ProcessMaster.modelViews.Add(this);
		line = GetComponent<LineRenderer>();
		if (vertices == null) {
			vertices = new Vector3[line.positionCount];
			line.GetPositions(vertices);
			SetVertices(vertices);
		}
		if (attached == null) {
			attached = new Viewport[nElements];
		} else if (attached.Length < nElements) {
			// Locate which element each attachment is attached to; if multiple are in the same location, we do
			// first-come-first-serve, and bump any latecomers down.
			ModelView[] unlocated = attached;
			attached = new Viewport[nElements];
			for (int i = 0; i < unlocated.Length; i++) {
				int j = Mathf.RoundToInt(Project(unlocated[i].transform.position) * nElements - 0.5f); // new location
				j = Mathf.Clamp(j, 0, nElements - 1);
				while (attached[j % nElements] != null) j++;
				attached[j % nElements] = unlocated[i];
			}
		}
	}

	private void Update() {
		line.material.color = (model?.blocked ?? false) ? Color.yellow : Color.white;
		if (snapAttachments) {
			snapAttachments = false;
			for (int i = 0; i < nElements; i++)
				if (attached[i] != null)
					SnapToLine(attached[i].transform, parameter: (i + 0.5f) / nElements);
		}
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

	// Transformation functions between pipe and world space.
	public Vector3 PointOnSegment(int seg, float param) => transform.TransformPoint(Vector3.Lerp(vertices[seg], vertices[seg + 1], param));
	public Vector3 DirectionOfSegment(int segment) => transform.TransformDirection(vertices[segment + 1] - vertices[segment]);
	public Vector3 PointOnLine(float parameter) {
		Specify(parameter, out int segment, out float specificParam);
		return PointOnSegment(segment, specificParam);
	}

	// specificParam returns the lerp parameter between vertices[i] and vertices[i+1].
	public bool ProjectSpecific(Vector3 point, out int segment, out float specificParam) {
		point = transform.InverseTransformPoint(point);
		int closest = 0;
		float closestMinDistance = -1;
		float specificParamOfClosest = 0;
		for (int i = 0; i < vertices.Length - 1; i++) {
			Vector3 dir = vertices[i + 1] - vertices[i];
			float minDistance;
			float param = Vector3.Dot(point - vertices[i], dir) / dir.sqrMagnitude; // 0 to 1 on the segment
			/* If the point is not between the two vertices, use distance to the closer vertex instead of perp. distance
			 *      *     *
			 *      |    /       ~ : the line segment being considered
			 *      |   /        | : the min distance derived
			 *    ~~~~~~
			 */
			minDistance = param < 0 ? (vertices[i] - point).magnitude :
						 (param > 1 ? (vertices[i + 1] - point).magnitude :
						  Mathf.Abs(Vector3.Dot(point - vertices[i], Vector2.Perpendicular(dir).normalized)));
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

[Serializable]
public struct PipeAttachment {
	public PipeAttachable attached;
	public int location;
	public PipeAttachment(PipeAttachable attached, int location) {
		this.attached = attached;
		this.location = location;
	}
}

public interface PipeAttachable {
	void Transmit(FluidElement element);
}

// A pipe is not a FluidProvider because FluidProvider marks objects which accept Withdraw() requests;
// a pipe only pushes fluid elements downstream and does not accept requests from the receiver.
// Similarly, a pipe does not make requests of its upstream; it only pushes and never pulls.
[Serializable]
public class PipeModel : ModelObject, FluidReceiver {

	// Design variables
	public readonly int length;
	public FluidReceiver receiver;
	public PipeAttachment[] attachments;
	bool valid { get => length != 0 && inventory != null && inventory.Length == length + 1; }

	// Operating variables
	int pointer;
	int ppointer { get => (pointer + 1) % inventory.Length; }
	public override bool blocked { get => valid && inventory[pointer] != default; }
	public override string whyBlocked { get => "No receiver or receiver is blocked."; }
	public FluidElement[] inventory;

	public PipeModel(int length) {
		this.length = length;
		inventory = new FluidElement[length + 1];
	}

	/* Insert() places into pointer (denoted V below) and Tick() pushes from (pointer+1) (denoted ^ below). They
	 * may occur in any order.
	 * 
	 *      V   ^                V   ^                    V   ^
	 *    |   | A |   >Tick()  |   |   |   >Insert(B)   | B |   |
	 *    
	 *    or
	 *    
	 *      V   ^                   V   ^                 V   ^
	 *    |   | A |   >Insert(B)  | B | A |   >Tick()   | B |   |
	 *    
	 *                        (temporarily reads
	 *                         as blocked here)
	 *                         
	 * Hence, a pipe has an inventory of length+1.
	 * Unblocking behaviour after a true block is not tick order blind as whether the pipe ticks before its provider
	 * determines whether the provider's tick will go through on that frame.
	 */

	public override void Tick() {
		if (receiver != null && receiver.Insert(inventory[ppointer]))
			inventory[ppointer] = default;
	}

	public override void Tock() {
		foreach (PipeAttachment attachment in attachments)
			attachment.attached.Transmit(inventory[(pointer + inventory.Length - attachment.location) % inventory.Length]);
		if (inventory[ppointer] == default)
			pointer = ppointer;
		// else, it blocks and the next Insert() returns false.
	}

	public bool Insert(FluidElement fluidElement) {
		if (inventory[pointer] == default) {
			inventory[pointer] = fluidElement;
			return true;
		}
		return false;
	}

}
