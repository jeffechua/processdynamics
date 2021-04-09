using System;
using UnityEngine;

[Serializable]
public struct FluidState : IEquatable<FluidState> {
	public double r;
	public double g;
	public double b;
	public double T;
	public override bool Equals(object obj) => obj is FluidState && Equals((FluidState)obj);
	public bool Equals(FluidState other) => r == other.r && g == other.g && b == other.b && T == other.T;
	public override int GetHashCode() => new Tuple<Color, double>(new Color((float)r, (float)g, (float)b), T).GetHashCode();
	public static bool operator ==(FluidState lhs, FluidState rhs) => lhs.Equals(rhs);
	public static bool operator !=(FluidState lhs, FluidState rhs) => !(lhs == rhs);
}

[Serializable]
public struct FluidElement : IEquatable<FluidElement> {
	public FluidState f;
	public double amount;
	public override bool Equals(object obj) => obj is FluidElement && Equals((FluidElement)obj);
	public bool Equals(FluidElement other) => amount == other.amount && f == other.f;
	public override int GetHashCode() => new Tuple<FluidState, double>(f, amount).GetHashCode();
	public static bool operator ==(FluidElement lhs, FluidElement rhs) => lhs.Equals(rhs);
	public static bool operator !=(FluidElement lhs, FluidElement rhs) => !(lhs == rhs);
}

public interface ModelView {
	ModelObject model { get; }
	void InstantiateModel();
	void LinkModel();
}

public interface ModelObject {
	// Under normal operation, blocked must return false between frames.
	// blocked is allowed to be temporarily true in the middle of a frame (after some have ticked but others haven't)
	// The blocked property is purely informational. A blocked object should not stop ticking or throw exceptions,
	// and should automatically unblock if/when possible. We allow unblocking behaviour to be non-tick order-blind.
	bool blocked { get; }
	string whyBlocked { get; }
	void Tick();
}

public interface FluidReceiver {
	// Return true if successfully received
	bool Insert(FluidElement fluidElement);
}

[Serializable]
public class Pipe : FluidReceiver, ModelObject {

	// Design variables
	public readonly int length;
	public FluidReceiver receiver;

	// Operating variables
	int outgoingPointer;
	int incomingPointer;
	public bool blocked { get => inventory[incomingPointer] != default; }
	public string whyBlocked { get => "No receiver or receiver is blocked."; }
	public FluidElement[] inventory;

	public Pipe(int length) {
		this.length = length;
		inventory = new FluidElement[length + 1];
		outgoingPointer = 1;
	}

	/* Insert() is called by the pipe's source's Tick(). This pipe's Tick() and Insert() may be called in any order.
	 * Denoting V as the incoming pointer and ^ as the outgoing pointer, we may thus have
	 * 
	 *      V   ^               ^V                        ^   V
	 *    |   | A |   >Tick()  |   |   |   >Insert(B)   | B |   |
	 *    
	 *    or
	 *    
	 *      V   ^                      V^                 ^   V
	 *    |   | A |   >Insert(B)  | B | A |   >Tick()   | B |   |
	 *    
	 *                        (temporarily reads
	 *                         as blocked here)
	 *                         
	 * Hence, a pipe (and any other vessel) must have at least 1 element, and therefore 2 inventory.
	 * Unblocking behaviour after a true block is not tick order blind as whether the pipe ticks before its provider
	 * determines whether the provider's tick will go through on that frame.
	 */

	public void Tick() {
		if (receiver != null && receiver.Insert(inventory[outgoingPointer])) {
			inventory[outgoingPointer] = default;
			outgoingPointer = (outgoingPointer + 1) % length;
		}
	}

	public bool Insert(FluidElement fluidElement) {
		if (inventory[incomingPointer] != default)
			return false;
		inventory[incomingPointer] = fluidElement;
		incomingPointer = (incomingPointer + 1) % length;
		return true;
	}


}
