using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProcessMaster : MonoBehaviour {

	public static List<ModelView> modelViews = new List<ModelView>();
	public List<ModelView> views;
	public static List<ModelObject> models = new List<ModelObject>();
	public static int time;
	public bool step;
	public bool running;
	public bool rebuildModels;

	public static void Register(ModelView modelView) {
		modelViews.Add(modelView);
	}
	public static void Deregister(ModelView modelView) {
		modelViews.Remove(modelView);
	}

	// Update is called once per frame
	void Update() {
		views = modelViews;
		if (rebuildModels) {
			rebuildModels = false;
			foreach (ModelView modelView in modelViews) modelView.InitModel();
			foreach (ModelView modelView in modelViews) modelView.LinkModel();
			models = modelViews.ConvertAll((view) => view.model);
		}
	}

	void FixedUpdate() {
		if (running || step) {
			foreach (ModelObject model in models) model.Tick();
			foreach (ModelObject model in models) model.Tock();
			foreach (ModelObject model in models) if (model.blocked) Debug.Log(model.GetType().ToString());
			time++;
			step = false;
		}
	}

}

public static class Util {
	public static T TryCast<T>(object obj) => obj is T ? (T)obj : default(T);
}