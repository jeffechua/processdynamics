using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Viewport : ModelView {

	public double maximumAmount = 1.0;
	public SpriteRenderer fluidSprite;
	public Transform rBar;
	public Transform gBar;
	public Transform bBar;

	public FluidElement reading { get => viewportModel == null ? default : viewportModel.reading; }

	// Model
	public ViewportModel viewportModel;
	public override ModelObject model { get => viewportModel; }

	public override void InitModel() {
		viewportModel = new ViewportModel();
	}
	public override void LinkModel() { }

	// Update is called once per frame
	void Update() {
		Redraw();
	}

	void Redraw() {
		FluidState conc = reading.conc;
		float r = Mathf.Clamp((float)conc.r, 0, 1);
		float g = Mathf.Clamp((float)conc.g, 0, 1);
		float b = Mathf.Clamp((float)conc.b, 0, 1);
		float amount = Mathf.Clamp((float)(reading.amount / maximumAmount), 0, 1);
		fluidSprite.color = new Color(r, g, b);
		fluidSprite.transform.localScale = new Vector3(1, amount, 1);
		fluidSprite.transform.localPosition = new Vector3(0, (amount - 1) / 2, 0);
		rBar.transform.localScale = new Vector3(rBar.transform.localScale.x, r, 1);
		gBar.transform.localScale = new Vector3(gBar.transform.localScale.x, g, 1);
		bBar.transform.localScale = new Vector3(bBar.transform.localScale.x, b, 1);
		rBar.transform.localPosition = new Vector3(rBar.transform.localPosition.x, -r / 2, 0);
		gBar.transform.localPosition = new Vector3(gBar.transform.localPosition.x, -g / 2, 0);
		bBar.transform.localPosition = new Vector3(bBar.transform.localPosition.x, -b / 2, 0);
	}

}

[Serializable]
public class ViewportModel : ModelObject, PipeAttachable {

	// Design variables

	// Operating variables
	public FluidElement reading;

	public ViewportModel() {
	}

	public void Transmit(FluidElement fluidElement) {
		reading = fluidElement;
	}

}
