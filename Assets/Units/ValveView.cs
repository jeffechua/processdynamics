using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ValveView : ModelView {

	public double flowRate;
	public ModelView provider;
	public ModelView receiver;

	// Model
	public ValveModel valveModel;
	public override ModelObject model { get => valveModel; }

	public override void InitModel() {
		valveModel = new ValveModel(flowRate);
	}
	public override void LinkModel() {
		valveModel.provider = Util.TryCast<FluidProvider>(provider?.model);
		valveModel.receiver = Util.TryCast<FluidReceiver>(receiver?.model);
	}

}


[Serializable]
public class ValveModel : ModelObject {

	// Design variables
	public FluidProvider provider;
	public FluidReceiver receiver;

	// Operating variables
	public double flowRate;
	public override bool blocked { get => receiver == null; }
	public override string whyBlocked { get => ""; }

	public ValveModel(double flowRate) {
		this.flowRate = flowRate;
	}

	public override void Tick() {
		if (provider == null || receiver == null)
			return;
		FluidElement element = provider.Withdraw(flowRate);
		if (!receiver.Insert(element))
			provider.Withdraw(-element.amount); // refund to provider.
	}

}
