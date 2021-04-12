using System;
using UnityEngine;

[Serializable]
public struct FluidState : IEquatable<FluidState> {
	public double r;
	public double g;
	public double b;
	public double T;

	public FluidElement unit { get => new FluidElement { amount = 1, net = this }; }

	public override bool Equals(object obj) => obj is FluidState && Equals((FluidState)obj);
	public bool Equals(FluidState other) => r == other.r && g == other.g && b == other.b && T == other.T;
	public override int GetHashCode() => new Tuple<Color, double>(new Color((float)r, (float)g, (float)b), T).GetHashCode();
	public static bool operator ==(FluidState lhs, FluidState rhs) => lhs.Equals(rhs);
	public static bool operator !=(FluidState lhs, FluidState rhs) => !(lhs == rhs);

	public static FluidState operator +(FluidState first, FluidState second) =>
		new FluidState {
			r = first.r + second.r,
			g = first.g + second.g,
			b = first.b + second.b,
			T = first.T + second.T,
		};

	public static FluidState operator -(FluidState first, FluidState second) =>
		new FluidState {
			r = first.r - second.r,
			g = first.g - second.g,
			b = first.b - second.b,
			T = first.T - second.T,
		};

	public static FluidState operator *(FluidState state, double factor) =>
		new FluidState {
			r = state.r * factor,
			g = state.g * factor,
			b = state.b * factor,
			T = state.T * factor,
		};

	public static FluidState operator /(FluidState state, double factor) =>
		new FluidState {
			r = state.r / factor,
			g = state.g / factor,
			b = state.b / factor,
			T = state.T / factor,
		};

}

[Serializable]
public struct FluidElement : IEquatable<FluidElement> {
	public FluidState net;
	public FluidState conc {
		get => amount == 0 ? default : net / amount;
		set => net = value * amount;
	}
	public double amount;

	public FluidElement(FluidState composition, double amount) {
		this.amount = amount;
		net = composition * amount;
	}

	public override bool Equals(object obj) => obj is FluidElement && Equals((FluidElement)obj);
	public bool Equals(FluidElement other) => amount == other.amount && net == other.net;
	public override int GetHashCode() => new Tuple<FluidState, double>(net, amount).GetHashCode();
	public static bool operator ==(FluidElement lhs, FluidElement rhs) => lhs.Equals(rhs);
	public static bool operator !=(FluidElement lhs, FluidElement rhs) => !(lhs == rhs);

	public static FluidElement operator +(FluidElement first, FluidElement second) =>
		new FluidElement {
			amount = first.amount + second.amount,
			net = first.net + second.net
		};
	public static FluidElement operator -(FluidElement first, FluidElement second) =>
		new FluidElement {
			amount = first.amount - second.amount,
			net = first.net - second.net
		};
	public static FluidElement operator *(FluidElement element, double factor) =>
		new FluidElement {
			amount = element.amount * factor,
			net = element.net * factor
		};
	public static FluidElement operator /(FluidElement element, double factor) =>
		new FluidElement {
			amount = element.amount / factor,
			net = element.net / factor
		};
}

public abstract class ModelView : MonoBehaviour {
	public abstract ModelObject model { get; }
	public abstract void InitModel();
	public abstract void LinkModel();
	private void Start() {
		ProcessMaster.Register(this);
	}
	private void OnDestroy() {
		ProcessMaster.Deregister(this);
	}
}

public abstract class ViewableModelView : ModelView {
	public Viewport viewport;
	public override void LinkModel() {
		((ViewableModelObject)model).viewport = viewport.viewportModel;
	}
}

public abstract class ModelObject {
	// Under normal operation, blocked must return false between frames.
	// blocked is allowed to be temporarily true in the middle of a frame (after some have ticked but others haven't)
	// The blocked property is purely informational. A blocked object should not stop ticking or throw exceptions,
	// and should automatically unblock if/when possible. We allow unblocking behaviour to be non-tick order-blind.
	public virtual bool blocked { get => false; }
	public virtual string whyBlocked { get => ""; }
	// Tick() should be used for all physical transfers between unit operations or model objects.
	public virtual void Tick() { }
	// Tock() should be used for (a) resolving internal buffered actions, and (b) transmitting information between
	// model objects to be used in the next frame's Tick(). Transmissions should be reflective of the inter-frame
	// (post-Tock()) state, so they should originate from the Tock() of the reading's origin. If readings from multiple
	// objects are needed, they should be cached in an intermediary controller during the origin frame, and combined and
	// re-transmitted on the next frame; this will manifest as a necessary signal delay of 1 frame.
	public virtual void Tock() { }

	// Example:
	// TICK:
	//	  valve.Tick(): calls Withdraw() on CSTR1, Insert()s the element into a pipe.
	//    pipe.Tick(): Insert()s an element into CSTR2.
	// TOCK:
	//    CSTR1.Tock(): applies valve's withdrawal to contents. Transmit()s to viewport1 for display.
	//    CSTR2.Tock(): applies pipe's insertion to contents. Transmit()s to viewport3 for display.
	//    pipe.Tock(): Transmit()s third element data to viewport2.
	//        viewport2.Transmit(): display, and also cascade Transmit() to controller1.
	//             controller1.Transmit(): compares to set point and sets valve.flowRate
	//
	// On the next Tick(), the new valve.flowRate is applied.
	// If the controller is a cascade controller, the set point is transmitted from another component. Therefore, both
	// set point and reading transmission should be cached this frame, and valve.flowRate set in next frame's Tock(),
	// resulting in actual valve actuation two frames later rather than one frame later.
}

public abstract class ViewableModelObject : ModelObject {
	public ViewportModel viewport;
	public abstract FluidElement viewed { get; }
	public override void Tock() {
		viewport.Transmit(viewed);
	}
}

public interface FluidReceiver {
	// Return true if successfully received
	// If it returns false, this indicates no fluid was transferred.
	// Should be called even if it registers as blocked
	bool Insert(FluidElement fluidElement);
}

public interface FluidProvider {
	// may not return amount requested
	FluidElement Withdraw(double amount);
}