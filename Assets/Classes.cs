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

public interface Source {
	FluidElement Pop();
}

public interface Sink {
	void Push(FluidElement fluidElement);
}

[Serializable]
public class Pipe : Source, Sink {

	// Design variables
	public readonly int length;
	public Source source;
	public Sink sink;

	// Operating variables
	int pointer;
	bool _isAwaitingPush;
	public bool isAwaitingPush { get => _isAwaitingPush; }
	public FluidElement[] inventory;

	public Pipe(int length) {
		this.length = length;
		inventory = new FluidElement[length + 1];
	}

	public FluidElement Pop() {
		if (_isAwaitingPush)
			throw new Exception("Tried to pull from awaiting pipe.");
		FluidElement retval = inventory[pointer];
		inventory[pointer] = default;
		_isAwaitingPush = true;
		return retval;
	}

	public void Push(FluidElement fluidElement) {
		if (!_isAwaitingPush)
			throw new Exception("Tried to push to full pipe.");
		inventory[pointer] = fluidElement;
		pointer = (pointer + 1) % length;
		_isAwaitingPush = false;
	}


}
