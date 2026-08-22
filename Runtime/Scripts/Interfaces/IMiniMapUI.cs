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
		Vector2 WorldToMiniMapPoint(Vector3 worldPosition);

		Vector3 Center { get; set; }
		float Rotation { get; set; }
		float ViewWorldHeight { get; set; }
		float ViewAspectRatio { get; }
	}
}
