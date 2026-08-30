using UnityEngine;

namespace ParkMinPackages.Workflow.Minimap.Interfaces
{
	public interface IMiniMapUI
	{
		void SetView(
			Vector3 center,
			float rotation,
			float viewWorldHeight
		);
		void SetCenter(Vector3 center);
		void SetRotation(float rotation);
		void SetViewWorldHeight(float viewWorldHeight);

		Vector3 Center { get; }
		float Rotation { get; }
		float ViewWorldHeight { get; }
		float ViewAspectRatio { get; }
	}
}