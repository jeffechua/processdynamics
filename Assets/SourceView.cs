using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SourceView : ViewableModelView {

	public FluidState compositionBase;
	public FluidState compositionAmplitude;
	public double amountBase;
	public double amountAmplitude;
	public double period;
	public ModelView receiver;

	// Model
	public SourceModel sourceModel;
	public override ModelObject model { get => sourceModel; }

	public override void InitModel() {
		sourceModel = new SourceModel(compositionBase, compositionAmplitude, amountBase, amountAmplitude, period, Util.TryCast<FluidReceiver>(receiver?.model));
	}

	public override void LinkModel() {
		sourceModel.receiver = Util.TryCast<FluidReceiver>(receiver?.model);
		base.LinkModel();
	}

}

[Serializable]
public class SourceModel : ViewableModelObject, FluidProvider {

	// Design variables
	public FluidState compositionBase;
	public FluidState compositionAmplitude;
	public double amountBase;
	public double amountAmplitude;
	public double period;
	public double phaseFactor { get => Math.Sin(ProcessMaster.time * 2 * Math.PI / period); }
	public FluidState composition { get => compositionBase + compositionAmplitude * phaseFactor; }
	public double amount { get => amountBase + amountAmplitude * phaseFactor; }
	public FluidElement element {
		get {
			double factor = phaseFactor;
			return new FluidElement(compositionBase + compositionAmplitude * factor, amountBase + amountAmplitude * factor);
		}
	}
	public override FluidElement viewed {
		get => receiver == null ? composition.unit : element;
	}
	public FluidReceiver receiver;

	public SourceModel(FluidState compositionBase, FluidState compositionAmplitude, double amountBase, double amountAmplitude, double period, FluidReceiver receiver) {
		this.compositionBase = compositionBase;
		this.compositionAmplitude = compositionAmplitude;
		this.amountBase = amountBase;
		this.amountAmplitude = amountAmplitude;
		this.period = period;
		this.receiver = receiver;
	}

	public override void Tick() {
		receiver?.Insert(element);
	}

	public FluidElement Withdraw(double amount) {
		return new FluidElement(composition, amount);
	}

}