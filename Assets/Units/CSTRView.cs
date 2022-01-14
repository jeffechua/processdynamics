using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CSTRView : ViewableModelView {

	public double capacity;
	public FluidElement contents;

	// Model
	public CSTRModel cstrModel;
	public override ModelObject model { get => cstrModel; }

	public override void InitModel() {
		cstrModel = new CSTRModel(capacity, contents);
	}


}

[Serializable]
public class CSTRModel : ViewableModelObject, FluidReceiver, FluidProvider {

	// Design variables
	public readonly double capacity;

	// Operating variables
	public FluidElement contents;
	public override FluidElement viewed { get => contents; }
	public FluidElement inBuffer;
	public double withdrawnAmount;
	public override bool blocked { get => contents.amount >= capacity; }
	public override string whyBlocked { get => "CSTR overfull."; }

	public CSTRModel(double capacity, FluidElement contents) {
		this.capacity = capacity;
		this.contents = contents;
	}

	public void React() {

	}

	public override void Tick() {
	}

	public override void Tock() {
		contents = new FluidElement(contents.conc, (contents.amount - withdrawnAmount));
		withdrawnAmount = 0;
		if (inBuffer.amount > capacity - contents.amount) {
			FluidElement transferred = inBuffer.conc.unit * (capacity - contents.amount);
			contents += transferred;
			inBuffer -= transferred;
		} else {
			contents += inBuffer;
			inBuffer = default;
		}
		React();
		base.Tock();
	}

	public bool Insert(FluidElement element) {
		if (!blocked) {
			inBuffer += element;
			return true;
		}
		return false;
	}

	public FluidElement Withdraw(double amount) {
		amount = Math.Min(amount, contents.amount - withdrawnAmount);
		withdrawnAmount += amount;
		return new FluidElement(contents.conc, amount);
	}

}