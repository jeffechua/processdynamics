using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Viewport : MonoBehaviour {

	public double unitAmount = 1.0;
	public SpriteRenderer fluidSprite;
	public Transform rBar;
	public Transform gBar;
	public Transform bBar;

	public FluidElement userSetState;
	public bool usingUserSetState;

	FluidElement _state;
	public FluidElement state {
		set {
			if (_state != value) {
				_state = value;
				Redraw();
			}
		}
		get => _state;
	}

	// Update is called once per frame
	void Update() {
		if (usingUserSetState) {
			state = userSetState;
		}
	}

	void Redraw() {
		float r = Mathf.Clamp((float)state.f.r, 0, 1);
		float g = Mathf.Clamp((float)state.f.g, 0, 1);
		float b = Mathf.Clamp((float)state.f.b, 0, 1);
		float amount = Mathf.Clamp((float)(state.amount / unitAmount), 0, 1);
		fluidSprite.color = new Color(r, g, b);
		fluidSprite.transform.localScale = new Vector3(1, amount, 1);
		fluidSprite.transform.localPosition = new Vector3(0, (amount - 1) / 2, 0);
		rBar.transform.localScale = new Vector3(rBar.transform.localScale.x, r, 1);
		gBar.transform.localScale = new Vector3(rBar.transform.localScale.x, g, 1);
		bBar.transform.localScale = new Vector3(rBar.transform.localScale.x, b, 1);
		rBar.transform.localPosition = new Vector3(rBar.transform.localPosition.x, -r / 2, 0);
		gBar.transform.localPosition = new Vector3(gBar.transform.localPosition.x, -g / 2, 0);
		bBar.transform.localPosition = new Vector3(bBar.transform.localPosition.x, -b / 2, 0);
	}

}
