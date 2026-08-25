using System;
using ParkMinPackages.Foundation.Components;
using ParkMinPackages.Foundation.Constants;
using ParkMinPackages.Workflow.Minimap.Interfaces;
using Sirenix.OdinInspector;
using UnityEngine;

namespace ParkMinPackages.Workflow.Minimap.Components.UILogics
{
	public sealed class MiniMapUIFitter : DependencyBehaviour
	{
		// - Class Struct Enum -
		public enum PaddingMode
		{
			World,
			Ratio
		}

		// - Public Methods -
		public void Initialize(IMiniMapUI miniMapUI) {
			_miniMapUI = miniMapUI;
		}

		public override void ValidateDependencies() {
			if (_miniMapUI == null)
				throw new InvalidOperationException($"{nameof(MiniMapUI)} is not assigned.");
			if (_viewDirection.sqrMagnitude <= Mathf.Epsilon)
				throw new ArgumentOutOfRangeException(nameof(ViewDirection));
			if (_leftPadding < 0f)
				throw new ArgumentOutOfRangeException(nameof(LeftPadding));
			if (_rightPadding < 0f)
				throw new ArgumentOutOfRangeException(nameof(RightPadding));
			if (_topPadding < 0f)
				throw new ArgumentOutOfRangeException(nameof(TopPadding));
			if (_bottomPadding < 0f)
				throw new ArgumentOutOfRangeException(nameof(BottomPadding));
			if (_paddingMode == PaddingMode.Ratio &&
			    (1f <= _leftPadding + _rightPadding || 1f <= _topPadding + _bottomPadding))
				throw new ArgumentOutOfRangeException(nameof(Mode));
		}

		public void Fit(Vector3 pointA, Vector3 pointB) {
			Fit(
				pointA,
				pointB,
				_viewDirection,
				_leftPadding,
				_rightPadding,
				_topPadding,
				_bottomPadding,
				_paddingMode
			);
		}

		public void Fit(Vector3 pointA, Vector3 pointB, Vector2 viewDirection) {
			Fit(
				pointA,
				pointB,
				viewDirection,
				_leftPadding,
				_rightPadding,
				_topPadding,
				_bottomPadding,
				_paddingMode
			);
		}

		public void Fit(
			Vector3 pointA,
			Vector3 pointB,
			Vector2 viewDirection,
			float leftPadding,
			float rightPadding,
			float topPadding,
			float bottomPadding,
			PaddingMode paddingMode
		) {
			if (_miniMapUI == null)
				throw new InvalidOperationException($"{nameof(MiniMapUI)} is not assigned.");
			if (viewDirection.sqrMagnitude <= Mathf.Epsilon)
				throw new ArgumentOutOfRangeException(nameof(viewDirection));
			if (leftPadding < 0f || rightPadding < 0f || topPadding < 0f || bottomPadding < 0f)
				throw new ArgumentOutOfRangeException(nameof(leftPadding));
			if (paddingMode == PaddingMode.Ratio &&
			    (1f <= leftPadding + rightPadding || 1f <= topPadding + bottomPadding))
				throw new ArgumentOutOfRangeException(nameof(paddingMode));

			Vector2 worldDirection = new Vector2(pointB.x - pointA.x, pointB.z - pointA.z);
			float distance = worldDirection.magnitude;
			Vector2 normalizedViewDirection = viewDirection.normalized;
			float rotation = distance <= Mathf.Epsilon
				? MiniMapUI.Rotation
				: Vector2.SignedAngle(worldDirection, normalizedViewDirection);
			float contentWidth = Mathf.Abs(normalizedViewDirection.x) * distance;
			float contentHeight = Mathf.Abs(normalizedViewDirection.y) * distance;
			float viewAspectRatio = Mathf.Max(0.01f, MiniMapUI.ViewAspectRatio);

			switch (paddingMode) {
				case PaddingMode.World:
					break;
				case PaddingMode.Ratio:
					float ratioViewWorldHeight = Mathf.Max(
						0.01f,
						contentHeight / (1f - topPadding - bottomPadding),
						contentWidth / ((1f - leftPadding - rightPadding) * viewAspectRatio)
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
			float requiredWorldHeight = contentHeight + topPadding + bottomPadding;
			float requiredWorldWidth = contentWidth + leftPadding + rightPadding;
			float viewWorldHeight = Mathf.Max(
				0.01f,
				requiredWorldHeight,
				requiredWorldWidth / viewAspectRatio
			);
			MiniMapUI.SetView((pointA + pointB) * 0.5f + centerOffset, rotation, viewWorldHeight);
		}

		// - Public Properties -
		public IMiniMapUI MiniMapUI => _miniMapUI;
		public Vector2 ViewDirection
		{
			get { return _viewDirection; }
			set { _viewDirection = value; }
		}
		public float LeftPadding
		{
			get { return _leftPadding; }
			set { _leftPadding = value; }
		}
		public float RightPadding
		{
			get { return _rightPadding; }
			set { _rightPadding = value; }
		}
		public float TopPadding
		{
			get { return _topPadding; }
			set { _topPadding = value; }
		}
		public float BottomPadding
		{
			get { return _bottomPadding; }
			set { _bottomPadding = value; }
		}
		public PaddingMode Mode
		{
			get { return _paddingMode; }
			set { _paddingMode = value; }
		}

		// - Handler -
		void Awake() {
			if (_miniMapUI == null && _miniMapUIObject != null)
				_miniMapUI = _miniMapUIObject as IMiniMapUI;
		}

		// - Internals -
		[Title(Headers.Injectable)]
		[SerializeField] UnityEngine.Object _miniMapUIObject;

		[Title(Headers.Settings)]
		[SerializeField] Vector2 _viewDirection = Vector2.up;
		[SerializeField] float _leftPadding;
		[SerializeField] float _rightPadding;
		[SerializeField] float _topPadding;
		[SerializeField] float _bottomPadding;
		[SerializeField] PaddingMode _paddingMode;

		IMiniMapUI _miniMapUI;
	}
}
