using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SinkView : ViewableModelView {

	// Model
	public SinkModel sinkModel;
	public override ModelObject model { get => sinkModel; }

	public override void InitModel() {
		sinkModel = new SinkModel();
	}

}

[Serializable]
public class SinkModel : ViewableModelObject, FluidReceiver {
	public FluidElement last;
	public override FluidElement viewed { get => last; }
	public bool Insert(FluidElement element) {
		last = element;
		return true;
	}
}