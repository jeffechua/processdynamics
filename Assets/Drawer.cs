using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Drawer : MonoBehaviour
{

	public Transform lip;

	public float height = 0.7f;

	public Vector3 prevMousePosition;

	private void OnMouseDown() {
		prevMousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
	}

	private void OnMouseDrag() {
		Vector3 newMousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		Vector3 dir = transform.TransformDirection(Vector3.down);
		height += Vector3.Dot(newMousePosition - prevMousePosition, dir)/transform.parent.TransformVector(dir).magnitude;
		prevMousePosition = newMousePosition;
		Redraw();
	}

	void Redraw() {
		if (height < 0) height = 0;
		transform.localScale = new Vector3(transform.localScale.x, height, 1);
		lip.position = transform.TransformPoint(new Vector3(0, -1, 0));
	}
}
