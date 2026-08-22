using System;
using ParkMinPackages.Foundation.Components;
using ParkMinPackages.Workflow.Minimap.Interfaces;
using UnityEngine;

namespace ParkMinPackages.Workflow.Minimap.Components.UILogics
{
	[DisallowMultipleComponent, RequireComponent(typeof(MiniMapUI))]
	public sealed class MiniMapBottomToTopFitController : ExtendedBehaviour
	{
		// - Class Struct Enum -
		public enum PaddingMode
		{
			World,
			Ratio
		}

		// - Public Methods -
		public void Fit(
			Vector3 bottomWorldPosition,
			Vector3 topWorldPosition,
			float padding,
			PaddingMode paddingMode
		) {
			Fit(
				bottomWorldPosition,
				topWorldPosition,
				padding,
				padding,
				padding,
				padding,
				paddingMode
			);
		}

		public void Fit(
			Vector3 bottomWorldPosition,
			Vector3 topWorldPosition,
			float horizontalPadding,
			float verticalPadding,
			PaddingMode paddingMode
		) {
			Fit(
				bottomWorldPosition,
				topWorldPosition,
				horizontalPadding,
				horizontalPadding,
				verticalPadding,
				verticalPadding,
				paddingMode
			);
		}

		public void Fit(
			Vector3 bottomWorldPosition,
			Vector3 topWorldPosition,
			float leftPadding,
			float rightPadding,
			float topPadding,
			float bottomPadding,
			PaddingMode paddingMode
		) {
			if (leftPadding < 0f || rightPadding < 0f || topPadding < 0f || bottomPadding < 0f)
				throw new ArgumentOutOfRangeException(nameof(leftPadding));
			if (paddingMode == PaddingMode.Ratio &&
			    (1f <= leftPadding + rightPadding || 1f <= topPadding + bottomPadding))
				throw new ArgumentOutOfRangeException(nameof(paddingMode));

			IMiniMapUI miniMapUI = MiniMapUI;
			Vector2 direction = new Vector2(
				topWorldPosition.x - bottomWorldPosition.x,
				topWorldPosition.z - bottomWorldPosition.z
			);
			float distance = direction.magnitude;
			float rotation = distance <= Mathf.Epsilon
				? miniMapUI.Rotation
				: Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
			float viewAspectRatio = Mathf.Max(0.01f, miniMapUI.ViewAspectRatio);

			switch (paddingMode) {
				case PaddingMode.World:
					break;
				case PaddingMode.Ratio:
					float ratioViewWorldHeight = Mathf.Max(
						0.01f,
						distance / (1f - topPadding - bottomPadding)
					);
					float ratioViewWorldWidth = ratioViewWorldHeight * viewAspectRatio;
					leftPadding *= ratioViewWorldWidth;
					rightPadding *= ratioViewWorldWidth;
					topPadding *= ratioViewWorldHeight;
					bottomPadding *= ratioViewWorldHeight;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(paddingMode));
			}

			float rotationRadians = rotation * Mathf.Deg2Rad;
			float localCenterX = (rightPadding - leftPadding) * 0.5f;
			float localCenterY = (topPadding - bottomPadding) * 0.5f;
			Vector3 centerOffset = new Vector3(
				localCenterX * Mathf.Cos(rotationRadians) + localCenterY * Mathf.Sin(rotationRadians),
				0f,
				-localCenterX * Mathf.Sin(rotationRadians) + localCenterY * Mathf.Cos(rotationRadians)
			);
			float requiredWorldHeight = distance + topPadding + bottomPadding;
			float requiredWorldWidth = leftPadding + rightPadding;
			float viewWorldHeight = Mathf.Max(
				0.01f,
				requiredWorldHeight,
				requiredWorldWidth / viewAspectRatio
			);
			miniMapUI.SetView(
				(bottomWorldPosition + topWorldPosition) * 0.5f + centerOffset,
				rotation,
				viewWorldHeight
			);
		}

		// - Public Properties -
		public IMiniMapUI MiniMapUI
		{
			get
			{
				if (_miniMapUI == null)
					throw new ArgumentNullException(nameof(_miniMapUI));

				return _miniMapUI;
			}
			set { _miniMapUI = value ?? throw new ArgumentNullException(nameof(value)); }
		}

		// - Internals -
		IMiniMapUI _miniMapUI;
	}
}